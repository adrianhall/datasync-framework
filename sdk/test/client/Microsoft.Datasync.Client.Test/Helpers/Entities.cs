using System.ComponentModel.DataAnnotations;

namespace Microsoft.Datasync.Client.Test;

[ExcludeFromCodeCoverage]
[RemoteTableEntity]
public class RemoteDefaultEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
[RemoteTableEntity("/api/Alternate")]
public class  RemotePathEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
public class OfflineOnlyEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
}
