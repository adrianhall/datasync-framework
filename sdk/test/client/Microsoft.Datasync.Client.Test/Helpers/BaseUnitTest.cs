using FluentAssertions.Equivalency;
using Microsoft.Datasync.Client.Sync;

namespace Microsoft.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
public abstract class BaseUnitTest
{
    public static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new TestDbContext(options);
        return context;
    }

    /// <summary>
    /// Convenience method for excluding the metadata of a queue entry from comparison.
    /// </summary>
    public static EquivalencyAssertionOptions<OfflineOperationsQueueEntity> ExcludingMetadata(EquivalencyAssertionOptions<OfflineOperationsQueueEntity> options)
    {
        options.Excluding(x => x.TransactionId);
        options.Excluding(x => x.CreatedAt);
        options.Excluding(x => x.UpdatedAt);
        return options;
    }

    /// <summary>
    /// Copies a queue entity for comparison later.
    /// </summary>
    protected static OfflineOperationsQueueEntity CopyOf(OfflineOperationsQueueEntity entity) => new()
    {
        TransactionId = entity.TransactionId,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        OperationType = entity.OperationType,
        EntityId = entity.EntityId,
        EntityType = Type.GetType(entity.EntityType.FullName!)!,
        SerializedEntity = entity.SerializedEntity
    };
}
