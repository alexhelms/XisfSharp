using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public class XisfMatrixProperty : XisfProperty
{
    public Array Elements { get; }

    public override object Value => Elements;

    public int Rows { get; }

    public int Columns { get; }

    public int Length => Rows * Columns;

    public override string ToString()
    {
        return $"Matrix<{Type}> [{Rows}×{Columns}]";
    }

    public XisfMatrixProperty(
        string id,
        XisfPropertyType type,
        Array elements,
        int rows,
        int columns)
        : this(id, type, elements, rows, columns, null, null)
    {
    }

    public XisfMatrixProperty(
        string id,
        XisfPropertyType type,
        Array elements,
        int rows,
        int columns,
        string? comment)
        : this(id, type, elements, rows, columns, comment, null)
    { 
    }

    public XisfMatrixProperty(
        string id,
        XisfPropertyType type,
        Array elements,
        int rows, 
        int columns,
        string? comment,
        string? format)
        : base(id, type, comment, format)
    {
        ArgumentNullException.ThrowIfNull(elements);

        if (type != XisfPropertyType.I8Matrix &&
            type != XisfPropertyType.UI8Matrix &&
            type != XisfPropertyType.I16Matrix &&
            type != XisfPropertyType.UI16Matrix &&
            type != XisfPropertyType.I32Matrix &&
            type != XisfPropertyType.UI32Matrix &&
            type != XisfPropertyType.I64Matrix &&
            type != XisfPropertyType.UI64Matrix &&
            type != XisfPropertyType.F32Matrix &&
            type != XisfPropertyType.F64Matrix)
            throw new ArgumentException("Type must be a vector type.", nameof(type));

        if (elements.Length != rows * columns)
            throw new ArgumentException("Element count must equal rows x columns", nameof(elements));

        Elements = elements;
        Rows = rows;
        Columns = columns;
    }

    public T[] GetElements<T>() where T : struct => (T[])Elements;

    public byte[] GetBytes()
    {
        return Type switch
        {
            XisfPropertyType.I8Matrix => GetBytesFromArray((sbyte[])Elements),
            XisfPropertyType.UI8Matrix => (byte[])Elements,
            XisfPropertyType.I16Matrix => GetBytesFromArray((short[])Elements),
            XisfPropertyType.UI16Matrix => GetBytesFromArray((ushort[])Elements),
            XisfPropertyType.I32Matrix => GetBytesFromArray((int[])Elements),
            XisfPropertyType.UI32Matrix => GetBytesFromArray((uint[])Elements),
            XisfPropertyType.I64Matrix => GetBytesFromArray((long[])Elements),
            XisfPropertyType.UI64Matrix => GetBytesFromArray((ulong[])Elements),
            XisfPropertyType.F32Matrix => GetBytesFromArray((float[])Elements),
            XisfPropertyType.F64Matrix => GetBytesFromArray((double[])Elements),
            _ => throw new XisfException($"Cannot get bytes for type: {Type}")
        };
    }

    private static byte[] GetBytesFromArray<T>(T[] array) where T : struct
    {
        var bytes = new byte[array.Length * Unsafe.SizeOf<T>()];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static XisfMatrixProperty Create<T>(
        string id,
        T[] elements,
        int rows,
        int columns)
        where T : struct
        => Create(id, elements, rows, columns, null, null);

    public static XisfMatrixProperty Create<T>(
        string id,
        T[] elements,
        int rows,
        int columns,
        string? comment)
        where T : struct
        => Create(id, elements, rows, columns, comment, null);

    public static XisfMatrixProperty Create<T>(
        string id, 
        T[] elements,
        int rows, 
        int columns,
        string? comment,
        string? format)
        where T : struct
    {
        var type = typeof(T) switch
        {
            var t when t == typeof(sbyte) => XisfPropertyType.I8Matrix,
            var t when t == typeof(byte) => XisfPropertyType.UI8Matrix,
            var t when t == typeof(short) => XisfPropertyType.I16Matrix,
            var t when t == typeof(ushort) => XisfPropertyType.UI16Matrix,
            var t when t == typeof(int) => XisfPropertyType.I32Matrix,
            var t when t == typeof(uint) => XisfPropertyType.UI32Matrix,
            var t when t == typeof(long) => XisfPropertyType.I64Matrix,
            var t when t == typeof(ulong) => XisfPropertyType.UI64Matrix,
            var t when t == typeof(float) => XisfPropertyType.F32Matrix,
            var t when t == typeof(double) => XisfPropertyType.F64Matrix,
            _ => throw new XisfException($"Unsupported matrix element type: {typeof(T).FullName}")
        };

        return new XisfMatrixProperty(id, type, elements, rows, columns, comment, format);
    }
}