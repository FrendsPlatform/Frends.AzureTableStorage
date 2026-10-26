using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.QueryEntities.Definitions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Frends.AzureTableStorage.QueryEntities.Tests;

[TestFixture]
internal class QueryEntitiesTests : TestBase
{
    private string tableName;
    private TableClient tableClient;

    [SetUp]
    public async Task SetUp()
    {
        tableName = $"queryentities{Guid.NewGuid():N}"[..30];
        tableClient = new TableServiceClient(ConnectionString).GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync();

        await tableClient.AddEntityAsync(new TableEntity("pk1", "1")
        {
            ["Name"] = "Anna",
            ["Price"] = 10,
            ["Bytes"] = new byte[] { 1, 2, 3 },
            ["Hidden"] = "x",
        });
        await tableClient.AddEntityAsync(new TableEntity("pk1", "2") { ["Name"] = "Bob", ["Price"] = 20, ["Hidden"] = "y" });
        await tableClient.AddEntityAsync(new TableEntity("pk1", "3") { ["Name"] = "Cara", ["Price"] = 30, ["Hidden"] = "z" });
        await tableClient.AddEntityAsync(new TableEntity("pk2", "4") { ["Name"] = "Dan", ["Price"] = 40, ["Hidden"] = "w" });
    }

    [TearDown]
    public async Task TearDown()
    {
        if (tableClient == null)
            return;

        try
        {
            await tableClient.DeleteAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }
    }

    [Test]
    public async Task ShouldFilterAndSelectEntityProperties()
    {
        var input = new Input
        {
            TableName = tableName,
            Filter = "PartitionKey eq 'pk1' and RowKey ge '2'",
            Select = ["Name"],
            Top = 10,
        };

        var result = await RunAsync(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entities, Is.TypeOf<JArray>());
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That((string)result.Entities[0]["PartitionKey"], Is.EqualTo("pk1"));
        Assert.That((string)result.Entities[0]["RowKey"], Is.EqualTo("2"));
        Assert.That((string)result.Entities[0]["Name"], Is.EqualTo("Bob"));
        Assert.That((string)result.Entities[0]["odata.etag"], Is.Not.Empty);
        Assert.That(result.Entities[0]["Price"], Is.Null);
        Assert.That(result.Entities[0]["Hidden"], Is.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ShouldReturnOnlyTheFirstPageWithTypedProperties()
    {
        var input = new Input
        {
            TableName = tableName,
            Filter = "PartitionKey eq 'pk1'",
            Select = ["Name", "Price", "Bytes"],
            Top = 2,
        };

        var result = await RunAsync(input);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Entities, Has.Count.EqualTo(2));
        Assert.That((string)result.Entities[0]["RowKey"], Is.EqualTo("1"));
        Assert.That((string)result.Entities[0]["Name"], Is.EqualTo("Anna"));
        Assert.That((int)result.Entities[0]["Price"], Is.EqualTo(10));
        Assert.That(
            (string)((JArray)result.Entities)[0]["Bytes"],
            Is.EqualTo(Convert.ToBase64String([1, 2, 3])));
        Assert.That((string)result.Entities[1]["RowKey"], Is.EqualTo("2"));
        Assert.That(result.ContinuationToken, Is.Not.Empty);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ShouldResumeUntilTheLastPage()
    {
        var input = new Input
        {
            TableName = tableName,
            Filter = "PartitionKey eq 'pk1'",
            Select = ["Name"],
            Top = 2,
        };

        var rowKeys = new List<string>();
        for (var pageNumber = 0; pageNumber < 2; pageNumber++)
        {
            var result = await RunAsync(input);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Count, Is.EqualTo(pageNumber == 0 ? 2 : 1));
            Assert.That(result.Entities, Has.Count.EqualTo(result.Count));
            Assert.That(result.Error, Is.Null);

            rowKeys.AddRange(((JArray)result.Entities).Select(entity => (string)entity["RowKey"]));
            input.ContinuationToken = result.ContinuationToken;

            Assert.That(result.ContinuationToken, pageNumber == 0 ? Is.Not.Empty : Is.Null);
        }

        Assert.That(rowKeys, Is.EqualTo(["1", "2", "3"]));
    }

    [Test]
    public async Task ShouldOmitOptionalParametersAndReturnAllEntities()
    {
        var result = await RunAsync(new Input { TableName = tableName });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(4));
        Assert.That(
            ((JArray)result.Entities).Select(e => (string)e["RowKey"]).OrderBy(x => x),
            Is.EqualTo(new List<string> { "1", "2", "3", "4" }));
        Assert.That(result.ContinuationToken, Is.Null);
    }

    [Test]
    public async Task ShouldReturnEmptyPageWhenNothingMatches()
    {
        var result = await RunAsync(new Input
        {
            TableName = tableName,
            Filter = "PartitionKey eq 'notfound'",
            Top = 1,
        });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Entities, Is.Empty);
        Assert.That(result.Count, Is.Zero);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ShouldReturnFailureWhenTableDoesNotExist()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await RunAsync(
            new Input { TableName = $"missingtable{Guid.NewGuid():N}"[..30] }, options: options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Entities, Is.Null);
        Assert.That(result.Count, Is.Zero);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Error.AdditionalInfo, Is.TypeOf<RequestFailedException>());
        Assert.That(((RequestFailedException)result.Error.AdditionalInfo).Status, Is.EqualTo(404));
    }

    [Test]
    public async Task ShouldReturnFailureForInvalidODataFilter()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await RunAsync(
            new Input
            {
                TableName = tableName,
                Filter = "PartitionKey eq 'unterminated",
            },
            options: options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.AdditionalInfo, Is.TypeOf<RequestFailedException>());
    }

    [Test]
    public async Task ShouldHonorCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            RunAsync(new Input { TableName = tableName }, cancellationToken: source.Token));
    }

    [Test]
    public async Task ShouldQueryWithSasToken()
    {
        var result = await RunAsync(
            new Input
            {
                TableName = tableName,
                Filter = "PartitionKey eq 'pk1'",
            },
            DefaultSasTokenConnection());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShouldQueryWithOAuth2()
    {
        var result = await RunAsync(
            new Input
            {
                TableName = tableName,
                Filter = "PartitionKey eq 'pk1'",
            },
            DefaultOAuth2Connection());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(3));
    }
}
