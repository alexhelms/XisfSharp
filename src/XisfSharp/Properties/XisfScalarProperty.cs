using System.Diagnostics;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public sealed class XisfScalarProperty : XisfProperty
{
    public object ScalarValue { get; }

    public override object Value => ScalarValue;

    public override string ToString() => ScalarValue?.ToString() ?? string.Empty;

    public XisfScalarProperty(string id, XisfPropertyType type, object value)
        : this(id, type, value, null, null)
    {
    }

    public XisfScalarProperty(string id, XisfPropertyType type, object value, string? comment)
        : this(id, type, value, comment, null)
    {
    }

    public XisfScalarProperty(string id, XisfPropertyType type, object value, string? comment, string? format)
        : base(id, type, comment, format)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (type != XisfPropertyType.Boolean &&
            type != XisfPropertyType.Int8 &&
            type != XisfPropertyType.UInt8 &&
            type != XisfPropertyType.Int16 &&
            type != XisfPropertyType.UInt16 &&
            type != XisfPropertyType.Int32 &&
            type != XisfPropertyType.UInt32 &&
            type != XisfPropertyType.Int64 &&
            type != XisfPropertyType.UInt64 &&
            type != XisfPropertyType.Float32 &&
            type != XisfPropertyType.Float64)
            throw new ArgumentException("Type must be a scalar type.", nameof(type));

        ScalarValue = value;
    }

    public static XisfScalarProperty Create<T>(string id, T value)
        where T : struct
        => Create(id, value, null, null);

    public static XisfScalarProperty Create<T>(string id, T value, string? comment)
        where T : struct
        => Create(id, value, comment, null);

    public static XisfScalarProperty Create<T>(string id, T value, string? comment, string? format)
        where T : struct
    {
        var type = typeof(T) switch
        {
            var t when t == typeof(bool) => XisfPropertyType.Boolean,
            var t when t == typeof(sbyte) => XisfPropertyType.Int8,
            var t when t == typeof(byte) => XisfPropertyType.UInt8,
            var t when t == typeof(short) => XisfPropertyType.Int16,
            var t when t == typeof(ushort) => XisfPropertyType.UInt16,
            var t when t == typeof(int) => XisfPropertyType.Int32,
            var t when t == typeof(uint) => XisfPropertyType.UInt32,
            var t when t == typeof(long) => XisfPropertyType.Int64,
            var t when t == typeof(ulong) => XisfPropertyType.UInt64,
            var t when t == typeof(float) => XisfPropertyType.Float32,
            var t when t == typeof(double) => XisfPropertyType.Float64,
            _ => throw new XisfException($"Unsupported scalar type: {typeof(T).FullName}")
        };

        return new XisfScalarProperty(id, type, value, comment, format);
    }
}
