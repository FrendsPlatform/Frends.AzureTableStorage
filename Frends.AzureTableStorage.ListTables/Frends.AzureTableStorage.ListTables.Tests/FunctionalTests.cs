using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Frends.AzureTableStorage.ListTables.Definitions;
using NUnit.Framework;

namespace Frends.AzureTableStorage.ListTables.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    private string prefix;
    private string firstTableName;
    private string secondTableName;
    private string thirdTableName;

    [SetUp]
    public async Task SetUp()
    {
        Assume.That(ConnectionString, Is.Not.Empty, "Connection string is required for functional tests.");

        prefix = $"frendstest{DateTime.UtcNow:yyMMddhhmmssfff}";
        firstTableName = $"{prefix}a";
        secondTableName = $"{prefix}b";
        thirdTableName = $"{prefix}c";

        var serviceClient = new TableServiceClient(ConnectionString);
        await serviceClient.CreateTableIfNotExistsAsync(firstTableName);
        await serviceClient.CreateTableIfNotExistsAsync(secondTableName);
        await serviceClient.CreateTableIfNotExistsAsync(thirdTableName);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (string.IsNullOrEmpty(ConnectionString)) return;

        var serviceClient = new TableServiceClient(ConnectionString);

        foreach (var tableName in new[] { firstTableName, secondTableName, thirdTableName })
        {
            if (string.IsNullOrEmpty(tableName)) continue;
            try
            {
                await serviceClient.DeleteTableAsync(tableName);
            }
            catch
            {
                // ignored
            }
        }
    }

    [Test]
    public async Task ListTables_WithConnectionString_ShouldReturnMatchingTables()
    {
        var input = new Input
        {
            TableNamePrefix = prefix,
            MaxResults = 0,
        };

        var result = await AzureTableStorage.ListTables(input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Tables, Is.Not.Null);
        Assert.That(result.Tables.Count, Is.EqualTo(3));
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Tables.Exists(table => table.Name == firstTableName), Is.True);
        Assert.That(result.Tables.Exists(table => table.Name == secondTableName), Is.True);
        Assert.That(result.Tables.Exists(table => table.Name == thirdTableName), Is.True);
    }

    [Test]
    public async Task ListTables_WithMaxResults_ShouldReturnLimitedNumberOfTables()
    {
        var input = new Input
        {
            TableNamePrefix = prefix,
            MaxResults = 2,
        };

        var result = await AzureTableStorage.ListTables(input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Tables.Count, Is.EqualTo(2));
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ListTables_WithOAuth2_ShouldReturnMatchingTables()
    {
        Assume.That(AccountName, Is.Not.Empty, "Storage account name is required for OAuth2 test.");
        Assume.That(TenantId, Is.Not.Empty, "TenantId is required for OAuth2 test.");
        Assume.That(ClientId, Is.Not.Empty, "ClientId is required for OAuth2 test.");
        Assume.That(ClientSecret, Is.Not.Empty, "ClientSecret is required for OAuth2 test.");

        var input = new Input
        {
            TableNamePrefix = prefix,
            MaxResults = 0,
        };

        var result = await AzureTableStorage.ListTables(input, DefaultOAuth2Connection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Tables, Is.Not.Null);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ListTables_WithSasToken_ShouldReturnMatchingTables()
    {
        Assume.That(AccountName, Is.Not.Empty, "Storage account name is required for SAS test.");
        Assume.That(SasToken, Is.Not.Empty, "SAS token is required for SAS test.");

        var input = new Input
        {
            TableNamePrefix = prefix,
            MaxResults = 0,
        };

        var result = await AzureTableStorage.ListTables(input, DefaultSasTokenConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result.Tables, Is.Not.Null);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ListTables_WithNonMatchingPrefix_ShouldReturnEmptyList()
    {
        var input = new Input
        {
            TableNamePrefix = $"{prefix}xyz",
            MaxResults = 0,
        };

        var result = await AzureTableStorage.ListTables(input, DefaultConnectionStringConnection(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Count, Is.EqualTo(0));
        Assert.That(result.Tables, Is.Empty);
        Assert.That(result.Error, Is.Null);
    }

    [Test]
    public async Task ListTables_WithInvalidSasToken_ShouldReturnErrorFromQuery()
    {
        Assume.That(AccountName, Is.Not.Empty, "Storage account name is required for invalid SAS test.");

        var input = new Input
        {
            TableNamePrefix = prefix,
            MaxResults = 1,
        };

        var connection = new Connection
        {
            ConnectionMethod = ConnectionMethod.SasToken,
            StorageAccountName = AccountName,
            SasToken = "sv=2023-11-03&sp=rl&sig=invalidsignature",
        };

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = await AzureTableStorage.ListTables(input, connection, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tables, Is.Null);
        Assert.That(result.Count, Is.EqualTo(0));
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Message, Is.Not.Empty);
    }
}
