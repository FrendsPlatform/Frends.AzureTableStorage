namespace Frends.AzureTableStorage.InsertEntities.Definitions;

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
/// Defines how entities are written to the table.
/// </summary>
public enum InsertMode
{
    /// <summary>
    /// Inserts new entities only. If an entity with the same PartitionKey and RowKey already exists,
    /// the operation fails.
    /// </summary>
    Add,

    /// <summary>
    /// Inserts the entity if it does not exist. If it exists, the properties from the input are merged
    /// into the existing entity: properties present in the input are overwritten, properties not present
    /// in the input are left unchanged.
    /// </summary>
    UpsertMerge,

    /// <summary>
    /// Inserts the entity if it does not exist. If it exists, the whole entity is replaced with the input:
    /// properties not present in the input are removed from the stored entity.
    /// </summary>
    UpsertReplace,
}
