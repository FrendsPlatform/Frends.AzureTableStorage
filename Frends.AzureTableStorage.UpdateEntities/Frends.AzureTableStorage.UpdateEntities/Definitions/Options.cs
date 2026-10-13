using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.AzureTableStorage.UpdateEntities.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// Merge changes only the properties given in the input; other properties are left unchanged
    /// (a property cannot be removed in this mode). Replace replaces the whole entity,
    /// so properties missing from the input are removed.
    /// In both modes the entity must exist, otherwise the update fails with 404 and no entity is created.
    /// </summary>
    /// <example>Merge</example>
    [DefaultValue(UpdateMode.Merge)]
    public UpdateMode UpdateMode { get; set; } = UpdateMode.Merge;

    /// <summary>
    /// Maximum number of entities per transaction (1-100). Entities are grouped by PartitionKey and each group
    /// is sent in transactions of this size. A transaction is atomic: if one entity fails (e.g. it does not exist,
    /// or its odata.etag is outdated), none of the entities in that transaction are updated, and all of them
    /// are reported in Error.FailedItems.
    /// Use 1 to isolate such failures to a single entity and get results per entity.
    /// Lower the value if entities are large, because a single transaction is limited to 4 MB.
    /// </summary>
    /// <example>100</example>
    [DefaultValue(100)]
    [Range(1, 100, ErrorMessage = "BatchSize must be between 1 and 100.")]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// If false, processing stops at the first failed transaction (or entity).
    /// Entities updated before the failure remain updated (remote rollback is not supported) and Success is false.
    /// If true, failures are recorded in Error.FailedItems, processing continues and Success is true,
    /// so always check Error when this option is enabled.
    /// Independent of ThrowErrorOnFailure.
    /// </summary>
    /// <example>false</example>
    [DefaultValue(false)]
    public bool ContinueOnFailure { get; set; } = false;

    /// <summary>
    /// If true, an exception is thrown on failure. The exception message contains a summary
    /// including the number and keys of entities updated before the failure, the number of entities
    /// not updated or not attempted, and the cause of the failure.
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
