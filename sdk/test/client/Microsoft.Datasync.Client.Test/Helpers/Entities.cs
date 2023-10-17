namespace Microsoft.Datasync.Client.Helpers;

[ExcludeFromCodeCoverage]
public class TestEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
}
