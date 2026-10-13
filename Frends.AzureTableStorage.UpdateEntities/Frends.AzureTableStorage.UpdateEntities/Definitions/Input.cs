using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.UpdateEntities.Definitions;

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
    /// JSON array of entities to update. Every entity must be a flat JSON object with PartitionKey and RowKey
    /// (non-empty strings). Other properties can be strings, numbers or booleans; nested objects and arrays
    /// are not supported, and null values are skipped.
    /// Optional "odata.etag" from a previous read: if set, the entity is updated only if it has not changed
    /// since (otherwise 412); if not set, it is updated unconditionally.
    /// The entity must exist, otherwise the update fails with 404.
    /// Limits: 1 MB per entity, 64 KiB per property, 255 properties.
    /// </summary>
    /// <example>[ { "PartitionKey": "pk1", "RowKey": "1", "Name": "Anna", "odata.etag": "W/\"datetime'2024-05-01T10%3A15%3A30.1234567Z'\"" } ]</example>
    [DisplayFormat(DataFormatString = "Json")]
    [Required]
    public string Entities { get; set; } = string.Empty;
}
