using Microsoft.Datasync.Client.Sync;
using System.Text.Json;

namespace Microsoft.Datasync.Client.Test;

public partial class OfflineDbContext_Tests
{
    #region Basic Adds
    [Fact]
    public void QueueAdd_RemoteDefault_Works()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();

        var start = DateTimeOffset.UtcNow;
        context.Add(entity);
        context.SaveChanges();
        var end = DateTimeOffset.UtcNow;

        context.OfflineOperationsQueue.Should().HaveCount(1);

        var expected = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Add,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        var actual = context.OfflineOperationsQueue.First();

        actual.Should()
            .BeEquivalentTo(expected, ExcludingMetadata)
            .And.HaveValidMetadata(start, end);
    }

    [Fact]
    public void QueueAdd_RemotePath_Works()
    {
        var context = CreateContext();
        var entity = new RemotePathEntity();

        var start = DateTimeOffset.UtcNow;
        context.Add(entity);
        context.SaveChanges();
        var end = DateTimeOffset.UtcNow;

        context.OfflineOperationsQueue.Should().HaveCount(1);

        var expected = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Add,
            EntityType = typeof(RemotePathEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        var actual = context.OfflineOperationsQueue.First();

        actual.Should()
            .BeEquivalentTo(expected, ExcludingMetadata)
            .And.HaveValidMetadata(start, end);
    }

    [Fact]
    public void QueueAdd_OfflineOnly_Works()
    {
        var context = CreateContext();
        var entity = new OfflineOnlyEntity();

        context.Add(entity);
        context.SaveChanges();

        context.OfflineOperationsQueue.Should().HaveCount(0);
    }
    #endregion

    #region Corner Cases
    [Fact]
    public void QueueAdd_OfflineOperationsQueue_Works()
    {
        var context = CreateContext();
        var entity = new OfflineOnlyEntity();
        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Add,
            EntityType = typeof(OfflineOnlyEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };

        context.Add(queueEntity);
        context.SaveChanges();
        context.OfflineOperationsQueue.Should().HaveCount(1); // The one we inserted.

        var storedEntity = context.OfflineOperationsQueue.First();
        storedEntity.Should().BeEquivalentTo(queueEntity, ExcludingMetadata);
        storedEntity.TransactionId.Should().Be(queueEntity.TransactionId);
        storedEntity.UpdatedAt.Should().Be(queueEntity.UpdatedAt);
    }

    [Fact]
    public void QueueAdd_OfflineSyncEntity_Works()
    {
        var context = CreateContext();
        var queueEntity = new OfflineSynchronizationEntity()
        {
            TableName = "OfflineOnlyEntity",
            LastSynchronizationSequence = 42L
        };

        context.Add(queueEntity);
        context.SaveChanges();
        context.OfflineOperationsQueue.Should().HaveCount(0);
    }

    [Fact]
    public void QueueAdd_NoId_Works()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity { Id = string.Empty };
        context.Add(entity);
        Action act = () => _ = context.SaveChanges();
        act.Should().Throw<InvalidEntityException>();
    }
    #endregion
}
