using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.UpdateEntities.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.UpdateEntities.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private string tableName;

    [SetUp]
    public async Task SetUp()
    {
        tableName = $"testupdateentities{DateTime.UtcNow:yyyyMMddHHmmssfff}";
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

    [TestCase(UpdateMode.Merge, true)]
    [TestCase(UpdateMode.Replace, false)]
    public async Task UpdateEntities_Modes_ShouldHandleExistingProperties(
        UpdateMode updateMode,
        bool expectedKeepProperty)
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"), ("Keep", "Yes"));

        var input = BuildInput(new { PartitionKey = "pk1", RowKey = "1", Name = "New" });
        var options = DefaultOptions();
        options.UpdateMode = updateMode;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.SucceededItems, Has.Count.EqualTo(1));

        var saved = await GetEntityAsync("pk1", "1");
        Assert.That(saved.GetString("Name"), Is.EqualTo("New"));
        Assert.That(saved.ContainsKey("Keep"), Is.EqualTo(expectedKeepProperty));
    }

    [Test]
    public async Task UpdateEntities_DefaultOptions_UseMergeMode()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"), ("Keep", "Yes"));

        var input = BuildInput(new { PartitionKey = "pk1", RowKey = "1", Name = "New" });

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That((await GetEntityAsync("pk1", "1")).ContainsKey("Keep"), Is.True);
    }

    [TestCase(UpdateMode.Merge)]
    [TestCase(UpdateMode.Replace)]
    public async Task UpdateEntities_MissingEntity_FailsWith404AndDoesNotCreateIt(UpdateMode updateMode)
    {
        var input = BuildInput(new { PartitionKey = "pk1", RowKey = "1", Name = "Ghost" });

        var options = DefaultOptions();
        options.UpdateMode = updateMode;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(1));
        Assert.That(result.Error.FailedItems[0].IsCause, Is.True);
        Assert.That(result.Error.FailedItems[0].Reason, Does.Contain("404"));

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.False);
    }

    [Test]
    public async Task UpdateEntities_WithCurrentETag_ShouldSucceed()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        var etag = await GetETagAsync("pk1", "1");

        var input = BuildInput(Entity("pk1", "1", etag, "New"));

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("New"));
    }

    [Test]
    public async Task UpdateEntities_WithStaleETag_FailsWith412AndKeepsEntityUnchanged()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        var staleEtag = await GetETagAsync("pk1", "1");
        await ModifyDirectlyAsync("pk1", "1", ("Name", "ChangedByOthers"));

        var input = BuildInput(Entity("pk1", "1", staleEtag, "Mine"));

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(1));
        Assert.That(result.Error.FailedItems[0].IsCause, Is.True);
        Assert.That(result.Error.FailedItems[0].Reason, Does.Contain("412"));

        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("ChangedByOthers"));
    }

    [TestCase("")]
    [TestCase(null)]
    public async Task UpdateEntities_EmptyETag_UpdatesUnconditionally(string etag)
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        await ModifyDirectlyAsync("pk1", "1", ("Name", "ChangedByOthers"));

        var input = BuildInput(Entity("pk1", "1", etag, "Mine"));

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("Mine"));
    }

    [Test]
    public async Task UpdateEntities_ETagOfWrongType_ReturnsValidationError()
    {
        var input = BuildInput(new Dictionary<string, object>
        {
            ["PartitionKey"] = "pk1",
            ["RowKey"] = "1",
            ["odata.etag"] = 123,
        });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("odata.etag"));
        Assert.That(result.Error.FailedItems, Is.Empty);
    }

    [Test]
    public async Task UpdateEntities_MixedETags_OneStale_RollsBackWholeBatch()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        await SeedEntityAsync("pk1", "2", ("Name", "Old"));
        await SeedEntityAsync("pk1", "3", ("Name", "Old"));

        var currentEtag = await GetETagAsync("pk1", "1");
        var staleEtag = await GetETagAsync("pk1", "3");
        await ModifyDirectlyAsync("pk1", "3", ("Name", "ChangedByOthers"));

        var input = BuildInput(
            Entity("pk1", "1", currentEtag, "New"),
            Entity("pk1", "2", null, "New"),
            Entity("pk1", "3", staleEtag, "New"));

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(3));
        Assert.That(result.Error.FailedItems.Single(f => f.IsCause).RowKey, Is.EqualTo("3"));
        Assert.That(
            result.Error.FailedItems.Where(f => !f.IsCause).Select(f => f.Reason),
            Has.All.StartWith("Not updated"));

        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("Old"));
        Assert.That((await GetEntityAsync("pk1", "2")).GetString("Name"), Is.EqualTo("Old"));
    }

    [Test]
    public async Task UpdateEntities_BatchSizeOne_ContinueOnFailure_OnlyStaleEntityFails()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        await SeedEntityAsync("pk1", "2", ("Name", "Old"));
        await SeedEntityAsync("pk1", "3", ("Name", "Old"));

        var staleEtag = await GetETagAsync("pk1", "3");
        await ModifyDirectlyAsync("pk1", "3", ("Name", "ChangedByOthers"));

        var input = BuildInput(
            Entity("pk1", "1", null, "New"),
            Entity("pk1", "2", null, "New"),
            Entity("pk1", "3", staleEtag, "New"));

        var options = DefaultOptions();
        options.BatchSize = 1;
        options.ContinueOnFailure = true;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SucceededItems.Select(x => x.RowKey), Is.EquivalentTo(new[] { "1", "2" }));
        Assert.That(result.Error.Message, Does.Contain("1 of 3 entities failed"));
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(1));
        Assert.That(result.Error.FailedItems[0].RowKey, Is.EqualTo("3"));
        Assert.That(result.Error.FailedItems[0].IsCause, Is.True);

        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("New"));
        Assert.That((await GetEntityAsync("pk1", "2")).GetString("Name"), Is.EqualTo("New"));
        Assert.That((await GetEntityAsync("pk1", "3")).GetString("Name"), Is.EqualTo("ChangedByOthers"));
    }

    [Test]
    public async Task UpdateEntities_DefaultBatchSize_OneMissingEntity_RollsBackWholeBatch()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        await SeedEntityAsync("pk1", "3", ("Name", "Old"));

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "New" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "Missing" },
            new { PartitionKey = "pk1", RowKey = "3", Name = "New" });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(3));
        Assert.That(result.Error.FailedItems.Single(f => f.IsCause).RowKey, Is.EqualTo("2"));
        Assert.That(
            result.Error.FailedItems.Where(f => !f.IsCause).Select(f => f.Reason),
            Has.All.StartWith("Not updated"));

        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("Old"));
        Assert.That((await GetEntityAsync("pk1", "3")).GetString("Name"), Is.EqualTo("Old"));
        Assert.That(await EntityExistsAsync("pk1", "2"), Is.False);
    }

    [Test]
    public async Task UpdateEntities_WithContinueOnFailure_FailedBatchDoesNotBlockOtherPartitions()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        await SeedEntityAsync("pk2", "1", ("Name", "Old"));

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "New" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "Missing" },
            new { PartitionKey = "pk2", RowKey = "1", Name = "New" });

        var options = DefaultOptions();
        options.ContinueOnFailure = true;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(
            result.SucceededItems.Select(x => $"{x.PartitionKey}/{x.RowKey}"),
            Is.EquivalentTo(new[] { "pk2/1" }));
        Assert.That(result.Error.Message, Does.Contain("2 of 3 entities failed"));
        Assert.That(result.Error.FailedItems.Select(f => f.RowKey), Is.EquivalentTo(new[] { "1", "2" }));
        Assert.That(result.Error.FailedItems.Single(f => f.IsCause).RowKey, Is.EqualTo("2"));

        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("Old"));
        Assert.That((await GetEntityAsync("pk2", "1")).GetString("Name"), Is.EqualTo("New"));
    }

    [Test]
    public async Task UpdateEntities_BatchSizeSmallerThanPartition_FailedChunkDoesNotBlockNextChunk()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        await SeedEntityAsync("pk1", "3", ("Name", "Old"));

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "RolledBack" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "Missing" },
            new { PartitionKey = "pk1", RowKey = "3", Name = "Succeeded" });

        var options = DefaultOptions();
        options.BatchSize = 2; // transactions: [1, 2] and [3]
        options.ContinueOnFailure = true;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SucceededItems.Select(x => x.RowKey), Is.EquivalentTo(new[] { "3" }));
        Assert.That(result.Error.Message, Does.Contain("2 of 3 entities failed"));
        Assert.That(result.Error.FailedItems.Single(i => i.IsCause).RowKey, Is.EqualTo("2"));
        Assert.That(result.Error.FailedItems.Single(i => !i.IsCause).Reason, Does.StartWith("Not updated"));

        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("Old"));
        Assert.That((await GetEntityAsync("pk1", "3")).GetString("Name"), Is.EqualTo("Succeeded"));
    }

    [Test]
    public async Task UpdateEntities_WithThrowErrorOnFailureTrue_ShouldThrowFailureSummaryWhenPartiallyUpdated()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));
        await SeedEntityAsync("pk1", "2", ("Name", "Old"));
        await SeedEntityAsync("pk2", "2", ("Name", "Old"));
        await SeedEntityAsync("pk3", "1", ("Name", "Old"));

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1", Name = "New" },
            new { PartitionKey = "pk1", RowKey = "2", Name = "New" },
            new { PartitionKey = "pk2", RowKey = "1", Name = "Missing" },
            new { PartitionKey = "pk2", RowKey = "2", Name = "RolledBack" },
            new { PartitionKey = "pk3", RowKey = "1", Name = "NotAttempted" });

        var options = DefaultOptions();
        options.ContinueOnFailure = false;
        options.ThrowErrorOnFailure = true;

        var ex = Assert.Throws<Exception>(() =>
            AzureTableStorage.UpdateEntities(input, DefaultConnectionStringConnection(), options, CancellationToken.None).GetAwaiter().GetResult());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain("Updated before the failure: 2"));
        Assert.That(ex.Message, Does.Contain("Not updated: 2. Not attempted: 1."));
        Assert.That(ex.Message, Does.Contain("Cause: PartitionKey 'pk2', RowKey '1'"));

        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("New"));
        Assert.That((await GetEntityAsync("pk1", "2")).GetString("Name"), Is.EqualTo("New"));
        Assert.That((await GetEntityAsync("pk2", "2")).GetString("Name"), Is.EqualTo("Old"));
        Assert.That((await GetEntityAsync("pk3", "1")).GetString("Name"), Is.EqualTo("Old"));
    }

    [Test]
    public async Task UpdateEntities_MissingTable_MarksNoEntityAsCause()
    {
        var input = new Input
        {
            TableName = $"missingtable{Guid.NewGuid():N}"[..30],
            Entities = ToEntitiesJson(
                new { PartitionKey = "pk1", RowKey = "1", Name = "New" },
                new { PartitionKey = "pk1", RowKey = "2", Name = "New" }),
        };

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(2));
        Assert.That(result.Error.FailedItems.Any(f => f.IsCause), Is.False);
    }

    [Test]
    public async Task UpdateEntities_WhenErrorOccursBeforeLoop_ShouldReturnEmptyProgressLists()
    {
        var input = BuildInput(new { PartitionKey = "pk1", Name = "MissingRowKey" });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.UpdateEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("must have non-empty string PartitionKey and RowKey"));
        Assert.That(result.Error.AdditionalInfo, Is.Not.Null);
        Assert.That(result.Error.FailedItems, Is.Empty);
    }

    [Test]
    public async Task UpdateEntities_WithSasToken_ShouldUpdateEntity()
    {
        await SeedEntityAsync("pk1", "1", ("Name", "Old"));

        var result = await AzureTableStorage.UpdateEntities(
            BuildInput(new { PartitionKey = "pk1", RowKey = "1", Name = "New" }),
            DefaultSasTokenConnection(),
            DefaultOptions(),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That((await GetEntityAsync("pk1", "1")).GetString("Name"), Is.EqualTo("New"));
    }

    private static Dictionary<string, object> Entity(string pk, string rk, string etag, string name) => new()
    {
        ["PartitionKey"] = pk,
        ["RowKey"] = rk,
        ["Name"] = name,
        ["odata.etag"] = etag,
    };

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

    private async Task SeedEntityAsync(string pk, string rk, params (string Key, object Value)[] properties)
    {
        var entity = new TableEntity(pk, rk);
        foreach (var (key, value) in properties)
            entity[key] = value;

        await CreateTableClient().UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    // Changes the entity outside of the task, which makes any previously read ETag stale
    private async Task ModifyDirectlyAsync(string pk, string rk, params (string Key, object Value)[] properties)
    {
        var entity = new TableEntity(pk, rk);
        foreach (var (key, value) in properties)
            entity[key] = value;

        await CreateTableClient().UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Merge);
    }

    private async Task<TableEntity> GetEntityAsync(string partitionKey, string rowKey) =>
        (await CreateTableClient().GetEntityAsync<TableEntity>(partitionKey, rowKey)).Value;

    private async Task<string> GetETagAsync(string partitionKey, string rowKey) =>
        (await GetEntityAsync(partitionKey, rowKey)).ETag.ToString();

    private async Task<bool> EntityExistsAsync(string partitionKey, string rowKey)
    {
        try
        {
            await GetEntityAsync(partitionKey, rowKey);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }
}