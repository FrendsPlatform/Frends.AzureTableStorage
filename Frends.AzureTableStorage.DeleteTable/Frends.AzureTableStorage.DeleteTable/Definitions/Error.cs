using System;

namespace Frends.AzureTableStorage.DeleteTable.Definitions;

/// <summary>
/// Error information from a failed operation.
/// </summary>
public class Error
{
    /// <summary>
    /// Error message describing what went wrong.
    /// </summary>
    /// <example>Failed to delete table.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional technical details about the error, such as HTTP status codes or Azure error codes.
    /// </summary>
    /// <example>Status: 404 (Not Found), ErrorCode: RequestFailedException</example>
    public Exception AdditionalInfo { get; set; }
}
