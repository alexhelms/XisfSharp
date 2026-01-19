using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public sealed class XisfVectorProperty : XisfProperty
{
    public Array Elements { get; }

    public override object Value => Elements;

    public int Length => Elements.Length;

    public override string ToString()
    {
        return $"Vector<{Type}> of {Elements.Length} elements";
    }

    public XisfVectorProperty(string id, XisfPropertyType type, Array elements)
        : this(id, type, elements, null, null)
    {
    }

    public XisfVectorProperty(string id, XisfPropertyType type, Array elements, string? comment)
        : this(id, type, elements, comment, null)
    { 
    }

    public XisfVectorProperty(
        string id,
        XisfPropertyType type,
        Array elements, 
        string? comment,
        string? format)
        : base(id, type, comment, format)
    {
        ArgumentNullException.ThrowIfNull(elements);

        if (type != XisfPropertyType.I8Vector &&
            type != XisfPropertyType.UI8Vector &&
            type != XisfPropertyType.I16Vector &&
            type != XisfPropertyType.UI16Vector &&
            type != XisfPropertyType.I32Vector &&
            type != XisfPropertyType.UI32Vector &&
            type != XisfPropertyType.I64Vector &&
            type != XisfPropertyType.UI64Vector &&
            type != XisfPropertyType.F32Vector &&
            type != XisfPropertyType.F64Vector)
            throw new ArgumentException("Type must be a vector type.", nameof(type));

        Elements = elements;
    }

    public T[] GetElements<T>() where T : struct => (T[])Elements;

    public byte[] GetBytes()
    {
        return Type switch
        {
            XisfPropertyType.I8Vector => GetBytesFromArray((sbyte[])Elements),
            XisfPropertyType.UI8Vector => (byte[])Elements,
            XisfPropertyType.I16Vector => GetBytesFromArray((short[])Elements),
            XisfPropertyType.UI16Vector => GetBytesFromArray((ushort[])Elements),
            XisfPropertyType.I32Vector => GetBytesFromArray((int[])Elements),
            XisfPropertyType.UI32Vector => GetBytesFromArray((uint[])Elements),
            XisfPropertyType.I64Vector => GetBytesFromArray((long[])Elements),
            XisfPropertyType.UI64Vector => GetBytesFromArray((ulong[])Elements),
            XisfPropertyType.F32Vector => GetBytesFromArray((float[])Elements),
            XisfPropertyType.F64Vector => GetBytesFromArray((double[])Elements),
            _ => throw new XisfException($"Cannot get bytes for type: {Type}")
        };
    }

    private static byte[] GetBytesFromArray<T>(T[] array)
        where T : struct
    {
        var bytes = new byte[array.Length * Unsafe.SizeOf<T>()];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static XisfVectorProperty Create<T>(string id, T[] elements)
        where T : struct
        => Create(id, elements, null, null);

    public static XisfVectorProperty Create<T>(string id, T[] elements, string? comment)
        where T : struct
        => Create(id, elements, comment, null);

    public static XisfVectorProperty Create<T>(string id, T[] elements, string? comment, string? format)
        where T : struct
    {
        var type = typeof(T) switch
        {
            var t when t == typeof(sbyte) => XisfPropertyType.I8Vector,
            var t when t == typeof(byte) => XisfPropertyType.UI8Vector,
            var t when t == typeof(short) => XisfPropertyType.I16Vector,
            var t when t == typeof(ushort) => XisfPropertyType.UI16Vector,
            var t when t == typeof(int) => XisfPropertyType.I32Vector,
            var t when t == typeof(uint) => XisfPropertyType.UI32Vector,
            var t when t == typeof(long) => XisfPropertyType.I64Vector,
            var t when t == typeof(ulong) => XisfPropertyType.UI64Vector,
            var t when t == typeof(float) => XisfPropertyType.F32Vector,
            var t when t == typeof(double) => XisfPropertyType.F64Vector,
            _ => throw new XisfException($"Unsupported vector element type: {typeof(T).FullName}")
        };

        return new XisfVectorProperty(id, type, elements, comment, format);
    }
}