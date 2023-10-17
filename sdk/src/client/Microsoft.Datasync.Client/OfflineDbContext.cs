using Microsoft.Datasync.Client.Converters;
using Microsoft.Datasync.Client.Queue;
using Microsoft.Datasync.Client.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        QueueHandler = new QueueHandler(this);
    }

    /// <inheritdoc />
    public OfflineDbContext(DbContextOptions options) : base(options)
    {
        QueueHandler = new QueueHandler(this);
    }

    /// <inheritdoc />
    public override int SaveChanges()
        => SaveChanges(true, QueueHandlerOptions.DefaultOptions);

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => SaveChanges(acceptAllChangesOnSuccess, QueueHandlerOptions.DefaultOptions);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, QueueHandlerOptions.DefaultOptions, cancellationToken);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess, QueueHandlerOptions.DefaultOptions, cancellationToken);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.TransactionId).HasConversion<GuidValueConverter>();
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.CreatedAt).HasConversion<DateTimeOffsetValueConverter>();
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.UpdatedAt).HasConversion<DateTimeOffsetValueConverter>();
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.OperationType).HasConversion<string>();
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.EntityType).HasConversion<TypeValueConverter>();

        base.OnModelCreating(modelBuilder);
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

    /// <summary>
    /// The queue handler that updates the <see cref="OfflineOperationsQueue"/> when changes are made to the <see cref="DbContext"/>.
    /// </summary>
    internal IQueueHandler QueueHandler { get; }

    /// <summary>
    /// Saves all changes made in this context to the underlying database.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Indicates whether <c>AcceptAllChanges()</c> is called after the changes
    /// have been sent successfully to the database.</param>
    /// <param name="options">The options for controlling queue behavior when adding a change to the <see cref="OfflineOperationsQueue"/>.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <exception cref="DbUpdateException">An error is encountered while saving to the database.</exception>
    /// <exception cref="DbUpdateConcurrencyException">A concurrency violation is encountered while saving to the database. A concurrency violation 
    /// occurs when an unexpected number of rows are affected during save. This is usually because the data in the database has been modified since 
    /// it was loaded into memory.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is cancelled.</exception>
    /// <exception cref="OfflineOperationsQueueException">An error occurred attempting to add an operation to the offline operations queue.</exception>
    internal int SaveChanges(bool acceptAllChangesOnSuccess, QueueHandlerOptions options)
    {
        if (ChangeTracker.AutoDetectChangesEnabled)
        {
            ChangeTracker.DetectChanges();
        }

        if (options.AddChangesToQueue)
        {
            foreach (var change in ChangeTracker.Entries())
            {
                // Skip any change that belongs to the OfflineOperationsQueue or OfflineSynchronizations DbSet.
                if (change.Entity is OfflineOperationsQueueEntity || change.Entity is OfflineSynchronizationEntity)
                {
                    continue;
                }
                QueueHandler.Add(change, options);
            }
        }

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Saves all changes made in this context to the underlying database.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Indicates whether <c>AcceptAllChanges()</c> is called after the changes
    /// have been sent successfully to the database.</param>
    /// <param name="options">The options for controlling queue behavior when adding a change to the <see cref="OfflineOperationsQueue"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    /// <exception cref="DbUpdateException">An error is encountered while saving to the database.</exception>
    /// <exception cref="DbUpdateConcurrencyException">A concurrency violation is encountered while saving to the database. A concurrency violation 
    /// occurs when an unexpected number of rows are affected during save. This is usually because the data in the database has been modified since 
    /// it was loaded into memory.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is cancelled.</exception>
    /// <exception cref="OfflineOperationsQueueException">An error occurred attempting to add an operation to the offline operations queue.</exception>
    internal async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, QueueHandlerOptions options, CancellationToken cancellationToken)
    {
        if (ChangeTracker.AutoDetectChangesEnabled)
        {
            ChangeTracker.DetectChanges();
        }

        if (options.AddChangesToQueue)
        {
            foreach (var change in ChangeTracker.Entries())
            {
                await QueueHandler.AddAsync(change, options, cancellationToken).ConfigureAwait(false);
            }
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
    }
}