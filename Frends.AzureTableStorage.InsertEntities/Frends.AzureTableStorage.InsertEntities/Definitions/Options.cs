using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.InsertEntities.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Defines how entities are written. Add inserts new entities only and fails for an entity that already exists (409).
    /// UpsertMerge inserts or merges the input properties into the existing entity.
    /// UpsertReplace inserts or fully replaces the existing entity.
    /// </summary>
    /// <example>Add</example>
    [DefaultValue(InsertMode.Add)]
    public InsertMode InsertMode { get; set; } = InsertMode.Add;

    /// <summary>
    /// If true, entities are grouped by PartitionKey and sent in transactions of up to BatchSize entities.
    /// A transaction is atomic: if one entity fails, none of the entities in that transaction are written.
    /// If false, entities are written one by one (no atomicity, BatchSize is ignored).
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool UseTransactions { get; set; } = true;

    /// <summary>
    /// Maximum number of entities per transaction (1-100). Use 1 to get results per entity.
    /// Lower the value if entities are large, because a single transaction is limited to 4 MB.
    /// </summary>
    /// <example>100</example>
    [DefaultValue(100)]
    [UIHint(nameof(UseTransactions), "", true)]
    [Range(1, 100, ErrorMessage = "BatchSize must be between 1 and 100.")]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// If false, processing stops at the first failed transaction (or entity). Items written before it remain
    /// Entities written before the failure remain in the table and Success is false.
    /// If true, failures are recorded in Error.FailedItems, processing continues and Success is true,
    /// so always check Error when this option is enabled.
    /// Independent of ThrowErrorOnFailure.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool ContinueOnFailure { get; set; } = false;

    /// <summary>
    /// If true, an exception is thrown on failure. The exception message contains a summary
    /// including the number and keys of entities written before the failure, the number of entities
    /// not written or not attempted, and the cause of the failure.
    /// SucceededItems and Error.FailedItems are not returned.
    /// If false, the failure is returned in the result together with both lists.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Overrides the error message on failure.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
