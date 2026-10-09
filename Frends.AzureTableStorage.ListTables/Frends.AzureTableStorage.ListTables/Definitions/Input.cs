using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.ListTables.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Optional prefix to filter tables by name. Leave empty to list all tables.
    /// </summary>
    /// <example>orders</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string TableNamePrefix { get; set; }

    /// <summary>
    /// Maximum number of tables to return. 0 means no limit.
    /// </summary>
    /// <example>100</example>
    [Range(0, int.MaxValue, ErrorMessage = "MaxResults must be greater than or equal to 0.")]
    [DefaultValue(0)]
    public int MaxResults { get; set; }
}
