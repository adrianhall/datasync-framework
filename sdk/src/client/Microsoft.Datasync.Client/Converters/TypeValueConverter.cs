using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.Datasync.Client.Converters;

/// <summary>
/// A value converter for <see cref="Type"/> values.
/// </summary>
internal class TypeValueConverter : ValueConverter<Type, string>
{
    public TypeValueConverter() : base(v => v.FullName!, v => Type.GetType(v)!)
    {
    }
}
