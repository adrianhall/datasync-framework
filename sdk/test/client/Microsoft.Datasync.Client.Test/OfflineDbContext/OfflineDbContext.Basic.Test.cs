using Microsoft.Datasync.Client.Sync;
using System.ComponentModel.DataAnnotations;

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
