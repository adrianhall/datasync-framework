namespace Microsoft.Datasync.Client.Helpers;

[ExcludeFromCodeCoverage]
public class TestDbContext : OfflineDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) 
    { 
    }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}
