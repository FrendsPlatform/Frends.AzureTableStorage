using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Frends.AzureTableStorage.QueryEntities.Definitions;

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
    /// Entities in this page as JSON objects, including their keys and odata.etag.
    /// </summary>
    /// <example>[ { "PartitionKey": "pk1", "RowKey": "1", "Name": "Anna", "odata.etag": "W/\"datetime'2024-05-01T10%3A15%3A30.1234567Z'\"" } ]</example>
    public List<JObject> Entities { get; set; }

    /// <summary>
    /// Number of entities on this page.
    /// </summary>
    /// <example>2</example>
    public int Count { get; set; }

    /// <summary>
    /// Token for the next page, or null or empty if there are no more pages.
    /// A page may be empty even when this token is present.
    /// </summary>
    /// <example>next-page-token</example>
    public string ContinuationToken { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
