using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Frends.AzureTableStorage.CreateTable.Definitions;

namespace Frends.AzureTableStorage.CreateTable.Helpers;

/// <summary>
/// Connection handler to connect with Azure Table Storage.
/// </summary>
public static class ConnectionHandler
{
    /// <summary>
    /// Get Table Service Client.
    /// </summary>
    /// <param name="connection">Connection task parameters</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns>TableServiceClient object</returns>
    public static TableServiceClient GetTableServiceClient(
        Connection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            return connection.ConnectionMethod switch
            {
                ConnectionMethod.ConnectionString => GetTableServiceClientWithConnectionString(connection),
                ConnectionMethod.SasToken => GetTableServiceClientWithSasToken(connection),
                ConnectionMethod.OAuth2 => GetTableServiceClientWithOAuth2(connection),
                ConnectionMethod.ArcManagedIdentity => GetTableServiceClientWithArcManagedIdentity(connection),
                ConnectionMethod.ArcManagedIdentityCrossTenant => GetTableServiceClientWithArcManagedIdentityCrossTenant(
                    connection,
                    cancellationToken),
                _ => throw new NotSupportedException(),
            };
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"GetTableServiceClient error: {ex.Message}", ex);
        }
    }

    private static TableServiceClient GetTableServiceClientWithConnectionString(Connection connection)
    {
        return new TableServiceClient(connection.ConnectionString);
    }

    private static TableServiceClient GetTableServiceClientWithSasToken(Connection connection)
    {
        return new TableServiceClient(GetUri(connection.StorageAccountName, connection.SasToken));
    }

    private static TableServiceClient GetTableServiceClientWithOAuth2(Connection connection)
    {
        return new TableServiceClient(
            GetUri(connection.StorageAccountName),
            new ClientSecretCredential(
                connection.TenantId,
                connection.ApplicationId,
                connection.ClientSecret,
                new ClientSecretCredentialOptions()));
    }

    [ExcludeFromCodeCoverage(Justification = "We do not have environment prepared to test this connection")]
    private static TableServiceClient GetTableServiceClientWithArcManagedIdentity(Connection connection)
    {
        {
            var credentials = new ManagedIdentityCredential();

            return new TableServiceClient(GetUri(connection.StorageAccountName), credentials);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "We do not have environment prepared to test this connection")]
    private static TableServiceClient GetTableServiceClientWithArcManagedIdentityCrossTenant(
        Connection connection,
        CancellationToken cancellationToken)
    {
        {
            var credentials = new ManagedIdentityCredential();
            ClientAssertionCredential assertion = new(
                connection.TargetTenantId,
                connection.TargetClientId,
                async _ =>
                {
                    var tokenRequestContext = new TokenRequestContext(connection.Scopes);
                    var accessToken = await credentials
                        .GetTokenAsync(tokenRequestContext, cancellationToken).ConfigureAwait(false);

                    return accessToken.Token;
                });

            return new TableServiceClient(GetUri(connection.StorageAccountName), assertion);
        }
    }

    private static Uri GetUri(string storageAccountName, string sasToken = null)
    {
        var normalizedSasToken = sasToken?.TrimStart('?');
        return sasToken is null
            ? new Uri($"https://{storageAccountName}.table.core.windows.net")
            : new Uri($"https://{storageAccountName}.table.core.windows.net?{normalizedSasToken}");
    }
}
