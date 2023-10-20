using System.ComponentModel.DataAnnotations;

namespace Microsoft.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
[RemoteTableEntity]
public class RemoteDefaultEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
}

[ExcludeFromCodeCoverage]
[RemoteTableEntity("/api/Alternate")]
public class  RemotePathEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
}

[ExcludeFromCodeCoverage]
public class OfflineOnlyEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
}
