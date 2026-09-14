namespace Frends.AzureTableStorage.DeleteTable.Definitions;

/// <summary>
/// Connection method for connecting to Azure Table Storage.
/// </summary>
public enum ConnectionMethod
{
    /// <summary>
    /// Connection string.
    /// </summary>
    ConnectionString = 1,

    /// <summary>
    /// OAuth2 authentication.
    /// </summary>
    OAuth2 = 2,

    /// <summary>
    /// Shared Access Signature token.
    /// </summary>
    SasToken = 3,

    /// <summary>
    /// Azure Arc Managed Identity.
    /// </summary>
    ArcManagedIdentity = 4,

    /// <summary>
    /// Azure Arc Managed Identity Cross Tenant.
    /// </summary>
    ArcManagedIdentityCrossTenant = 5,
}
