using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.InsertEntities.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary
    /// >Name of the table. The table must already exist.
    /// </summary>
    /// <example>orders</example>
    [Required]
    [DefaultValue("")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of entities. Every entity must be a flat JSON object with PartitionKey and RowKey (non-empty strings).
    /// Other properties can be strings, numbers or booleans. Nested objects and arrays are not supported,
    /// and null values are skipped. Limits: 1 MB per entity, 64 KiB per property, 255 properties.
    /// Entities are identified in the result by PartitionKey and RowKey.
    /// </summary>
    /// <example>[ { "PartitionKey": "pk1", "RowKey": "1", "Name": "Anna", "Age": 30 } ]</example>
    [DisplayFormat(DataFormatString = "Json")]
    [Required]
    public string Entities { get; set; } = string.Empty;
}
