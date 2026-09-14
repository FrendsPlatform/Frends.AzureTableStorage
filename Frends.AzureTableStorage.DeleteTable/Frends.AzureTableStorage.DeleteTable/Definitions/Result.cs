namespace Frends.AzureTableStorage.DeleteTable.Definitions;

/// <summary>
/// Result of the DeleteTable operation.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the table deletion operation completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Name of the table that was deleted or didn't exist.
    /// </summary>
    /// <example>Customers</example>
    public string TableName { get; set; }

    /// <summary>
    /// Indicates whether the table was deleted (true) or didn't exist (false).
    /// </summary>
    /// <example>true</example>
    public bool Deleted { get; set; }

    /// <summary>
    /// URI to the table in Azure Table Storage before deletion.
    /// </summary>
    /// <example>https://mystorageaccount.table.core.windows.net/Customers</example>
    public string TableUri { get; set; }

    /// <summary>
    /// Error details if the operation failed. Null when Success is true.
    /// </summary>
    /// <example>null</example>
    public Error Error { get; set; }
}
