namespace Frends.AzureTableStorage.UpdateEntities.Definitions;

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

/// <summary>
/// Defines how existing entities are updated. The entity must already exist, otherwise the update fails (404).
/// </summary>
public enum UpdateMode
{
    /// <summary>
    /// Merges the input properties into the existing entity: properties present in the input are overwritten,
    /// properties not present in the input are left unchanged. A property cannot be removed in this mode.
    /// </summary>
    Merge = 1,

    /// <summary>
    /// Replaces the whole entity with the input: properties not present in the input are removed.
    /// </summary>
    Replace = 2,
}