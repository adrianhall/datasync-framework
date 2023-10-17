namespace Microsoft.Datasync.Client.Sync;

/// <summary>
/// The default implementation of the <see cref="ISynchronizationProvider"/> that
/// doesn't do anything with the queue.  It throws an error if the synchronization
/// is requested.
/// </summary>
public class NullSynchronizationProvider : ISynchronizationProvider
{
}
