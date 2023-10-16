using Microsoft.EntityFrameworkCore;

namespace Microsoft.Datasync.Client;

/// <summary>
/// The <see cref="OfflineDbContext"/> is a replacement base class for the <see cref="DbContext"/>
/// used by Entity Framework Core.
/// </summary>
/// <remarks>
/// You can use any driver that supports Entity Framework Core, but the one we use in testing is
/// SQLite (with some adjustments).
/// </remarks>
public class OfflineDbContext : DbContext
{
    /// <inheritdoc />
    protected OfflineDbContext() : base()
    {
    }

    /// <inheritdoc />
    public OfflineDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// The synchronization provider is used for maintaining the offline cache.  It specifically maintains the operations
    /// queue and the date/time that a <see cref="DbSet{TEntity}"/> was last synchronized to the server.
    /// </summary>
    public ISynchronizationProvider SynchronizationProvider { get; init; } = new NullSynchronizationProvider();

    /// <summary>
    /// The operations queue holds the operations that have been performed on a specific <see cref="DbSet{TEntity}"/> since
    /// the last synchronization operation.
    /// </summary>
    internal DbSet<OfflineOperationsQueueEntity> OfflineOperationsQueue => Set<OfflineOperationsQueueEntity>();

    /// <summary>
    /// The offline synchronizations table holds the last time a <see cref="DbSet{TEntity}"/> was synchronized to the server.
    /// </summary>
    internal DbSet<OfflineSynchronizationEntity> OfflineSynchronizations => Set<OfflineSynchronizationEntity>();

    // TODO: SaveChanges

    // TODO: SaveChangesAsync
}