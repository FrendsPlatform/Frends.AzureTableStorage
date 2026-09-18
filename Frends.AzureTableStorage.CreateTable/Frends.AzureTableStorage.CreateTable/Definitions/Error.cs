using System;

namespace Frends.AzureTableStorage.CreateTable.Definitions;

/// <summary>
/// Error information from a failed operation.
/// </summary>
public class Error
{
    /// <summary>
    /// Error message describing what went wrong.
    /// </summary>
    /// <example>Failed to create table: The table already exists.</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional technical details about the error, such as HTTP status codes or Azure error codes.
    /// </summary>
    /// <example>Status: 409 (Conflict), ErrorCode: TableAlreadyExists</example>
    public Exception AdditionalInfo { get; set; }
}
