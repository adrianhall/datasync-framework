using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.Datasync.Client.Converters;

/// <summary>
/// A value converter for <see cref="Guid"/> values.
/// </summary>
internal class GuidValueConverter : ValueConverter<Guid, string>
{
    public GuidValueConverter() : base(v => v.ToString(), v => Guid.Parse(v))
    {
    }
}
