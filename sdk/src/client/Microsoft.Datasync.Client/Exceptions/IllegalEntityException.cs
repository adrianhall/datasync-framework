using Microsoft.Datasync.Client.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Datasync.Client;

/// <summary>
/// The Datasync Framework has discovered an entity that is considered
/// wrong.  Typically, this means that the ID of the entity is not a valid
/// type or that the framework cannot find the ID of the entity.
/// </summary>
[ExcludeFromCodeCoverage]
internal class IllegalEntityException : DatasyncFrameworkException
{
    /// <inheritdoc />
    public IllegalEntityException()
    {
    }

    /// <inheritdoc />
    public IllegalEntityException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public IllegalEntityException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <inheritdoc />
    public IllegalEntityException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
