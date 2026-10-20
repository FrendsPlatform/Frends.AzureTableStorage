namespace Frends.AzureTableStorage.DeleteEntities.Definitions;

/// <summary>
/// Entity that was not deleted.
/// </summary>
public class FailedItem
{
    /// <summary>
    /// PartitionKey of the entity.
    /// </summary>
    /// <example>pk1</example>
    public string PartitionKey { get; set; }

    /// <summary>
    /// RowKey of the entity.
    /// </summary>
    /// <example>3</example>
    public string RowKey { get; set; }

    /// <summary>
    /// True if this entity caused the failure (e.g. it does not exist, it was modified since it was read
    /// and its odata.etag is outdated, or it is invalid) and needs attention.
    /// False if it was not deleted only because another entity in the same transaction failed,
    /// or because of a failure not related to a specific entity (e.g. missing table, authorization).
    /// Entities with IsCause=false can be sent again without changes once the cause is fixed.
    /// </summary>
    /// <example>true</example>
    public bool IsCause { get; set; }

    /// <summary>
    /// Why the entity was not deleted.
    /// </summary>
    /// <example>Status 404 ResourceNotFound: The specified resource does not exist.</example>
    public string Reason { get; set; }
}