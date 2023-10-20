using Microsoft.Datasync.Client.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Datasync.Client;

/// <summary>
/// The Datasync Framework has tried to execute an illegal operation.  This
/// is generally generated because something went wrong in the Entity Framework
/// Core Change Tracker (e.g. two deletes of the same entity).
/// </summary>
[ExcludeFromCodeCoverage]
internal class IllegalOperationException : DatasyncFrameworkException
{
    /// <inheritdoc />
    public IllegalOperationException()
    {
    }

    /// <inheritdoc />
    public IllegalOperationException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public IllegalOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <inheritdoc />
    public IllegalOperationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
