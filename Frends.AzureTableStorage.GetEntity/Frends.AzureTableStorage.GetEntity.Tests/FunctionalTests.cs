using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.GetEntity.Definitions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Frends.AzureTableStorage.GetEntity.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private string tableName;

    [SetUp]
    public async Task SetUp()
    {
        tableName = $"testgetentity{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var serviceClient = new TableServiceClient(ConnectionString);
        await serviceClient.CreateTableIfNotExistsAsync(tableName);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (string.IsNullOrEmpty(ConnectionString) || string.IsNullOrEmpty(tableName))
            return;

        try
        {
            var serviceClient = new TableServiceClient(ConnectionString);
            await serviceClient.DeleteTableAsync(tableName);
        }
        catch
        {
            // Ignore cleanup errors.
        }
    }

    [Test]
    public async Task GetEntity_MissingEntity_ReturnsSuccessWithFoundFalse()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = true;

        var result = await AzureTableStorage.GetEntity(
            BuildInput("pk1", "missing"), DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Found, Is.False);
        Assert.That((object)result.Entity, Is.Null);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task GetEntity_MissingTable_ReturnsErrorInsteadOfFoundFalse()
    {
        var input = new Input
        {
            TableName = $"missingtable{Guid.NewGuid():N}"[..30],
            PartitionKey = "pk1",
            RowKey = "1",
        };

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.GetEntity(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Found, Is.False);
        Assert.That((object)result.Entity, Is.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Is.Not.Empty);
    }

    [Test]
    public async Task GetEntity_MissingTable_WithThrowErrorOnFailureTrue_Throws()
    {
        var input = new Input
        {
            TableName = $"missingtable{Guid.NewGuid():N}"[..30],
            PartitionKey = "pk1",
            RowKey = "1",
        };

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = true;

        var ex = Assert.Throws<RequestFailedException>(() =>
             AzureTableStorage.GetEntity(input, DefaultConnectionStringConnection(), options, CancellationToken.None).GetAwaiter().GetResult());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message.Contains("The table specified does not exist"));
    }

    [Test]
    public async Task GetEntity_ExistingEntity_ReturnsPropertiesAndETag()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Anna"), ("Age", 30));

        var result = await AzureTableStorage.GetEntity(
            BuildInput("pk1", "1"), DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        var entity = (JObject)result.Entity;
        var expectedETag = (await CreateTableClient().GetEntityAsync<TableEntity>("pk1", "1")).Value.ETag.ToString();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Found, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That((string)entity["PartitionKey"], Is.EqualTo("pk1"));
        Assert.That((string)entity["RowKey"], Is.EqualTo("1"));
        Assert.That((string)entity["Name"], Is.EqualTo("Anna"));
        Assert.That((int)entity["Age"], Is.EqualTo(30));
        Assert.That((string)entity["odata.etag"], Is.EqualTo(expectedETag));
    }

    [Test]
    public async Task GetEntity_ReturnsNewETagAfterEntityWasModified()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));

        var before = (JObject)(await AzureTableStorage.GetEntity(
            BuildInput("pk1", "1"), DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None)).Entity;

        await CreateTableClient().UpdateEntityAsync(
            new TableEntity("pk1", "1") { ["Name"] = "New" }, ETag.All, TableUpdateMode.Merge);

        var after = (JObject)(await AzureTableStorage.GetEntity(
            BuildInput("pk1", "1"), DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None)).Entity;

        Assert.That((string)after["Name"], Is.EqualTo("New"));
        Assert.That((string)after["odata.etag"], Is.Not.EqualTo((string)before["odata.etag"]));
    }

    [Test]
    public async Task GetEntity_MapsSpecialTypes()
    {
        var guid = Guid.NewGuid();
        var bytes = new byte[] { 1, 2, 3 };
        var date = new DateTimeOffset(2024, 5, 1, 10, 15, 30, TimeSpan.Zero);

        await SeedEntityAsync("pk1", "1", ("Bytes", bytes), ("Id", guid), ("Flag", true), ("Price", 12.5), ("Created", date));

        var result = await AzureTableStorage.GetEntity(
            BuildInput("pk1", "1"), DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        var entity = (JObject)result.Entity;

        Assert.That((string)entity["Bytes"], Is.EqualTo(Convert.ToBase64String(bytes)));
        Assert.That((string)entity["Id"], Is.EqualTo(guid.ToString()));
        Assert.That((bool)entity["Flag"], Is.True);
        Assert.That((double)entity["Price"], Is.EqualTo(12.5));
        Assert.That((DateTimeOffset)entity["Created"], Is.EqualTo(date));
    }

    [Test]
    public async Task GetEntity_WithSasToken_ShouldReturnEntity()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Anna"));

        var result = await AzureTableStorage.GetEntity(
            BuildInput("pk1", "1"), DefaultSasTokenConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Found, Is.True);
    }

    private Input BuildInput(string partitionKey, string rowKey) => new()
    {
        TableName = tableName,
        PartitionKey = partitionKey,
        RowKey = rowKey,
    };

    private TableClient CreateTableClient()
    {
        var serviceClient = new TableServiceClient(ConnectionString);
        return serviceClient.GetTableClient(tableName);
    }

    private async Task SeedEntityAsync(string pk, string rk, params (string Key, object Value)[] properties)
    {
        var entity = new TableEntity(pk, rk);
        foreach (var (key, value) in properties)
            entity[key] = value;

        await CreateTableClient().UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }
}
