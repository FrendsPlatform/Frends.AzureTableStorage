using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Frends.AzureTableStorage.QueryEntities.Definitions;

namespace Frends.AzureTableStorage.QueryEntities.Helpers;

/// <summary>
/// Connection handler to connect with Azure Table Storage.
/// </summary>
public static class ConnectionHandler
{
    /// <summary>
    /// Get Table Service Client.
    /// </summary>
    /// <param name="connection">Connection task parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>TableServiceClient object.</returns>
    public static TableServiceClient GetTableServiceClient(
        Connection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            return connection.ConnectionMethod switch
            {
                ConnectionMethod.ConnectionString => new TableServiceClient(connection.ConnectionString),
                ConnectionMethod.SasToken => new TableServiceClient(GetUri(connection.StorageAccountName, connection.SasToken)),
                ConnectionMethod.OAuth2 => new TableServiceClient(
                    GetUri(connection.StorageAccountName),
                    new ClientSecretCredential(
                        connection.TenantId,
                        connection.ApplicationId,
                        connection.ClientSecret,
                        new ClientSecretCredentialOptions())),
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

    [ExcludeFromCodeCoverage(Justification = "We do not have environment prepared to test this connection")]
    private static TableServiceClient GetTableServiceClientWithArcManagedIdentity(Connection connection)
    {
        var credentials = new ManagedIdentityCredential();
        return new TableServiceClient(GetUri(connection.StorageAccountName), credentials);
    }

    [ExcludeFromCodeCoverage(Justification = "We do not have environment prepared to test this connection")]
    private static TableServiceClient GetTableServiceClientWithArcManagedIdentityCrossTenant(
        Connection connection,
        CancellationToken cancellationToken)
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

    private static Uri GetUri(string storageAccountName, string sasToken = null)
    {
        if (storageAccountName is null
            || storageAccountName.Length < 3
            || storageAccountName.Length > 24
            || !storageAccountName.All(c => char.IsLower(c) || char.IsDigit(c)))
            throw new ValidationException("StorageAccountName must be 3-24 lowercase letters or digits.");

        var normalizedSasToken = sasToken?.TrimStart('?');
        return sasToken is null
            ? new Uri($"https://{storageAccountName}.table.core.windows.net")
            : new Uri($"https://{storageAccountName}.table.core.windows.net?{normalizedSasToken}");
    }
}
