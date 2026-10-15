using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.GetEntity.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>Name of the table.</summary>
    /// <example>orders</example>
    [Required]
    public string TableName { get; set; } = string.Empty;

    /// <summary>PartitionKey of the entity.</summary>
    /// <example>pk1</example>
    [Required]
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>RowKey of the entity.</summary>
    /// <example>1</example>
    [Required]
    public string RowKey { get; set; } = string.Empty;
}
