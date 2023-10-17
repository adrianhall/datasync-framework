using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.Datasync.Client.Converters;

/// <summary>
/// A value converter for <see cref="DateTimeOffset"/> values.
/// </summary>
internal class DateTimeOffsetValueConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetValueConverter() : base(v => v.ToUniversalTime().UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero))
    {
    }
}
