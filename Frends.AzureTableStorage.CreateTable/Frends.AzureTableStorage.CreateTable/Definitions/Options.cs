using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.CreateTable.Definitions;

/// <summary>
/// Optional parameters for controlling task behavior.
/// </summary>
public class Options
{
    /// <summary>
    /// Whether to treat an already existing table as a failure.
    /// If false, the task succeeds with Created = false when the table already exists.
    /// If true, the already-existing table is treated as a failure — the behavior then depends
    /// on ThrowErrorOnFailure: either an exception is thrown or a result with Success = false is returned.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool FailIfTableExists { get; set; } = false;

    /// <summary>
    /// Whether to throw an exception if the operation fails.
    /// If set to false, the task returns a result with Success = false instead of throwing.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Custom error message to use when the operation fails.
    /// If empty, the original error message is used.
    /// </summary>
    /// <example>Failed to create Azure table</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
