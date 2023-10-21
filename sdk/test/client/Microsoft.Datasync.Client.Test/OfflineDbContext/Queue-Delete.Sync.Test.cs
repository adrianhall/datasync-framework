﻿using Microsoft.Datasync.Client.Sync;
using System.Text.Json;

namespace Microsoft.Datasync.Client.Test;

public partial class OfflineDbContext_Tests
{
    #region Basic Deletes
    [Fact]
    public void QueueDelete_RemoteDefault_Works()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();
        context.RemoteDefaultEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var start = DateTimeOffset.UtcNow;
        context.RemoteDefaultEntities.Remove(entity);
        context.SaveChanges();
        var end = DateTimeOffset.UtcNow;

        context.OfflineOperationsQueue.Should().HaveCount(1);

        var expected = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Delete,
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
    public void QueueDelete_RemotePath_Works()
    {
        var context = CreateContext();
        var entity = new RemotePathEntity();
        context.RemotePathEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var start = DateTimeOffset.UtcNow;
        context.RemotePathEntities.Remove(entity);
        context.SaveChanges();
        var end = DateTimeOffset.UtcNow;

        context.OfflineOperationsQueue.Should().HaveCount(1);

        var expected = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Delete,
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
    public void QueueDelete_OfflineOnly_Works()
    {
        var context = CreateContext();
        var entity = new OfflineOnlyEntity();
        context.OfflineOnlyEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        context.OfflineOnlyEntities.Remove(entity);
        context.SaveChanges();

        context.OfflineOperationsQueue.Should().HaveCount(0);
    }
    #endregion

    #region Queue Updates
    [Fact]
    public void QueueDelete_FollowedByAdd_Throws()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();
        
        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Delete,
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
    public void QueueDelete_FollowedByDelete_Throws()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();

        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Delete,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.RemoteDefaultEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        context.RemoteDefaultEntities.Remove(entity);
        Action act = () => _ = context.SaveChanges();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void QueueDelete_FollowedByUpdate_Throws()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();

        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Delete,
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
        Action act = () => _ = context.SaveChanges();

        act.Should().Throw<InvalidOperationException>();
    }
    #endregion
}
