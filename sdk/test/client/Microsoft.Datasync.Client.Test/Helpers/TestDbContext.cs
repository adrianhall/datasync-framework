namespace Microsoft.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
public class TestDbContext : OfflineDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) 
    { 
    }

    public DbSet<RemoteDefaultEntity> RemoteDefaultEntities => Set<RemoteDefaultEntity>();
    public DbSet<RemotePathEntity> RemotePathEntities => Set<RemotePathEntity>();
    public DbSet<OfflineOnlyEntity> OfflineOnlyEntities => Set<OfflineOnlyEntity>();
}
