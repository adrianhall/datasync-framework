namespace Microsoft.Datasync.Client;

[ExcludeFromCodeCoverage]
public abstract class BaseUnitTest
{
    public TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new TestDbContext(options);
        return context;
    }
}
