using Microsoft.Datasync.Client.Sync;
using System.Text.Json;

namespace Microsoft.Datasync.Client.Test;

public partial class OfflineDbContext_Tests
{
    #region Basic Async Adds 
    [Fact]
    public async Task QueueAdd_RemoteDefault_WorksAsync()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();

        var start = DateTimeOffset.UtcNow;
        context.RemoteDefaultEntities.Add(entity);
        await context.SaveChangesAsync();
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
    public async Task QueueAdd_RemotePath_WorksAsync()
    {
        var context = CreateContext();
        var entity = new RemotePathEntity();

        var start = DateTimeOffset.UtcNow;
        context.RemotePathEntities.Add(entity);
        await context.SaveChangesAsync();
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
    public async Task QueueAdd_OfflineOnly_WorksAsync()
    {
        var context = CreateContext();
        var entity = new OfflineOnlyEntity();

        context.OfflineOnlyEntities.Add(entity);
        await context.SaveChangesAsync();

        context.OfflineOperationsQueue.Should().HaveCount(0);
    }
    #endregion

    #region Corner Cases
    [Fact]
    public async Task QueueAdd_OfflineOperationsQueue_WorksAsync()
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

        context.OfflineOperationsQueue.Add(queueEntity);
        await context.SaveChangesAsync();
        context.OfflineOperationsQueue.Should().HaveCount(1); // The one we inserted.

        var storedEntity = context.OfflineOperationsQueue.First();
        storedEntity.Should().BeEquivalentTo(queueEntity, ExcludingMetadata);
        storedEntity.TransactionId.Should().Be(queueEntity.TransactionId);
        storedEntity.UpdatedAt.Should().Be(queueEntity.UpdatedAt);
    }

    [Fact]
    public async Task QueueAdd_OfflineSyncEntity_WorksAsync()
    {
        var context = CreateContext();
        var queueEntity = new OfflineSynchronizationEntity()
        {
            TableName = "OfflineOnlyEntity",
            LastSynchronizationSequence = 42L
        };

        context.OfflineSynchronizations.Add(queueEntity);
        await context.SaveChangesAsync();
        context.OfflineOperationsQueue.Should().HaveCount(0);
    }

    [Fact]
    public async Task QueueAdd_NoId_WorksAsync()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity { Id = string.Empty };
        context.RemoteDefaultEntities.Add(entity);
        Func<Task> act = async () => _ = await context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidEntityException>();
    }
    #endregion

    #region Queue Updates
    [Fact]
    public async Task QueueAdd_FollowedByAdd_ThrowsAsync()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();
        
        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Add,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        context.RemoteDefaultEntities.Add(entity);
        Func<Task> act = async () => _ = await context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task QueueAdd_FollowedByDelete_DeletedQueueEntryAsync()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();

        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Add,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.RemoteDefaultEntities.Add(entity);
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        context.RemoteDefaultEntities.Remove(entity);
        await context.SaveChangesAsync();

        context.OfflineOperationsQueue.Should().HaveCount(0);
    }

    [Fact]
    public async Task QueueAdd_FollowedByUpdate_UpdatesQueueEntryAsync()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();

        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Add,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.RemoteDefaultEntities.Add(entity);
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var originalQueueEntity = CopyOf(queueEntity);

        var start = DateTimeOffset.UtcNow;
        entity.Name = "Updated";
        context.RemoteDefaultEntities.Update(entity);
        await context.SaveChangesAsync();

        context.OfflineOperationsQueue.Should().HaveCount(1); // The one we inserted, then updated.

        var storedEntity = context.OfflineOperationsQueue.First();
        storedEntity.TransactionId.Should().Be(originalQueueEntity.TransactionId);
        storedEntity.CreatedAt.Should().Be(originalQueueEntity.CreatedAt);
        storedEntity.UpdatedAt.Should().BeAfter(start);
        storedEntity.OperationType.Should().Be(OperationType.Add);
        storedEntity.EntityId.Should().Be(entity.Id);
        storedEntity.EntityType.Should().Be(typeof(RemoteDefaultEntity));
        storedEntity.SerializedEntity.Should().Be(JsonSerializer.Serialize(entity, context.SerializerOptions));
    }
    #endregion
}
