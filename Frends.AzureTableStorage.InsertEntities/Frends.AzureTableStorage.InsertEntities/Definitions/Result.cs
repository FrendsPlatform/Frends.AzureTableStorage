using System.Collections.Generic;

namespace Frends.AzureTableStorage.InsertEntities.Definitions;

/// <summary>
/// Result of the task.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates if the task completed successfully.
    /// With ContinueOnFailure=false: true only if all entities were written.
    /// With ContinueOnFailure=true: always true when the task ran to the end, even if some entities failed.
    /// Check Error.FailedItems in that case.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Entities confirmed as written (inserted or upserted), identified by PartitionKey and RowKey.
    /// After a failure it contains the entities written before it (remote rollback is not supported).
    /// Not returned when an exception is thrown (ThrowErrorOnFailure=true).
    /// </summary>
    /// <example>[ { "PartitionKey": "pk1", "RowKey": "1" }, { "PartitionKey": "pk1", "RowKey": "2" } ]</example>
    public List<EntityKey> SucceededItems { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// With ContinueOnFailure=true it is set only if some entities failed.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo, List&lt;FailedItem&gt; FailedItems }</example>
    public Error Error { get; set; }
}
