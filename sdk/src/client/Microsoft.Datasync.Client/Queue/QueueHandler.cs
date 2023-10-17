using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Microsoft.Datasync.Client.Queue;

/// <summary>
/// Concrete implementation of the <see cref="IQueueHandler"/> that adjusts
/// the OfflineQueueOperations table within the provided context.
/// </summary>
internal class QueueHandler : IQueueHandler
{
    private readonly OfflineDbContext context;

    public QueueHandler(OfflineDbContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Adds a change to the ofline operation queue.
    /// </summary>
    /// <param name="changeEntry">The ChangeTracker entity being added to the offline operations queue.</param>
    /// <param name="options">The queue handler options.</param>
    /// <returns><c>true</c> if the change was added to the queue; <c>false otherwise</c>.</returns>
    public bool Add(EntityEntry changeEntry, QueueHandlerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a change to the ofline operation queue asynchronously.
    /// </summary>
    /// <param name="changeEntry">The ChangeTracker entity being added to the offline operations queue.</param>
    /// <param name="options">The queue handler options.</param>
    /// <returns>A task that on completion returns <c>true</c> if the change was added to the queue; <c>false otherwise</c>.</returns>
    public ValueTask<bool> AddAsync(EntityEntry changeEntry, QueueHandlerOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
