using Microsoft.Datasync.Client.Sync;
using System.Text.Json;

namespace Microsoft.Datasync.Client.Test;

public partial class OfflineDbContext_Tests
{
    #region Basic Replaces
    [Fact]
    public void QueueReplace_RemoteDefault_Works()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();
        context.RemoteDefaultEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var start = DateTimeOffset.UtcNow;
        entity.Name = "Updated";
        context.RemoteDefaultEntities.Update(entity);
        context.SaveChanges();
        var end = DateTimeOffset.UtcNow;

        context.OfflineOperationsQueue.Should().HaveCount(1);

        var expected = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Replace,
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
    public void QueueReplace_RemotePath_Works()
    {
        var context = CreateContext();
        var entity = new RemotePathEntity();
        context.RemotePathEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var start = DateTimeOffset.UtcNow;
        entity.Name = "Updated";
        context.RemotePathEntities.Update(entity);
        context.SaveChanges();
        var end = DateTimeOffset.UtcNow;

        context.OfflineOperationsQueue.Should().HaveCount(1);

        var expected = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Replace,
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
    public void QueueReplace_OfflineOnly_Works()
    {
        var context = CreateContext();
        var entity = new OfflineOnlyEntity();
        context.OfflineOnlyEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        entity.Name = "Updated";
        context.OfflineOnlyEntities.Update(entity);
        context.SaveChanges();

        context.OfflineOperationsQueue.Should().HaveCount(0);
    }
    #endregion

    #region Queue Updates
    [Fact]
    public void QueueReplace_FollowedByAdd_Throws()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();
        
        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Replace,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        context.RemoteDefaultEntities.Add(entity);
        Action act = () => _ = context.SaveChanges();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void QueueReplace_FollowedByDelete_UpdatesQueueToDelete()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();

        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Replace,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.RemoteDefaultEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var originalQueueEntity = CopyOf(queueEntity);
        var start = DateTimeOffset.UtcNow;

        context.RemoteDefaultEntities.Remove(entity);
        context.SaveChanges();

        context.OfflineOperationsQueue.Should().HaveCount(1); // The one we inserted, then updated.

        var storedEntity = context.OfflineOperationsQueue.First();
        storedEntity.TransactionId.Should().Be(originalQueueEntity.TransactionId);
        storedEntity.CreatedAt.Should().Be(originalQueueEntity.CreatedAt);
        storedEntity.UpdatedAt.Should().BeAfter(start);
        storedEntity.OperationType.Should().Be(OperationType.Delete);
        storedEntity.EntityId.Should().Be(entity.Id);
        storedEntity.EntityType.Should().Be(typeof(RemoteDefaultEntity));
        storedEntity.SerializedEntity.Should().Be(originalQueueEntity.SerializedEntity);
    }

    [Fact]
    public void QueueReplace_FollowedByUpdate_UpdatesQueueEntity()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();

        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Replace,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.RemoteDefaultEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var originalQueueEntity = CopyOf(queueEntity);

        var start = DateTimeOffset.UtcNow;
        entity.Name = "Updated";
        context.RemoteDefaultEntities.Update(entity);
        context.SaveChanges();

        var storedEntity = context.OfflineOperationsQueue.First();
        storedEntity.TransactionId.Should().Be(originalQueueEntity.TransactionId);
        storedEntity.CreatedAt.Should().Be(originalQueueEntity.CreatedAt);
        storedEntity.UpdatedAt.Should().BeAfter(start);
        storedEntity.OperationType.Should().Be(OperationType.Replace);
        storedEntity.EntityId.Should().Be(entity.Id);
        storedEntity.EntityType.Should().Be(typeof(RemoteDefaultEntity));
        storedEntity.SerializedEntity.Should().Be(JsonSerializer.Serialize(entity, context.SerializerOptions));
    }
    #endregion
}
