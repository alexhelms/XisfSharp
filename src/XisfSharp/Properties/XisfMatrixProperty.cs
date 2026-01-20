using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public class XisfMatrixProperty : XisfProperty
{
    /// <summary>
    /// Gets the array of elements in the matrix.
    /// </summary>
    public Array Elements { get; }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public override object Value => Elements;

    /// <summary>
    /// Gets the number of rows in the matrix.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// Gets the number of columns in the matrix.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Gets the total number of elements in the matrix (rows × columns).
    /// </summary>
    public int Length => Rows * Columns;

    public override string ToString()
    {
        return $"Matrix<{Type}> [{Rows}×{Columns}]";
    }

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="type">The matrix type of the property value.</param>
    /// <param name="elements">The rank 1 array of matrix elements in row-major order. Cannot be null.</param>
    /// <param name="rows">The number of rows in the matrix. Must be non-negative.</param>
    /// <param name="columns">The number of columns in the matrix. Must be non-negative.</param>
    public XisfMatrixProperty(
        string id,
        XisfPropertyType type,
        Array elements,
        int rows,
        int columns)
        : this(id, type, elements, rows, columns, null, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="type">The matrix type of the property value.</param>
    /// <param name="elements">The rank 1 array of matrix elements in row-major order. Cannot be null.</param>
    /// <param name="rows">The number of rows in the matrix. Must be non-negative.</param>
    /// <param name="columns">The number of columns in the matrix. Must be non-negative.</param>
    /// <param name="comment">An optional comment describing the property.</param>
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

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="type">The matrix type of the property value.</param>
    /// <param name="elements">The rank 1 array of matrix elements in row-major order. Cannot be null.</param>
    /// <param name="rows">The number of rows in the matrix. Must be non-negative.</param>
    /// <param name="columns">The number of columns in the matrix. Must be non-negative.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
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
        ArgumentOutOfRangeException.ThrowIfNegative(rows);
        ArgumentOutOfRangeException.ThrowIfNegative(columns);

        if (elements.Rank is not (1 or 2))
            throw new InvalidOperationException($"Unsupported array rank: {elements.Rank}");

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

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a 2D array with inferred dimensions.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 2 array of matrix elements. Cannot be null.</param>
    public XisfMatrixProperty(string id, Array elements)
        : this(id, elements, null, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a 2D array with inferred dimensions.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 2 array of matrix elements. Cannot be null.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public XisfMatrixProperty(string id, Array elements, string? comment)
        : this(id, elements, comment, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a 2D array with inferred dimensions.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 2 array of matrix elements. Cannot be null.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
    public XisfMatrixProperty(string id, Array elements, string? comment, string? format)
        : base(id, DetermineMatrixType(elements), comment, format)
    {
        ArgumentNullException.ThrowIfNull(elements);

        if (elements.Rank == 2)
        {
            // Store 2D array directly without flattening
            Rows = elements.GetLength(0);
            Columns = elements.GetLength(1);
            Elements = elements;
        }
        else if (elements.Rank == 1)
        {
            throw new ArgumentException(
                "For 1D arrays, use constructor with explicit rows and columns parameters.",
                nameof(elements));
        }
        else
        {
            throw new ArgumentException(
                $"Array must be 2-dimensional. Provided array has rank {elements.Rank}.",
                nameof(elements));
        }
    }

    private static XisfPropertyType DetermineMatrixType(Array array)
    {
        ArgumentNullException.ThrowIfNull(array);

        var elementType = array.GetType().GetElementType()
            ?? throw new XisfException("Unable to determine array element type.");

        return elementType switch
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
            _ => throw new XisfException($"Unsupported matrix element type: {elementType.FullName}")
        };
    }

    /// <summary>
    /// Gets the matrix elements as a strongly-typed 1D array in row-major order.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the matrix elements.</typeparam>
    /// <returns>The matrix elements as a flattened 1D array.</returns>
    public T[] GetElementsAs1D<T>() where T : unmanaged
    {
        if (Elements.Rank == 1)
        {
            return (T[])Elements;
        }
        else if (Elements.Rank == 2)
        {
            var array2d = (T[,])Elements;
            var flat = new T[Rows * Columns];
            int index = 0;
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    flat[index++] = array2d[i, j];
                }
            }
            return flat;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported array rank: {Elements.Rank}");
        }
    }

    /// <summary>
    /// Gets the matrix elements as a strongly-typed 2D array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the matrix elements.</typeparam>
    /// <returns>The matrix elements as a 2D array with dimensions [Rows, Columns].</returns>
    public T[,] GetElementsAs2D<T>() where T : unmanaged
    {
        if (Elements.Rank == 2)
        {
            return (T[,])Elements;
        }
        else if (Elements.Rank == 1)
        {
            var array1d = (T[])Elements;
            var array2d = new T[Rows, Columns];
            int index = 0;
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    array2d[i, j] = array1d[index++];
                }
            }
            return array2d;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported array rank: {Elements.Rank}");
        }
    }

    internal byte[] GetBytes()
    {
        if (Elements.Rank == 1)
        {
            return Type switch
            {
                XisfPropertyType.I8Matrix => GetBytesFromArray((sbyte[])Elements),
                XisfPropertyType.UI8Matrix => GetBytesFromArray((byte[])Elements),
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
        else if (Elements.Rank == 2)
        {
            return Type switch
            {
                XisfPropertyType.I8Matrix => GetBytesFromArray((sbyte[,])Elements),
                XisfPropertyType.UI8Matrix => GetBytesFromArray((byte[,])Elements),
                XisfPropertyType.I16Matrix => GetBytesFromArray((short[,])Elements),
                XisfPropertyType.UI16Matrix => GetBytesFromArray((ushort[,])Elements),
                XisfPropertyType.I32Matrix => GetBytesFromArray((int[,])Elements),
                XisfPropertyType.UI32Matrix => GetBytesFromArray((uint[,])Elements),
                XisfPropertyType.I64Matrix => GetBytesFromArray((long[,])Elements),
                XisfPropertyType.UI64Matrix => GetBytesFromArray((ulong[,])Elements),
                XisfPropertyType.F32Matrix => GetBytesFromArray((float[,])Elements),
                XisfPropertyType.F64Matrix => GetBytesFromArray((double[,])Elements),
                _ => throw new XisfException($"Cannot get bytes for type: {Type}")
            };
        }
        else
        {
            throw new InvalidOperationException($"Unsupported array rank: {Elements.Rank}");
        }
    }

    private static byte[] GetBytesFromArray<T>(T[,] array) where T : unmanaged
    {
        if (array.Length == 0)
            return [];

        ref T firstElement = ref array[0, 0];
        var span = MemoryMarshal.CreateSpan(ref firstElement, array.Length);
        return MemoryMarshal.AsBytes(span).ToArray();
    }

    private static byte[] GetBytesFromArray<T>(T[] array) where T : unmanaged
    {
        return MemoryMarshal.AsBytes(array).ToArray();
    }

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a strongly-typed 1D array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the matrix elements.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 1 array of matrix elements in row-major order.</param>
    /// <param name="rows">The number of rows in the matrix.</param>
    /// <param name="columns">The number of columns in the matrix.</param>
    public static XisfMatrixProperty Create<T>(
        string id,
        T[] elements,
        int rows,
        int columns)
        where T : unmanaged
        => Create(id, elements, rows, columns, null, null);

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a strongly-typed 1D array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the matrix elements.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 1 array of matrix elements in row-major order.</param>
    /// <param name="rows">The number of rows in the matrix.</param>
    /// <param name="columns">The number of columns in the matrix.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public static XisfMatrixProperty Create<T>(
        string id,
        T[] elements,
        int rows,
        int columns,
        string? comment)
        where T : unmanaged
        => Create(id, elements, rows, columns, comment, null);

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a strongly-typed 1D array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the matrix elements.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 1 array of matrix elements in row-major order.</param>
    /// <param name="rows">The number of rows in the matrix.</param>
    /// <param name="columns">The number of columns in the matrix.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
    public static XisfMatrixProperty Create<T>(
        string id,
        T[] elements,
        int rows,
        int columns,
        string? comment,
        string? format)
        where T : unmanaged
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

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a strongly-typed 2D array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the matrix elements.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 2 array of matrix elements.</param>
    public static XisfMatrixProperty Create<T>(string id, T[,] elements)
        where T : unmanaged
        => Create(id, elements, null, null);

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a strongly-typed 2D array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the matrix elements.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 2 array of matrix elements.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public static XisfMatrixProperty Create<T>(string id, T[,] elements, string? comment)
        where T : unmanaged
        => Create(id, elements, comment, null);

    /// <summary>
    /// Create a new <see cref="XisfMatrixProperty"/> from a strongly-typed 2D array.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the matrix elements.</typeparam>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="elements">The rank 2 array of matrix elements.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
    public static XisfMatrixProperty Create<T>(string id, T[,] elements, string? comment, string? format)
        where T : unmanaged
    {
        return new XisfMatrixProperty(id, elements, comment, format);
    }
}