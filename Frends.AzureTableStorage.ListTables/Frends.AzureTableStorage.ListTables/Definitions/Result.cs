using System.Collections.Generic;

namespace Frends.AzureTableStorage.ListTables.Definitions;

/// <summary>
/// Result of the task.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates if the task completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Tables found.
    /// </summary>
    /// <example>[ { Name = "orders", Uri = "https://myaccount.table.core.windows.net/orders" } ]</example>
    public List<TableInfo> Tables { get; set; }

    /// <summary>
    /// Number of tables returned.
    /// </summary>
    /// <example>5</example>
    public int Count { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
