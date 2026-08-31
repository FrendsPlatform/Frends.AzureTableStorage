using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.CreateTable.Definitions;

/// <summary>
/// Essential parameters for creating an Azure Table Storage table.
/// </summary>
public class Input
{
    /// <summary>
    /// Name of the table to create. Table names must be between 3 and 63 characters long, contain only alphanumeric characters, and cannot begin with a numeric character.
    /// </summary>
    /// <example>Customers</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    [Required]
    public string TableName { get; set; } = string.Empty;
}
