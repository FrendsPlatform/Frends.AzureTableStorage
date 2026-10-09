using System;
using System.Collections.Generic;

namespace Frends.AzureTableStorage.InsertEntities.Definitions;

/// <summary>
/// Error that occurred during the task.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>The specified entity already exists.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>object { Exception AdditionalInfo }</example>
    public Exception AdditionalInfo { get; set; }

    /// <summary>
    /// Entities that were not written, with the reason. Available when ThrowErrorOnFailure=false
    /// or ContinueOnFailure=true.
    /// </summary>
    /// <example>[ { "PartitionKey": "pk1", "RowKey": "3", "IsCause": true, "Reason": "Status 409 EntityAlreadyExists: The specified entity already exists." } ]</example>
    public List<FailedItem> FailedItems { get; set; }
}
