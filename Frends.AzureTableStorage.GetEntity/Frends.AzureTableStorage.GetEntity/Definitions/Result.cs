namespace Frends.AzureTableStorage.GetEntity.Definitions;

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
    /// True if the entity exists.
    /// </summary>
    /// <example>true</example>
    public bool Found { get; set; }

    /// <summary>
    /// The entity with its properties as JToken, including odata.etag
    /// Null if the entity was not found.
    /// </summary>
    /// <example>{ "PartitionKey": "pk1", "RowKey": "1", "Name": "Anna", "odata.etag": "W/\"datetime'2024-05-01T10%3A15%3A30.1234567Z'\"" }</example>
    public dynamic Entity { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}