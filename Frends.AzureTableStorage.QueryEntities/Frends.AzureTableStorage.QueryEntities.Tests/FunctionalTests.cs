using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.QueryEntities.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.QueryEntities.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private string tableName;
    private TableClient tableClient;

    [SetUp]
    public async Task SetUp()
    {
        Assume.That(ConnectionString, Is.Not.Empty, "Connection string is required for functional tests.");

        tableName = $"queryentities{Guid.NewGuid():N}"[..30];
        var serviceClient = new TableServiceClient(ConnectionString);
        tableClient = serviceClient.GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync();
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
        await SeedEntityAsync("pk1", "1", "Anna", 10);
        await SeedEntityAsync("pk1", "2", "Bob", 20);
        await SeedEntityAsync("pk2", "3", "Cara", 30);

        var input = new Input
        {
            TableName = tableName,
            Filter = "PartitionKey eq 'pk1' and RowKey ge '2'",
            Select = ["Name"],
            Top = 10,
        };

        var result = await AzureTableStorage.QueryEntities(
            input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That((string)result.Entities[0]["PartitionKey"], Is.EqualTo("pk1"));
        Assert.That((string)result.Entities[0]["RowKey"], Is.EqualTo("2"));
        Assert.That((string)result.Entities[0]["Name"], Is.EqualTo("Bob"));
        Assert.That(result.Entities[0]["Price"], Is.Null);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ShouldResumeUntilTheLastPage()
    {
        for (var i = 1; i <= 5; i++)
            await SeedEntityAsync("pk1", i.ToString(), $"Name{i}", i);

        var input = new Input
        {
            TableName = tableName,
            Filter = "PartitionKey eq 'pk1'",
            Select = ["Name"],
            Top = 2,
        };

        var rowKeys = new List<string>();
        for (var pageNumber = 0; pageNumber < 3; pageNumber++)
        {
            var result = await AzureTableStorage.QueryEntities(
                input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Count, Is.EqualTo(pageNumber < 2 ? 2 : 1));
            Assert.That(result.Entities, Has.Count.EqualTo(result.Count));
            Assert.That(result.Error, Is.Null);

            rowKeys.AddRange(result.Entities.Select(entity => (string)entity["RowKey"]));
            input.ContinuationToken = result.ContinuationToken;

            Assert.That(result.ContinuationToken, pageNumber < 2 ? Is.Not.Empty : Is.Null);
        }

        Assert.That(rowKeys, Is.EqualTo(new[] { "1", "2", "3", "4", "5" }));
    }

    [Test]
    public async Task ShouldReturnEmptyPageWhenNothingMatches()
    {
        var result = await AzureTableStorage.QueryEntities(
            new Input { TableName = tableName, Filter = "PartitionKey eq 'notfound'" },
            DefaultConnectionStringConnection(),
            DefaultOptions(),
            CancellationToken.None);

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

        var result = await AzureTableStorage.QueryEntities(
            new Input { TableName = $"missingtable{Guid.NewGuid():N}"[..30] },
            DefaultConnectionStringConnection(),
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Entities, Is.Null);
        Assert.That(result.Count, Is.Zero);
        Assert.That(result.ContinuationToken, Is.Null);
        Assert.That(result.Error.AdditionalInfo, Is.TypeOf<RequestFailedException>());
    }

    [Test]
    public async Task ShouldReturnFailureForInvalidODataFilter()
    {
        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.QueryEntities(
            new Input { TableName = tableName, Filter = "PartitionKey eq 'unterminated" },
            DefaultConnectionStringConnection(),
            options,
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.AdditionalInfo, Is.TypeOf<RequestFailedException>());
    }

    [Test]
    public async Task ShouldQueryWithSasToken()
    {
        Assume.That(AccountName, Is.Not.Empty, "Storage account name is required for the SAS test.");
        Assume.That(SasToken, Is.Not.Empty, "SAS token is required for the SAS test.");
        await SeedEntityAsync("pk1", "1", "Anna", 10);

        var result = await AzureTableStorage.QueryEntities(
            new Input { TableName = tableName, Filter = "PartitionKey eq 'pk1'" },
            DefaultSasTokenConnection(),
            DefaultOptions(),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ShouldQueryWithOAuth2()
    {
        Assume.That(AccountName, Is.Not.Empty, "Storage account name is required for the OAuth2 test.");
        Assume.That(TenantId, Is.Not.Empty, "Tenant ID is required for the OAuth2 test.");
        Assume.That(ClientId, Is.Not.Empty, "Client ID is required for the OAuth2 test.");
        Assume.That(ClientSecret, Is.Not.Empty, "Client secret is required for the OAuth2 test.");
        await SeedEntityAsync("pk1", "1", "Anna", 10);

        var result = await AzureTableStorage.QueryEntities(
            new Input { TableName = tableName, Filter = "PartitionKey eq 'pk1'" },
            DefaultOAuth2Connection(),
            DefaultOptions(),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(1));
    }

    private async Task SeedEntityAsync(string partitionKey, string rowKey, string name, int price)
    {
        await tableClient.AddEntityAsync(new TableEntity(partitionKey, rowKey)
        {
            ["Name"] = name,
            ["Price"] = price,
        });
    }
}
