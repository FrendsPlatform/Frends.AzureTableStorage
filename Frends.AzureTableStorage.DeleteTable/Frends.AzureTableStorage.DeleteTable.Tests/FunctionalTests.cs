using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Frends.AzureTableStorage.DeleteTable.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.DeleteTable.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private string tableName;
    private Input input;

    [SetUp]
    public async Task SetUp()
    {
        tableName = $"TestTable{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        input = new Input { TableName = tableName };

        if (!string.IsNullOrEmpty(ConnectionString))
        {
            var serviceClient = new TableServiceClient(ConnectionString);
            await serviceClient.CreateTableIfNotExistsAsync(tableName);
        }
    }

    [TearDown]
    public async Task TearDown()
    {
        if (!string.IsNullOrEmpty(ConnectionString))
        {
            try
            {
                var serviceClient = new TableServiceClient(ConnectionString);
                await serviceClient.DeleteTableAsync(tableName);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task DeleteTable_WithConnectionString_ShouldDeleteTable()
    {
        var result = await AzureTableStorage.DeleteTable(input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TableName, Is.EqualTo(input.TableName));
        Assert.That(result.Deleted, Is.True);
        Assert.That(result.TableUri, Is.Not.Empty);
        Assert.That(result.Error, Is.Null);

        var serviceClient = new TableServiceClient(ConnectionString);
        Assert.That(await TableExistsAsync(serviceClient, tableName), Is.False);
    }

    [Test]
    public async Task DeleteTable_WhenTableDoesNotExist_ShouldReturnDeletedFalse()
    {
        var nonExistentTable = $"NonExistent{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var testInput = new Input { TableName = nonExistentTable };

        var result = await AzureTableStorage.DeleteTable(testInput, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TableName, Is.EqualTo(nonExistentTable));
        Assert.That(result.Deleted, Is.False);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task DeleteTable_WithFailIfTableNotExists_ShouldReturnError()
    {
        var nonExistentTable = $"NonExistent{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var testInput = new Input { TableName = nonExistentTable };

        var options = DefaultOptions();
        options.FailIfTableNotExists = true;
        options.ThrowErrorOnFailure = false;
        var result = await AzureTableStorage.DeleteTable(testInput, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("does not exist"));
    }

    [Test]
    public async Task DeleteTable_WithOAuth2_ShouldDeleteTable()
    {
        var result = await AzureTableStorage.DeleteTable(input, DefaultOAuth2Connection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TableName, Is.EqualTo(input.TableName));
        Assert.That(result.Deleted, Is.True);
        Assert.That(result.TableUri, Is.Not.Empty);
        Assert.That(result.Error, Is.Null);

        var serviceClient = new TableServiceClient(ConnectionString);
        Assert.That(await TableExistsAsync(serviceClient, tableName), Is.False);
    }

    [Test]
    public async Task DeleteTable_WithSasToken_ShouldDeleteTable()
    {
        var result = await AzureTableStorage.DeleteTable(input, DefaultSasTokenConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TableName, Is.EqualTo(input.TableName));
        Assert.That(result.Deleted, Is.True);
        Assert.That(result.TableUri, Is.Not.Empty);
        Assert.That(result.Error, Is.Null);

        var serviceClient = new TableServiceClient(ConnectionString);
        Assert.That(await TableExistsAsync(serviceClient, tableName), Is.False);
    }

    private static async Task<bool> TableExistsAsync(TableServiceClient serviceClient, string tableName)
    {
        await foreach (var table in serviceClient.QueryAsync(filter: $"TableName eq '{tableName}'"))
        {
            return true;
        }

        return false;
    }
}
