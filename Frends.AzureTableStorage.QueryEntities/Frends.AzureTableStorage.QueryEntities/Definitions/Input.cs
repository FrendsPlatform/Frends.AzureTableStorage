using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Frends.AzureTableStorage.QueryEntities.Attributes;

namespace Frends.AzureTableStorage.QueryEntities.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Name of the table. The table must already exist. Table names must start with a letter
    /// and contain 3-63 alphanumeric characters.
    /// </summary>
    /// <example>orders</example>
    [Required]
    [DefaultValue("")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Optional OData filter. Leave empty to query all entities in the table.
    /// Use the same filter on each page when passing a continuation token.
    /// </summary>
    /// <example>PartitionKey eq 'pk1' and RowKey ge '10'</example>
    [DisplayFormat(DataFormatString = "Text")]
    [NotWhiteSpace(ErrorMessage = "Filter must contain an OData expression or be empty.")]
    [DefaultValue("")]
    public string Filter { get; set; } = string.Empty;

    /// <summary>
    /// Optional entity property names to return. PartitionKey and RowKey are always returned.
    /// Leave empty to return all properties.
    /// Use the same selection on each page when passing a continuation token.
    /// </summary>
    /// <example>["Name", "Price"]</example>
    [NotWhiteSpace(ErrorMessage = "Select must contain non-empty property names.")]
    [DefaultValue(null)]
    public string[] Select { get; set; } = [];

    /// <summary>
    /// Maximum number of entities per page (1-1000), not a limit on the whole query.
    /// Azure Table Storage can return fewer entities with a continuation token.
    /// </summary>
    /// <example>100</example>
    [Range(1, 1000, ErrorMessage = "Top must be between 1 and 1000.")]
    [DefaultValue(1000)]
    public int Top { get; set; } = 1000;

    /// <summary>
    /// Token returned by a previous query page. Leave empty to start from the beginning.
    /// </summary>
    /// <example>next-page-token</example>
    [DisplayFormat(DataFormatString = "Text")]
    [NotWhiteSpace(ErrorMessage = "ContinuationToken must be a token or be empty.")]
    [DefaultValue("")]
    public string ContinuationToken { get; set; } = string.Empty;
}
