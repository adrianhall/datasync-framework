using System.ComponentModel.DataAnnotations;

namespace Microsoft.Datasync.Client.Sync;

/// <summary>
/// The entity model for the offline synchronizations dataset.  This dataset
/// stores the last sequence ID that was synchronized for each table.
/// </summary>
internal class OfflineSynchronizationEntity
{
    /// <summary>
    /// The name of the table being synchronized.
    /// </summary>
    [Key]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The last sequence ID that was synchronized for this table.
    /// </summary>
    public long LastSynchronizationSequence { get; set; } = 0L;
}
