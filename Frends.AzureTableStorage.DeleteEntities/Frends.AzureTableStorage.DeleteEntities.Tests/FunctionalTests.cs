using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.DeleteEntities.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.DeleteEntities.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private string tableName;

    [SetUp]
    public async Task SetUp()
    {
        tableName = $"testdeleteentities{DateTime.UtcNow:yyyyMMddHHmmssfff}";
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
    public async Task DeleteEntities_ExistingEntities_ShouldDeleteThem()
    {
        await SeedEntityAsync("pk1", "1");
        await SeedEntityAsync("pk1", "2");

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1" },
            new { PartitionKey = "pk1", RowKey = "2" });

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.SucceededItems, Has.Count.EqualTo(2));

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.False);
        Assert.That(await EntityExistsAsync("pk1", "2"), Is.False);
    }

    [Test]
    public async Task DeleteEntities_MissingEntity_FailsWith404()
    {
        var input = BuildInput(new { PartitionKey = "pk1", RowKey = "1" });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(1));
        Assert.That(result.Error.FailedItems[0].IsCause, Is.True);
        Assert.That(result.Error.FailedItems[0].Reason, Does.Contain("404"));
    }

    [Test]
    public async Task DeleteEntities_WithCurrentETag_ShouldDelete()
    {
        await SeedEntityAsync("pk1", "1");
        var etag = await GetETagAsync("pk1", "1");

        var input = BuildInput(Key("pk1", "1", etag));

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(await EntityExistsAsync("pk1", "1"), Is.False);
    }

    [Test]
    public async Task DeleteEntities_WithStaleETag_FailsWith412AndKeepsEntity()
    {
        await SeedEntityAsync("pk1", "1");
        var staleEtag = await GetETagAsync("pk1", "1");
        await ModifyDirectlyAsync("pk1", "1", ("Name", "ChangedByOthers"));

        var input = BuildInput(Key("pk1", "1", staleEtag));

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(1));
        Assert.That(result.Error.FailedItems[0].IsCause, Is.True);
        Assert.That(result.Error.FailedItems[0].Reason, Does.Contain("412"));

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.True);
    }

    [TestCase("")]
    [TestCase(null)]
    public async Task DeleteEntities_EmptyETag_DeletesUnconditionally(string etag)
    {
        await SeedEntityAsync("pk1", "1");
        await ModifyDirectlyAsync("pk1", "1", ("Name", "ChangedByOthers"));

        var input = BuildInput(Key("pk1", "1", etag));

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(await EntityExistsAsync("pk1", "1"), Is.False);
    }

    [Test]
    public async Task DeleteEntities_ETagOfWrongType_ReturnsValidationError()
    {
        await SeedEntityAsync("pk1", "1");

        var input = BuildInput(new Dictionary<string, object>
        {
            ["PartitionKey"] = "pk1",
            ["RowKey"] = "1",
            ["odata.etag"] = 123,
        });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("odata.etag"));
        Assert.That(result.Error.FailedItems, Is.Empty);
        Assert.That(await EntityExistsAsync("pk1", "1"), Is.True);
    }

    [Test]
    public async Task DeleteEntities_UnsupportedProperties_ReturnsValidationErrorAndDeletesNothing()
    {
        await SeedEntityAsync("pk1", "1");

        // A full entity was passed by mistake instead of only its keys
        var input = BuildInput(new { PartitionKey = "pk1", RowKey = "1", Name = "Seed" });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Message, Does.Contain("unsupported properties"));
        Assert.That(result.Error.Message, Does.Contain("Name"));
        Assert.That(result.Error.FailedItems, Is.Empty);
        Assert.That(await EntityExistsAsync("pk1", "1"), Is.True);
    }

    [Test]
    public async Task DeleteEntities_DefaultBatchSize_OneMissingEntity_RollsBackWholeBatch()
    {
        await SeedEntityAsync("pk1", "1");
        await SeedEntityAsync("pk1", "3");

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1" },
            new { PartitionKey = "pk1", RowKey = "2" }, // missing
            new { PartitionKey = "pk1", RowKey = "3" });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(3));
        Assert.That(result.Error.FailedItems.Single(f => f.IsCause).RowKey, Is.EqualTo("2"));
        Assert.That(
            result.Error.FailedItems.Where(f => !f.IsCause).Select(f => f.Reason),
            Has.All.StartWith("Not deleted"));

        // the transaction is atomic: nothing was deleted
        Assert.That(await EntityExistsAsync("pk1", "1"), Is.True);
        Assert.That(await EntityExistsAsync("pk1", "3"), Is.True);
    }

    [Test]
    public async Task DeleteEntities_BatchSizeOne_ContinueOnFailure_OnlyMissingEntityFails()
    {
        await SeedEntityAsync("pk1", "1");
        await SeedEntityAsync("pk1", "3");

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1" },
            new { PartitionKey = "pk1", RowKey = "2" }, // missing
            new { PartitionKey = "pk1", RowKey = "3" });

        var options = DefaultOptions();
        options.BatchSize = 1;
        options.ContinueOnFailure = true;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SucceededItems.Select(x => x.RowKey), Is.EquivalentTo(new[] { "1", "3" }));
        Assert.That(result.Error.Message, Does.Contain("1 of 3 entities failed"));
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(1));
        Assert.That(result.Error.FailedItems[0].RowKey, Is.EqualTo("2"));
        Assert.That(result.Error.FailedItems[0].IsCause, Is.True);

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.False);
        Assert.That(await EntityExistsAsync("pk1", "3"), Is.False);
    }

    [Test]
    public async Task DeleteEntities_WithContinueOnFailure_FailedBatchDoesNotBlockOtherPartitions()
    {
        await SeedEntityAsync("pk1", "1");
        await SeedEntityAsync("pk2", "1");

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1" },
            new { PartitionKey = "pk1", RowKey = "2" }, // missing
            new { PartitionKey = "pk2", RowKey = "1" });

        var options = DefaultOptions();
        options.ContinueOnFailure = true;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(
            result.SucceededItems.Select(x => $"{x.PartitionKey}/{x.RowKey}"),
            Is.EquivalentTo(new[] { "pk2/1" }));
        Assert.That(result.Error.Message, Does.Contain("2 of 3 entities failed"));
        Assert.That(result.Error.FailedItems.Select(f => f.RowKey), Is.EquivalentTo(new[] { "1", "2" }));
        Assert.That(result.Error.FailedItems.Single(f => f.IsCause).RowKey, Is.EqualTo("2"));

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.True);
        Assert.That(await EntityExistsAsync("pk2", "1"), Is.False);
    }

    [Test]
    public async Task DeleteEntities_BatchSizeSmallerThanPartition_FailedChunkDoesNotBlockNextChunk()
    {
        await SeedEntityAsync("pk1", "1");
        await SeedEntityAsync("pk1", "3");

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1" },
            new { PartitionKey = "pk1", RowKey = "2" }, // missing
            new { PartitionKey = "pk1", RowKey = "3" });

        var options = DefaultOptions();
        options.BatchSize = 2; // transactions: [1, 2] and [3]
        options.ContinueOnFailure = true;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.SucceededItems.Select(x => x.RowKey), Is.EquivalentTo(new[] { "3" }));
        Assert.That(result.Error.Message, Does.Contain("2 of 3 entities failed"));
        Assert.That(result.Error.FailedItems.Single(i => i.IsCause).RowKey, Is.EqualTo("2"));
        Assert.That(result.Error.FailedItems.Single(i => !i.IsCause).Reason, Does.StartWith("Not deleted"));

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.True);
        Assert.That(await EntityExistsAsync("pk1", "3"), Is.False);
    }

    [Test]
    public async Task DeleteEntities_WithoutContinueOnFailure_StopsAtFirstFailedBatchAndReturnsProgress()
    {
        await SeedEntityAsync("pk1", "1");
        await SeedEntityAsync("pk1", "2");
        await SeedEntityAsync("pk2", "2");
        await SeedEntityAsync("pk3", "1");

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1" },
            new { PartitionKey = "pk1", RowKey = "2" },
            new { PartitionKey = "pk2", RowKey = "1" }, // missing
            new { PartitionKey = "pk2", RowKey = "2" },
            new { PartitionKey = "pk3", RowKey = "1" });

        var options = DefaultOptions();
        options.ContinueOnFailure = false;
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(
            result.SucceededItems.Select(x => $"{x.PartitionKey}/{x.RowKey}"),
            Is.EquivalentTo(new[] { "pk1/1", "pk1/2" }));
        Assert.That(
            result.Error.FailedItems.Select(f => $"{f.PartitionKey}/{f.RowKey}"),
            Is.EquivalentTo(new[] { "pk2/1", "pk2/2" }));
        Assert.That(result.Error.FailedItems.Single(f => f.IsCause).RowKey, Is.EqualTo("1"));

        // pk3 was not attempted: it is in neither list and still exists
        Assert.That(await EntityExistsAsync("pk3", "1"), Is.True);
        Assert.That(await EntityExistsAsync("pk2", "2"), Is.True);
    }

    [Test]
    public async Task DeleteEntities_WithThrowErrorOnFailureTrue_ShouldThrowFailureSummaryWhenPartiallyDeleted()
    {
        await SeedEntityAsync("pk1", "1");
        await SeedEntityAsync("pk1", "2");
        await SeedEntityAsync("pk2", "2");
        await SeedEntityAsync("pk3", "1");

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1" },
            new { PartitionKey = "pk1", RowKey = "2" },
            new { PartitionKey = "pk2", RowKey = "1" }, // missing
            new { PartitionKey = "pk2", RowKey = "2" },
            new { PartitionKey = "pk3", RowKey = "1" });

        var options = DefaultOptions();
        options.ContinueOnFailure = false;
        options.ThrowErrorOnFailure = true;

        var ex = Assert.Throws<Exception>(() =>
           AzureTableStorage.DeleteEntities(input, DefaultConnectionStringConnection(), options, CancellationToken.None).GetAwaiter().GetResult());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Does.Contain("Deleted before the failure: 2"));
        Assert.That(ex.Message, Does.Contain("Not deleted: 2. Not attempted: 1."));
        Assert.That(ex.Message, Does.Contain("Cause: PartitionKey 'pk2', RowKey '1'"));

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.False);
        Assert.That(await EntityExistsAsync("pk1", "2"), Is.False);
        Assert.That(await EntityExistsAsync("pk2", "2"), Is.True);
        Assert.That(await EntityExistsAsync("pk3", "1"), Is.True);
    }

    [Test]
    public async Task DeleteEntities_MissingTable_MarksNoEntityAsCause()
    {
        var input = new Input
        {
            TableName = $"missingtable{Guid.NewGuid():N}"[..30],
            Entities = ToEntitiesJson(
                new { PartitionKey = "pk1", RowKey = "1" },
                new { PartitionKey = "pk1", RowKey = "2" }),
        };

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.FailedItems, Has.Count.EqualTo(2));
        Assert.That(result.Error.FailedItems.Any(f => f.IsCause), Is.False);
    }

    [Test]
    public async Task DeleteEntities_WhenErrorOccursBeforeLoop_ShouldReturnEmptyProgressLists()
    {
        var input = BuildInput(new { PartitionKey = "pk1" });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("must have non-empty string PartitionKey and RowKey"));
        Assert.That(result.Error.AdditionalInfo, Is.Not.Null);
        Assert.That(result.Error.FailedItems, Is.Empty);
    }

    [Test]
    public async Task DeleteEntities_WithSasToken_ShouldDeleteEntity()
    {
        await SeedEntityAsync("pk1", "1");

        var result = await AzureTableStorage.DeleteEntities(
            BuildInput(new { PartitionKey = "pk1", RowKey = "1" }),
            DefaultSasTokenConnection(),
            DefaultOptions(),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(await EntityExistsAsync("pk1", "1"), Is.False);
    }

    [Test]
    public async Task DeleteEntities_DuplicateKeyInOneBatch_FailsAndDeletesNothing()
    {
        await SeedEntityAsync("pk1", "1");
        await SeedEntityAsync("pk1", "2");

        var input = BuildInput(
            new { PartitionKey = "pk1", RowKey = "1" },
            new { PartitionKey = "pk1", RowKey = "2" },
            new { PartitionKey = "pk1", RowKey = "1" });

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.DeleteEntities(
            input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        TestContext.WriteLine(string.Join(" | ", result.Error.FailedItems.Select(f => f.Reason)));

        Assert.That(result.Success, Is.False);
        Assert.That(result.SucceededItems, Is.Empty);
        Assert.That(result.Error.FailedItems.Select(f => f.Reason), Has.Some.Contain("400"));

        Assert.That(await EntityExistsAsync("pk1", "1"), Is.True);
        Assert.That(await EntityExistsAsync("pk1", "2"), Is.True);
    }

    private static Dictionary<string, object> Key(string pk, string rk, string etag) => new()
    {
        ["PartitionKey"] = pk,
        ["RowKey"] = rk,
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

    private async Task SeedEntityAsync(string pk, string rk)
    {
        var entity = new TableEntity(pk, rk) { ["Name"] = "Seed" };
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

    private async Task<string> GetETagAsync(string partitionKey, string rowKey) =>
        (await CreateTableClient().GetEntityAsync<TableEntity>(partitionKey, rowKey)).Value.ETag.ToString();

    private async Task<bool> EntityExistsAsync(string partitionKey, string rowKey)
    {
        try
        {
            await CreateTableClient().GetEntityAsync<TableEntity>(partitionKey, rowKey);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }
}