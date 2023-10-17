namespace Microsoft.Datasync.Client;

[ExcludeFromCodeCoverage]
public class OfflineDbContext_Tests : BaseUnitTest
{
    [Fact]
    public void Ctor_CanCreateContext()
    {
        var context = CreateContext();
        context.Should().NotBeNull();
        context.SynchronizationProvider.Should().NotBeNull();
        context.QueueHandler.Should().NotBeNull();
    }

    [Fact]
    public void Add_SaveChanges_AddsToQueue()
    {
        var context = CreateContext();
        var entity = new TestEntity();
        context.Add(entity);

        context.SaveChanges();

        context.OfflineOperationsQueue.Should().HaveCount(1);
        // TODO: Make sure the entity is correct.
    }

}