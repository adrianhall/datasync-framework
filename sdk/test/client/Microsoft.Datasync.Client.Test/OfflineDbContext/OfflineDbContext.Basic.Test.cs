using Microsoft.Datasync.Client.Sync;
using NSubstitute;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Microsoft.Datasync.Client.Test;

#nullable disable

[ExcludeFromCodeCoverage]
public partial class OfflineDbContext_Tests : BaseUnitTest
{
    [Fact]
    public void Ctor_CanCreateContext()
    {
        var context = CreateContext();
        context.Should().NotBeNull();
        context.SynchronizationProvider.Should().NotBeNull();
        context.SerializerOptions.Should().NotBeNull();
    }

    #region Properties
    [Fact]
    public void CanSetSerializerOptions()
    {
        var context = CreateContext();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General);
        context.SerializerOptions = options;
        context.SerializerOptions.Should().BeSameAs(options);
    }

    [Fact]
    public void CanSetSynchronizationProvider()
    {
        var context = CreateContext();
        var provider = Substitute.For<ISynchronizationProvider>();
        context.SynchronizationProvider = provider;
        context.SynchronizationProvider.Should().BeSameAs(provider);
    }
    #endregion

    #region AddChangeToQueue
    [Theory]
    [InlineData(OperationType.Add)]
    [InlineData(OperationType.Delete)]
    [InlineData(OperationType.Replace)]
    public void AddChangeToQueue_Unchanges_DoesntChangeQueue(OperationType op)
    {
        var context = CreateContext();
        var entity = new RemoteDefaultEntity();
        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = op,
            EntityType = typeof(RemoteDefaultEntity),
            EntityId = entity.Id,
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.RemoteDefaultEntities.Add(entity);
        context.SaveChanges(true, new QueueHandlerOptions { AddChangesToQueue = false });

        var entityEntry = context.Entry(entity);
        context.AddChangeToQueue(entityEntry);
        context.OfflineOperationsQueue.Should().HaveCount(1);
    }
    #endregion

    #region GetEntityId
    [Theory]
    [InlineData(typeof(Bad_MultiKey))]
    [InlineData(typeof(Bad_MultipleIds))]
    [InlineData(typeof(Bad_NoIds))]
    [InlineData(typeof(Bad_WrongIdType))]
    [InlineData(typeof(Bad_WrongKeyType))]
    [InlineData(typeof(Bad_NullId))]
    public void GetEntityId_InvalidObjects_Throws(Type type)
    {
        object entity = Activator.CreateInstance(type)!;
        Action act = () => _ = OfflineDbContext.GetEntityId(entity);
        act.Should().Throw<InvalidEntityException>();
    }

    [Theory]
    [InlineData(typeof(Good_GuidKey), "564dce31-0f3c-43af-80dc-ae751590e57f")]
    [InlineData(typeof(Good_stringKey), "pickme")]
    [InlineData(typeof(Good_GuidId), "564dce31-0f3c-43af-80dc-ae751590e57f")]
    [InlineData(typeof(Good_stringId), "pickme")]
    [InlineData(typeof(Good_PreferKey), "564dce31-0f3c-43af-80dc-ae751590e57f")]
    public void GetEntityId_ValidObjects_Works(Type type, string expected)
    {
        object entity = Activator.CreateInstance(type)!;
        string? id = OfflineDbContext.GetEntityId(entity);
        id.Should().NotBeNullOrEmpty().And.Be(expected);
    }
    #endregion

    #region GetOperationsQueueEntity
    [Theory]
    [InlineData(typeof(RemoteDefaultEntity), "matching-id")]
    [InlineData(typeof(RemotePathEntity), "non-matching-id")]
    public void GetOperationsQueueEntity_ReturnsNull_WhenNoMatch(Type type, string id)
    {
        var context = CreateContext();
        var entity = new RemotePathEntity();
        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Add,
            EntityType = typeof(RemotePathEntity),
            EntityId = "matching-id",
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.SaveChanges();

        var actual = context.GetOperationsQueueEntity(type, id);
        actual.Should().BeNull();
    }

    [Fact]
    public void GetOperationsQueueEntity_ReturnsEntity_WhenMatched()
    {
        var context = CreateContext();
        var entity = new RemotePathEntity();
        var queueEntity = new OfflineOperationsQueueEntity()
        {
            OperationType = OperationType.Add,
            EntityType = typeof(RemotePathEntity),
            EntityId = "matching-id",
            SerializedEntity = JsonSerializer.Serialize(entity, context.SerializerOptions)
        };
        context.OfflineOperationsQueue.Add(queueEntity);
        context.SaveChanges();

        var actual = context.GetOperationsQueueEntity(typeof(RemotePathEntity), "matching-id");
        actual.Should().BeEquivalentTo(queueEntity);
    }
    #endregion

    #region GetOperationType
    [Theory]
    [InlineData(EntityState.Unchanged)]
    [InlineData(EntityState.Detached)]
    public void GetOperationType_ReturnsUnknown(EntityState sut)
    {
        OperationType actual = OfflineDbContext.GetOperationType(sut);
        actual.Should().Be(OperationType.Unknown);
    }
    #endregion

    #region IsRemoteEntity
    [Theory]
    [InlineData(typeof(RemoteDefaultEntity), true)]
    [InlineData(typeof(RemotePathEntity), true)]
    [InlineData(typeof(OfflineOnlyEntity), false)]
    public void IsRemoteEntity_Works(Type type, bool expected)
    {
        var context = CreateContext();
        var entity = Activator.CreateInstance(type)!;
        bool actual = context.IsRemoteEntity(entity);
        actual.Should().Be(expected);

        // Repeat for the cache trigger.
        bool cachedActual = context.IsRemoteEntity(entity);
        cachedActual.Should().Be(expected);
    }
    #endregion

    #region Helpers - Bad Entity Types
    public class Bad_MultiKey 
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        [Key]
        public string Name { get; set; } = string.Empty;
    }

    public class Bad_MultipleIds
    {
        public string Id { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
    }

    public class Bad_NoIds
    {
        public string MyId { get; set; } = string.Empty;
    }

    public class Bad_WrongIdType
    {
        public int Id { get; set; }
    }

    public class Bad_WrongKeyType
    {
        [Key]
        public int MyId { get; set; }
    }

    public class Bad_NullId
    {
        [Key]
        public string Id { get; set; } = null;
    }
    #endregion

    #region Helpers - Good Entity Types
    public class Good_GuidKey
    {
        [Key]
        public Guid MyId { get; set; } = Guid.Parse("564dce31-0f3c-43af-80dc-ae751590e57f");
    }

    public class Good_stringKey
    {
        [Key]
        public string MyId { get; set; } = "pickme";
    }

    public class Good_GuidId
    {
        public Guid Id { get; set; } = Guid.Parse("564dce31-0f3c-43af-80dc-ae751590e57f");
    }

    public class Good_stringId
    {
        public string Id { get; set; } = "pickme";
    }

    public class Good_PreferKey
    {
        [Key]
        public Guid MyId { get; set; } = Guid.Parse("564dce31-0f3c-43af-80dc-ae751590e57f");

        public string Id { get; set; } = "don't pick me";
    }
    #endregion
}
