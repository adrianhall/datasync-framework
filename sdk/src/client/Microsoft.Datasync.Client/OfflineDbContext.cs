using Microsoft.Datasync.Client.Converters;
using Microsoft.Datasync.Client.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace Microsoft.Datasync.Client;

/// <summary>
/// The <see cref="OfflineDbContext"/> is a replacement base class for the <see cref="DbContext"/>
/// used by Entity Framework Core.
/// </summary>
/// <remarks>
/// You can use any driver that supports Entity Framework Core, but the one we use in testing is
/// SQLite (with some adjustments).
/// </remarks>
public class OfflineDbContext : DbContext
{
    /// <summary>
    /// A cache for the list of remote entities that have already been identified.
    /// </summary>
    private readonly Dictionary<Type, bool> remoteEntities = new();

    /// <inheritdoc />
    public OfflineDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    public override int SaveChanges()
        => SaveChanges(true);

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
        => SaveChanges(acceptAllChangesOnSuccess, QueueHandlerOptions.DefaultOptions);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, cancellationToken);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        => SaveChangesAsync(acceptAllChangesOnSuccess, QueueHandlerOptions.DefaultOptions, cancellationToken);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.TransactionId).HasConversion<GuidValueConverter>();
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.CreatedAt).HasConversion<DateTimeOffsetValueConverter>();
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.UpdatedAt).HasConversion<DateTimeOffsetValueConverter>();
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.OperationType).HasConversion<string>();
        modelBuilder.Entity<OfflineOperationsQueueEntity>().Property(e => e.EntityType).HasConversion<TypeValueConverter>();

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// The synchronization provider is used for maintaining the offline cache.  It specifically maintains the operations
    /// queue and the date/time that a <see cref="DbSet{TEntity}"/> was last synchronized to the server.
    /// </summary>
    public ISynchronizationProvider SynchronizationProvider { get; init; } = new NullSynchronizationProvider();

    /// <summary>
    /// The serializer options to use for serializing and deserializing entities.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <summary>
    /// The operations queue holds the operations that have been performed on a specific <see cref="DbSet{TEntity}"/> since
    /// the last synchronization operation.
    /// </summary>
    internal DbSet<OfflineOperationsQueueEntity> OfflineOperationsQueue => Set<OfflineOperationsQueueEntity>();

    /// <summary>
    /// The offline synchronizations table holds the last time a <see cref="DbSet{TEntity}"/> was synchronized to the server.
    /// </summary>
    internal DbSet<OfflineSynchronizationEntity> OfflineSynchronizations => Set<OfflineSynchronizationEntity>();

    /// <summary>
    /// Saves all changes made in this context to the underlying database.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Indicates whether <c>AcceptAllChanges()</c> is called after the changes
    /// have been sent successfully to the database.</param>
    /// <param name="options">The options for controlling queue behavior when adding a change to the <see cref="OfflineOperationsQueue"/>.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <exception cref="DbUpdateException">An error is encountered while saving to the database.</exception>
    /// <exception cref="DbUpdateConcurrencyException">A concurrency violation is encountered while saving to the database. A concurrency violation 
    /// occurs when an unexpected number of rows are affected during save. This is usually because the data in the database has been modified since 
    /// it was loaded into memory.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is cancelled.</exception>
    /// <exception cref="OfflineOperationsQueueException">An error occurred attempting to add an operation to the offline operations queue.</exception>
    internal int SaveChanges(bool acceptAllChangesOnSuccess, QueueHandlerOptions options)
    {
        if (ChangeTracker.AutoDetectChangesEnabled)
        {
            ChangeTracker.DetectChanges();
        }
        if (options.AddChangesToQueue)
        {
            AddChangesToQueue(ChangeTracker.Entries());
        }
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Saves all changes made in this context to the underlying database.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">Indicates whether <c>AcceptAllChanges()</c> is called after the changes
    /// have been sent successfully to the database.</param>
    /// <param name="options">The options for controlling queue behavior when adding a change to the <see cref="OfflineOperationsQueue"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    /// <exception cref="DbUpdateException">An error is encountered while saving to the database.</exception>
    /// <exception cref="DbUpdateConcurrencyException">A concurrency violation is encountered while saving to the database. A concurrency violation 
    /// occurs when an unexpected number of rows are affected during save. This is usually because the data in the database has been modified since 
    /// it was loaded into memory.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken"/> is cancelled.</exception>
    /// <exception cref="OfflineOperationsQueueException">An error occurred attempting to add an operation to the offline operations queue.</exception>
    internal async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, QueueHandlerOptions options, CancellationToken cancellationToken)
    {
        if (ChangeTracker.AutoDetectChangesEnabled)
        {
            ChangeTracker.DetectChanges();
        }
        if (options.AddChangesToQueue) 
        {
            AddChangesToQueue(ChangeTracker.Entries());
        }
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a set of changes to the operations queue (without saving them to the database).
    /// </summary>
    /// <param name="changes">The set of changes to process.</param>
    internal void AddChangesToQueue(IEnumerable<EntityEntry> changes)
    {
        var inscopeChanges = changes.Where(c => !IsOfflineDataset(c) && IsDataChange(c)).ToList();
        for (int idx = 0; idx < inscopeChanges.Count; idx++) 
        {
            AddChangeToQueue(inscopeChanges[idx]);
        }
    }

    /// <summary>
    /// Adds a single change to the operations queue (without saving it to the database).
    /// </summary>
    /// <param name="change">The change to process.</param>
    /// <exception cref="InvalidOperationException">Thrown if the change is invalid within the change tracker.</exception>
    internal void AddChangeToQueue(EntityEntry change)
    {
        if (!IsRemoteEntity(change.Entity))
        {
            return; // Don't add to queue.
        }

        Type entityType = change.Entity.GetType();
        string? entityId = GetEntityId(change.Entity);
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new InvalidEntityException("You must specify an ID for a remote entity.");
        }

        OfflineOperationsQueueEntity? existingEntity = OfflineOperationsQueue.FirstOrDefault(x => x.EntityType == entityType && x.EntityId == entityId);
        if (existingEntity is not null)
        {
            // Each operation type has a different behavior.
            if (existingEntity.OperationType == OperationType.Add)
            {
                switch (change.State)
                {
                    case EntityState.Added:
                        throw new InvalidOperationException("Cannot add an entity that has already been added to the offline operations queue.");
                    case EntityState.Modified:
                        existingEntity.SerializedEntity = SerializeEntity(change.Entity);
                        existingEntity.UpdatedAt = DateTimeOffset.UtcNow;
                        OfflineOperationsQueue.Update(existingEntity);
                        break;
                    case EntityState.Deleted:
                        OfflineOperationsQueue.Remove(existingEntity);
                        break;
                }
            }
            else if (existingEntity.OperationType == OperationType.Delete)
            {
                switch (change.State)
                {
                    case EntityState.Added:
                        throw new InvalidOperationException("Cannot add an entity after it has been deleted.  Either revert the delete or use a new entity ID.");
                    case EntityState.Modified:
                        throw new InvalidOperationException("Cannot modify an entity after it has been deleted.  Either revert the delete or use a new entity ID.");
                    case EntityState.Deleted:
                        throw new InvalidOperationException("Cannot delete an entity that has already been deleted.");
                }
            }
            else if (existingEntity.OperationType == OperationType.Replace)
            {
                switch (change.State)
                {
                    case EntityState.Added:
                        throw new InvalidOperationException("Cannot add an entity after it already exists.");
                    case EntityState.Modified:
                        existingEntity.SerializedEntity = SerializeEntity(change.Entity);
                        existingEntity.UpdatedAt = DateTimeOffset.UtcNow;
                        OfflineOperationsQueue.Update(existingEntity);
                        break;
                    case EntityState.Deleted:
                        existingEntity.OperationType = OperationType.Delete;
                        existingEntity.UpdatedAt = DateTimeOffset.UtcNow;
                        OfflineOperationsQueue.Update(existingEntity);
                        break;
                }
            }
        }
        else
        {
            var queueEntity = new OfflineOperationsQueueEntity
            {
                OperationType = GetOperationType(change.State),
                EntityId = entityId,
                EntityType = entityType,
                SerializedEntity = SerializeEntity(change.Entity)
            };
            OfflineOperationsQueue.Add(queueEntity);
        }
    }

    /// <summary>
    /// Retrieves the entity ID from the entity.
    /// </summary>
    /// <remarks>
    /// The entity ID is a string or Guid type.  It can be named Id or have a [Key] attribute.
    /// </remarks>
    /// <param name="entity">The entity to process.</param>
    /// <returns>The entity ID as a string.</returns>
    /// <exception cref="InvalidEntityException">if the entity does not have a suitable ID property.</exception>
    internal static string? GetEntityId(object entity)
    {
        // does the entity have a property with the key attribute?
        var properties = entity.GetType().GetProperties().Where(p => p.GetCustomAttribute<KeyAttribute>() is not null).ToList();
        if (properties.Count > 1)
        {
            throw new InvalidEntityException("Cannot have more than one key within a remote entity.");
        }
        if (properties.Count == 0)
        {
            properties = entity.GetType().GetProperties().Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToList();
            if (properties.Count > 1)
            {
                throw new InvalidEntityException("Cannot have more than one property with the name 'Id' within a remote entity.  Consider marking the correct one with the [Key] attribute.");
            }
            if (properties.Count == 0)
            {
                throw new InvalidEntityException("You must specify an 'Id' for a remote entity.");
            }
        }

        var idProperty = properties[0];
        return idProperty.PropertyType switch
        {
            Type t when t == typeof(string) => (string?)idProperty.GetValue(entity),
            Type t when t == typeof(Guid) => ((Guid?)idProperty.GetValue(entity))?.ToString("D"),
            _ => throw new InvalidEntityException("The ID of a remote entity must be a string or a Guid.")
        };
    }

    /// <summary>
    /// A conversion method to convert between an <see cref="EntityState"/> and an <see cref="OperationType"/>.
    /// </summary>
    /// <param name="state">the <see cref="EntityState"/> to convert.</param>
    /// <returns>The equivalent <see cref="OperationType"/></returns>
    internal static OperationType GetOperationType(EntityState state) => state switch
    {
        EntityState.Deleted => OperationType.Delete,
        EntityState.Modified => OperationType.Replace,
        EntityState.Added => OperationType.Add,
        _ => OperationType.Unknown
    };

    /// <summary>
    /// Determines if the change is for one of the internal datasets.
    /// </summary>
    /// <param name="change">The change to check.</param>
    /// <returns><c>true</c> if the change is for an offline dataset</returns>
    internal static bool IsOfflineDataset(EntityEntry change)
        => change.Entity is OfflineOperationsQueueEntity || change.Entity is OfflineSynchronizationEntity;

    /// <summary>
    /// Determines if the change is an actual data change.
    /// </summary>
    /// <param name="change">The change to check.</param>
    /// <returns><c>true</c> if the change modifies, adds, or deletes data.</returns>
    internal static bool IsDataChange(EntityEntry change)
        => change.State == EntityState.Added || change.State == EntityState.Deleted || change.State == EntityState.Modified;

    /// <summary>
    /// Determines if the entity is actually a remote entity.
    /// </summary>
    /// <remarks>
    /// An entity is a remote entity if:
    /// 
    /// - It has a [RemoteTableEntity] attribute.
    /// - There is an entity ID.  The ID can be a Guid or String, and must be either named Id or the property must have a [Key] attribute.
    /// 
    /// We cache remote entities in memory for faster access.
    /// </remarks>
    /// <param name="entity">The entity to check.</param>
    /// <returns><c>true</c> if the entity is a remote entity.</returns>
    /// <exception cref="InvalidEntityException">Thrown if there is a RemoteTableEntity attribute, but no suitable ID property could be found.</exception>
    internal bool IsRemoteEntity(object entity)
    {
        var entityType = entity.GetType();

        // Cache check - determine if we have already checked this entity type.
        if (remoteEntities.TryGetValue(entityType, out bool isRemote))
        {
            return isRemote;
        }

        // Determine if the entity has a RemoteTableEntity attribute.
        var remoteTableAttribute = entityType.GetCustomAttribute<RemoteTableEntityAttribute>();
        if (remoteTableAttribute is not null)
        {
            // The entity must also have an entity ID.  The <c>GetEntityId</c> method will throw.
            _ = GetEntityId(entity);

            // Store in the cache for later.
            remoteEntities.Add(entityType, true);
            return true;
        }

        // If there is no RemoteTableEntity attribute, then it's not a remote entity.
        remoteEntities.Add(entityType, false);
        return false;
    }

    /// <summary>
    /// Serializes the entity into a string for storage in the database.
    /// </summary>
    /// <param name="entity">The entity to serialize.</param>
    /// <returns>The serialized entity.</returns>
    /// <exception cref="InvalidEntityException">Thrown if the entity cannot be serialized.</exception>
    internal string SerializeEntity(object entity)
        => JsonSerializer.Serialize(entity, entity.GetType(), SerializerOptions);
}