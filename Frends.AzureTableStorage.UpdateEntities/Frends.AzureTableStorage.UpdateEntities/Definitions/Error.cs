using System;
using System.Collections.Generic;

namespace Frends.AzureTableStorage.UpdateEntities.Definitions;

/// <summary>
/// Error that occurred during the task.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>Unable to update entity.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>object { Exception AdditionalInfo }</example>
    public Exception AdditionalInfo { get; set; }

    /// <summary>
    /// Entities that were not updated, with the reason. Available when ThrowErrorOnFailure=false
    /// or ContinueOnFailure=true.
    /// </summary>
    /// <example>[ { "PartitionKey": "pk1", "RowKey": "3", "IsCause": true, "Reason": "Status 412 UpdateConditionNotSatisfied: The update condition specified in the request was not satisfied." } ]</example>
    public List<FailedItem> FailedItems { get; set; }
}
