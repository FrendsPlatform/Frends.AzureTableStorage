using System.ComponentModel;

namespace Frends.AzureTableStorage.DeleteTable.Definitions;

/// <summary>
/// Optional parameters for controlling task behavior.
/// </summary>
public class Options
{
    /// <summary>
    /// Whether to treat a non-existing table as a failure.
    /// If false, the task succeeds with Deleted = false when the table doesn't exist.
    /// If true, the non-existing table is treated as a failure.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool FailIfTableNotExists { get; set; } = false;

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
    /// <example>Failed to delete Azure table</example>
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
