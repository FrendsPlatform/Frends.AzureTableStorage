using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.InsertEntities.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.InsertEntities.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private string tableName;

    [SetUp]
    public async Task SetUp()
    {
        tableName = $"testinsertentities{DateTime.UtcNow:yyyyMMddhhmmssfff}";
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

    [TestCase(InsertMode.Add, true)]
    [TestCase(InsertMode.Add, false)]
    [TestCase(InsertMode.UpsertMerge, true)]
    [TestCase(InsertMode.UpsertReplace, false)]
    public async Task InsertEntities_WithDifferentModesAndTransactionSettings_ShouldSucceed(
        InsertMode insertMode,
        bool useTransactions)
    {
        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "Anna" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "Bob" });

        var options = DefaultOptions();
        options.InsertMode = insertMode;
        options.UseTransactions = useTransactions;
        options.BatchSize = 100;

        var result = await AzureTableStorage.InsertEntities(
            input,
            DefaultConnectionStringConnection(),
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.SucceededItems, Has.Count.EqualTo(2));
    }

    [TestCase(InsertMode.UpsertMerge, true)]
    [TestCase(InsertMode.UpsertReplace, false)]
    public async Task InsertEntities_UpsertModes_ShouldHandleExistingProperties(
        InsertMode insertMode,
        bool expectedKeepProperty)
    {
        await SeedEntityAsync(new { PartitionKey = "pk1", RowKey = "1", Name = "Old", Keep = "Yes" });

        var input = BuildInput(new { PartitionKey = "pk1", RowKey = "1", Name = "New" });
        var options = DefaultOptions();
        options.InsertMode = insertMode;
        options.UseTransactions = false;

        var result = await AzureTableStorage.InsertEntities(
            input,
            DefaultSasTokenConnection(),
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);

        var tableClient = CreateTableClient();
        var saved = (await tableClient.GetEntityAsync<TableEntity>("pk1", "1")).Value;

        Assert.That(saved.GetString("Name"), Is.EqualTo("New"));
        Assert.That(saved.ContainsKey("Keep"), Is.EqualTo(expectedKeepProperty));
    }

    [Test]
    public async Task InsertEntities_OnFailureAfterPartialWrite_ShouldReturnProgressAndFailureData()
    {
        await SeedEntityAsync(new { PartitionKey = "pk1", RowKey = "2", Name = "Exists" });

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "First" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "Duplicate" },
            new { PartitionKey = "pk1", RowKey = "3", Name = "NotAttempted" });

        var options = DefaultOptions();
        options.UseTransactions = false;
        options.ContinueOnFailure = false;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.InsertEntities(
            input,
            DefaultConnectionStringConnection(),
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems.Select(x => x.RowKey), Is.EquivalentTo(new[] { "1" }));
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Is.Not.Empty);
        Assert.That(result.Error.AdditionalInfo, Is.Not.Null);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(1));
        Assert.That(result.Error.FailedItems[0].RowKey, Is.EqualTo("2"));
        Assert.That(result.Error.FailedItems[0].IsCause, Is.True);

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.True);
        Assert.That(await EntityExistsAsync("pk1", "3"), Is.False);
    }

    [Test]
    public async Task InsertEntities_WithContinueOnFailure_ShouldSucceedAndReturnFailedItems()
    {
        await SeedEntityAsync(new { PartitionKey = "pk1", RowKey = "2", Name = "Exists" });

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "First" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "Duplicate" },
            new { PartitionKey = "pk1", RowKey = "3", Name = "Third" });

        var options = DefaultOptions();
        options.UseTransactions = false;
        options.ContinueOnFailure = true;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.InsertEntities(
            input,
            DefaultConnectionStringConnection(),
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SucceededItems.Select(x => x.RowKey), Is.EquivalentTo(new[] { "1", "3" }));
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("1 of 3 entities failed"));
        Assert.That(result.Error.AdditionalInfo, Is.Null);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(1));
        Assert.That(result.Error.FailedItems[0].RowKey, Is.EqualTo("2"));
    }

    [Test]
    public async Task InsertEntities_WithTransactionsAndContinueOnFailure_ShouldMarkCauseAndBatchAtomicFailure()
    {
        await SeedEntityAsync(new { PartitionKey = "pk1", RowKey = "2", Name = "Exists" });

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "RolledBack" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "Duplicate" },
            new { PartitionKey = "pk2", RowKey = "3", Name = "Succeeded" });

        var options = DefaultOptions();
        options.UseTransactions = true;
        options.BatchSize = 2;
        options.ContinueOnFailure = true;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.InsertEntities(
            input,
            DefaultConnectionStringConnection(),
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SucceededItems.Select(x => x.RowKey), Is.EquivalentTo(new[] { "3" }));
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("2 of 3 entities failed"));
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(2));
        Assert.That(result.Error.FailedItems.Count(i => i.IsCause), Is.EqualTo(1));
        Assert.That(result.Error.FailedItems.Single(i => !i.IsCause).Reason, Does.Contain("not applied"));

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.False);
        Assert.That(await EntityExistsAsync("pk2", "3"), Is.True);
    }

    [Test]
    public void InsertEntities_WithThrowErrorOnFailureTrue_ShouldThrowFailureSummaryWhenPartiallyWritten()
    {
        SeedEntityAsync(new { PartitionKey = "pk1", RowKey = "2", Name = "Exists" }).GetAwaiter().GetResult();

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "First" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "Duplicate" },
            new { PartitionKey = "pk1", RowKey = "3", Name = "NotAttempted" });

        var options = DefaultOptions();
        options.UseTransactions = false;
        options.ContinueOnFailure = false;
        options.ThrowErrorOnFailure = true;

        Func<Task> action = async () =>
            await AzureTableStorage.InsertEntities(
                input,
                DefaultConnectionStringConnection(),
                options,
                CancellationToken.None);

        var ex = Assert.ThrowsAsync<Exception>(action);

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain("Written before the failure: 1 ['pk1/1']."));
        Assert.That(ex.Message, Does.Contain("Not written: 1. Not attempted: 1."));
        Assert.That(ex.Message, Does.Contain("Cause: PartitionKey 'pk1', RowKey '2'"));
    }

    [Test]
    public async Task InsertEntities_WhenErrorOccursBeforeLoop_ShouldReturnEmptyProgressLists()
    {
        var input = BuildInput(new { PartitionKey = "pk1", Name = "MissingRowKey" });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.InsertEntities(
            input,
            DefaultConnectionStringConnection(),
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("must have non-empty string PartitionKey and RowKey"));
        Assert.That(result.Error.AdditionalInfo, Is.Not.Null);
        Assert.That(result.Error.FailedItems, Is.Empty);
    }

    private Input BuildInput(params object[] entities) => new()
    {
        TableName = tableName,
        Entities = ToEntitiesJson(entities),
    };

    private TableClient CreateTableClient()
    {
        var serviceClient = new TableServiceClient(ConnectionString);
        return serviceClient.GetTableClient(tableName);
    }

    private async Task SeedEntityAsync(object entity)
    {
        var seedInput = BuildInput(entity);
        var seedOptions = DefaultOptions();
        seedOptions.InsertMode = InsertMode.UpsertReplace;
        seedOptions.UseTransactions = false;
        seedOptions.ThrowErrorOnFailure = true;

        await AzureTableStorage.InsertEntities(
            seedInput,
            DefaultConnectionStringConnection(),
            seedOptions,
            CancellationToken.None);
    }

    private async Task<bool> EntityExistsAsync(string partitionKey, string rowKey)
    {
        var tableClient = CreateTableClient();
        try
        {
            await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }
}
