using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.Datasync.Client.Exceptions;

/// <summary>
/// The base exception for all custom exceptions thrown by the Datasync Framework.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DatasyncFrameworkException : ApplicationException
{
    /// <inheritdoc />
    protected DatasyncFrameworkException()
    {
    }

    /// <inheritdoc />
    protected DatasyncFrameworkException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    protected DatasyncFrameworkException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <inheritdoc />
    protected DatasyncFrameworkException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
