using Microsoft.Datasync.Client.Sync;
using System.Text.Json;

namespace Microsoft.Datasync.Client.Test;

public partial class OfflineDbContext_Tests
{
    #region Basic Deletes
    [Fact]
    public async Task QueueDelete_RemoteDefault_WorksAsync()
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();
        context.RemoteDefaultEntities.Add(entity);
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var start = DateTimeOffset.UtcNow;
        context.RemoteDefaultEntities.Remove(entity);
        await context.SaveChangesAsync();
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
    public async Task QueueDelete_RemotePath_WorksAsync()
    {
        var context = CreateContext();
        var entity = new RemotePathEntity();
        context.RemotePathEntities.Add(entity);
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var start = DateTimeOffset.UtcNow;
        context.RemotePathEntities.Remove(entity);
        await context.SaveChangesAsync();
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
    public async Task QueueDelete_OfflineOnly_WorksAsync()
    {
        var context = CreateContext();
        var entity = new OfflineOnlyEntity();
        context.OfflineOnlyEntities.Add(entity);
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        context.OfflineOnlyEntities.Remove(entity);
        await context.SaveChangesAsync();

        context.OfflineOperationsQueue.Should().HaveCount(0);
    }
    #endregion

    #region Queue Updates
    [Fact]
    public async Task QueueDelete_FollowedByAdd_ThrowsAsync()
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
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        context.RemoteDefaultEntities.Add(entity);
        Func<Task> act = async () => _ = await context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task QueueDelete_FollowedByDelete_ThrowsAsync()
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
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        context.RemoteDefaultEntities.Remove(entity);
        Func<Task> act = async () => _ = await context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task QueueDelete_FollowedByUpdate_ThrowsAsync()
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
        await context.SaveChangesAsync(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var originalQueueEntity = CopyOf(queueEntity);

        var start = DateTimeOffset.UtcNow;
        entity.Name = "Updated";
        context.RemoteDefaultEntities.Update(entity);
        Func<Task> act = async () => _ = await context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
    #endregion
}
