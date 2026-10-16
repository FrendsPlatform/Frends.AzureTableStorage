using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.DeleteEntities.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Name of the table. The table must already exist.
    /// </summary>
    /// <example>orders</example>
    [Required]
    [DefaultValue("")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of the entities to delete, identified by PartitionKey and RowKey (non-empty strings).
    /// Optional "odata.etag" from a previous read: if set, the entity is deleted only if it has not changed
    /// since (otherwise 412); if not set, it is deleted unconditionally.
    /// The entity must exist, otherwise the delete fails with 404.
    /// Other properties are not allowed.
    /// </summary>
    /// <example>[ { "PartitionKey": "pk1", "RowKey": "1" }, { "PartitionKey": "pk1", "RowKey": "2", "odata.etag": "W/\"datetime'2024-05-01T10%3A15%3A30.1234567Z'\"" } ]</example>
    [DisplayFormat(DataFormatString = "Json")]
    [Required]
    public string Entities { get; set; } = string.Empty;
}
