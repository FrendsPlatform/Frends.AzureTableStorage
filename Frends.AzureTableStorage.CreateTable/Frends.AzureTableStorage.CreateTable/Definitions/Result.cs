namespace Frends.AzureTableStorage.CreateTable.Definitions;

/// <summary>
/// Result of the CreateTable operation.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the table creation operation completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Name of the table that was created or already existed.
    /// </summary>
    /// <example>Customers</example>
    public string TableName { get; set; }

    /// <summary>
    /// Indicates whether the table was newly created (true) or already existed (false).
    /// </summary>
    /// <example>true</example>
    public bool Created { get; set; }

    /// <summary>
    /// URI to the created table in Azure Table Storage.
    /// </summary>
    /// <example>https://mystorageaccount.table.core.windows.net/Customers</example>
    public string TableUri { get; set; }

    /// <summary>
    /// Error details if the operation failed.
    /// Null when Success is true.
    /// </summary>
    /// <example>null</example>
    public Error Error { get; set; }
}
