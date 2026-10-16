using System;
using System.Collections.Generic;

namespace Frends.AzureTableStorage.DeleteEntities.Definitions;

/// <summary>
/// Error that occurred during the task.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>Unable to delete entity.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>object { Exception AdditionalInfo }</example>
    public Exception AdditionalInfo { get; set; }

    /// <summary>
    /// Entities that were not deleted, with the reason. Available when ThrowErrorOnFailure=false
    /// or ContinueOnFailure=true.
    /// </summary>
    /// <example>[ { "PartitionKey": "pk1", "RowKey": "3", "IsCause": true, "Reason": "Status 404 ResourceNotFound: The specified resource does not exist." } ]</example>
    public List<FailedItem> FailedItems { get; set; }
}
