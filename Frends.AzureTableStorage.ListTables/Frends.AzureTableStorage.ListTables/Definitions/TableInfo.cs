namespace Frends.AzureTableStorage.ListTables.Definitions;

/// <summary>
/// Represents table metadata, including its name and URI.
/// </summary>
public class TableInfo
{
    /// <summary>
    /// Name of the table.
    /// </summary>
    /// <example>Customers</example>
    public string Name { get; set; }

    /// <summary>
    /// Uri of the table.
    /// </summary>
    /// <example>https://mystorageaccount.table.core.windows.net/Customers</example>
    public string Uri { get; set; }
}
