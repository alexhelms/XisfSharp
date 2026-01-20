using System.Diagnostics;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public sealed class XisfScalarProperty : XisfProperty
{
    /// <summary>
    /// Gets the scalar value of the property.
    /// </summary>
    public object ScalarValue { get; }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public override object Value => ScalarValue;

    public override string ToString() => ScalarValue?.ToString() ?? string.Empty;

    /// <summary>
    /// Create a new <see cref="XisfScalarProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="type">The scalar type of the property value.</param>
    /// <param name="value">The scalar value. Cannot be null.</param>
    public XisfScalarProperty(string id, XisfPropertyType type, object value)
        : this(id, type, value, null, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfScalarProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="type">The scalar type of the property value.</param>
    /// <param name="value">The scalar value. Cannot be null.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public XisfScalarProperty(string id, XisfPropertyType type, object value, string? comment)
        : this(id, type, value, comment, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfScalarProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="type">The scalar type of the property value.</param>
    /// <param name="value">The scalar value. Cannot be null.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
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

    /// <summary>
    /// Creates a new <see cref="XisfScalarProperty"/> from a strongly-typed value.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the scalar value.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The scalar value.</param>
    public static XisfScalarProperty Create<T>(string id, T value)
        where T : unmanaged
        => Create(id, value, null, null);

    /// <summary>
    /// Creates a new <see cref="XisfScalarProperty"/> from a strongly-typed value.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the scalar value.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The scalar value.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public static XisfScalarProperty Create<T>(string id, T value, string? comment)
        where T : unmanaged
        => Create(id, value, comment, null);

    /// <summary>
    /// Creates a new <see cref="XisfScalarProperty"/> from a strongly-typed value.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the scalar value.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The scalar value.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
    public static XisfScalarProperty Create<T>(string id, T value, string? comment, string? format)
        where T : unmanaged
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
