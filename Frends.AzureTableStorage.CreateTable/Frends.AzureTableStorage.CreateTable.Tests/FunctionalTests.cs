using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Frends.AzureTableStorage.CreateTable.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.CreateTable.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private string tableName;
    private Input input;

    [SetUp]
    public void SetUp()
    {
        tableName = $"TestTable{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        input = new Input { TableName = tableName };
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
    public async Task CreateTable_WithConnectionString_ShouldCreateTable()
    {
        var result = await AzureTableStorage.CreateTable(input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TableName, Is.EqualTo(input.TableName));
        Assert.That(result.Created, Is.True);
        Assert.That(result.TableUri, Is.Not.Empty);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task CreateTable_WhenTableAlreadyExists_ShouldReturnCreatedFalse()
    {
        // Create table first time
        await AzureTableStorage.CreateTable(input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        // Create same table again
        var result = await AzureTableStorage.CreateTable(input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TableName, Is.EqualTo(input.TableName));
        Assert.That(result.Created, Is.False);
        Assert.That(result.TableUri, Is.Not.Empty);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task CreateTable_WithThrowErrorIfExists_ShouldReturnError()
    {
        // Create table first time
        await AzureTableStorage.CreateTable(input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        // Try to create same table with ThrowErrorIfExists = true
        var options = DefaultOptions();
        options.FailIfTableExists = true;
        options.ThrowErrorOnFailure = false;
        var result = await AzureTableStorage.CreateTable(input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("already exists"));
    }

    [Test]
    public async Task CreateTable_WithInvalidTableName_ShouldReturnError()
    {
        var input = new Input { TableName = "123Invalid" }; // Invalid: starts with number
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.CreateTable(input, DefaultConnectionStringConnection(), options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Does.Contain("invalid characters"));
    }

    [Test]
    public async Task CreateTable_WithOAuth2_ShouldCreateTable()
    {
        var result = await AzureTableStorage.CreateTable(input, DefaultOAuth2Connection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TableName, Is.EqualTo(input.TableName));
        Assert.That(result.Created, Is.True);
        Assert.That(result.TableUri, Is.Not.Empty);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task CreateTable_WithSasToken_ShouldCreateTable()
    {
        var result = await AzureTableStorage.CreateTable(input, DefaultSasTokenConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TableName, Is.EqualTo(input.TableName));
        Assert.That(result.Created, Is.True);
        Assert.That(result.TableUri, Is.Not.Empty);
        Assert.That(result.Error, Is.Null);
    }
}
