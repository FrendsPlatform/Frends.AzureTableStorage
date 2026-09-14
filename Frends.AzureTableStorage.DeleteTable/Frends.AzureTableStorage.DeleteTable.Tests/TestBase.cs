using System;
using dotenv.net;
using Frends.AzureTableStorage.DeleteTable.Definitions;

namespace Frends.AzureTableStorage.DeleteTable.Tests;

internal abstract class TestBase
{
    internal TestBase()
    {
        DotEnv.Load();
        ConnectionString = GetEnvVarOrDefault("Frends_AzureTableStorage_ConnString", string.Empty);
        AccountName = GetEnvVarOrDefault("Frends_AzureTableStorage_AccountName", string.Empty);
        TenantId = GetEnvVarOrDefault("Frends_AzureTableStorage_TenantID", string.Empty);
        ClientId = GetEnvVarOrDefault("Frends_AzureTableStorage_ClientID", string.Empty);
        ClientSecret = GetEnvVarOrDefault("Frends_AzureTableStorage_ClientSecret", string.Empty);
        SasToken = GetEnvVarOrDefault("Frends_AzureTableStorage_SasToken", string.Empty);
    }

    protected string ConnectionString { get; set; }

    protected string AccountName { get; set; }

    protected string TenantId { get; set; }

    protected string ClientId { get; set; }

    protected string ClientSecret { get; set; }

    protected string SasToken { get; set; }

    protected static Options DefaultOptions() => new()
    {
        ThrowErrorOnFailure = true,
        ErrorMessageOnFailure = string.Empty,
        FailIfTableNotExists = false,
    };

    protected Connection DefaultConnectionStringConnection() => new()
    {
        ConnectionMethod = ConnectionMethod.ConnectionString,
        ConnectionString = ConnectionString,
    };

    protected Connection DefaultOAuth2Connection() => new()
    {
        ConnectionMethod = ConnectionMethod.OAuth2,
        StorageAccountName = AccountName,
        TenantId = TenantId,
        ApplicationId = ClientId,
        ClientSecret = ClientSecret,
    };

    protected Connection DefaultSasTokenConnection() => new()
    {
        ConnectionMethod = ConnectionMethod.SasToken,
        StorageAccountName = AccountName,
        SasToken = SasToken,
    };

    protected Connection DefaultArcManagedIdentityConnection() => new()
    {
        ConnectionMethod = ConnectionMethod.ArcManagedIdentity,
        StorageAccountName = AccountName,
    };

    private static string GetEnvVarOrDefault(string name, string defaultValue) =>
        Environment.GetEnvironmentVariable(name) ?? defaultValue;
}
