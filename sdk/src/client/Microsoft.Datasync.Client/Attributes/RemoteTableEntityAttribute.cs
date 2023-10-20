namespace Microsoft.Datasync.Client;

/// <summary>
/// An attribute that is used to decorate a remote entity that will be
/// synchronized to a remote table.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RemoteTableEntityAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteTableEntityAttribute"/>
    /// with an empty path (which means that the type of the entity will be used
    /// to construct a path at runtime).
    /// </summary>
    public RemoteTableEntityAttribute()
    {
        Path = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteTableEntityAttribute"/>
    /// with a specific path (which means that the path is fixed at compile time).
    /// </summary>
    /// <param name="path">The path of the entity table on the remote service.</param>
    public RemoteTableEntityAttribute(string path)
    {
        // TODO: Path must be a valid path according to RFC2396.
        Path = path;
    }

    /// <summary>
    /// The path to use, or an empty string if the path should be calculated.
    /// </summary>
    public string Path { get; }
}
