using System.ComponentModel.DataAnnotations;

namespace Microsoft.Datasync.Client.Sync;

/// <summary>
/// The type of operation within the offline operations queue.
/// </summary>
public enum OperationType
{
    /// <summary>
    /// The default value - Unknown should never be used.
    /// </summary>
    Unknown,

    /// <summary>
    /// The operation is to add an entity to the remote dataset.
    /// </summary>
    Add,

    /// <summary>
    /// The operation is to delete an entity from the remote dataset.
    /// </summary>
    Delete,

    /// <summary>
    /// The operation is to replace an entity within the remote dataset.
    /// </summary>
    Replace
}

/// <summary>
/// Definition of a single transaction in the offline operations queue.
/// </summary>
public class OfflineOperationsQueueEntity
{
    /// <summary>
    /// The ID for this transaction.  The ID is unique for a single unsent
    /// operation for a single instance of an entity in a DbSet.
    /// </summary>
    [Key]
    public Guid TransactionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The date/time that the transaction was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date/time that the transaction was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The operation type (Add, Delete, Replace) for this transaction.
    /// </summary>
    public OperationType OperationType { get; set; } = OperationType.Unknown;

    /// <summary>
    /// The type of the entity within the operation.
    /// </summary>
    public Type EntityType { get; set; } = typeof(OfflineOperationsQueueEntity);

    /// <summary>
    /// The ID of the entity within the operation.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The original value of the entity, serialized for storage.
    /// </summary>
    public string SerializedEntity { get; set; } = string.Empty;
}
