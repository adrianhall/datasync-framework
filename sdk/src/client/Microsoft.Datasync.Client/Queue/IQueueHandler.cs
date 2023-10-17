using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Microsoft.Datasync.Client.Queue;

/// <summary>
/// Description of the queue handler that is used for maintaining the offline operations queue.
/// </summary>
internal interface IQueueHandler
{
    /// <summary>
    /// Adds a change to the ofline operation queue.
    /// </summary>
    /// <param name="changeEntry">The ChangeTracker entity being added to the offline operations queue.</param>
    /// <param name="options">The queue handler options.</param>
    /// <returns><c>true</c> if the change was added to the queue; <c>false otherwise</c>.</returns>
    bool Add(EntityEntry changeEntry, QueueHandlerOptions options);

    /// <summary>
    /// Adds a change to the ofline operation queue asynchronously.
    /// </summary>
    /// <param name="changeEntry">The ChangeTracker entity being added to the offline operations queue.</param>
    /// <param name="options">The queue handler options.</param>
    /// <returns>A task that on completion returns <c>true</c> if the change was added to the queue; <c>false otherwise</c>.</returns>
    ValueTask<bool> AddAsync(EntityEntry changeEntry, QueueHandlerOptions options, CancellationToken cancellationToken = default);
}
