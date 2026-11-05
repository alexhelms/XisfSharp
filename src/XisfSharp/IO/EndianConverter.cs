using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XisfSharp.IO;

internal static class EndianConverter
{
    /// <summary>
    /// Reverses the byte order of every element in <paramref name="data"/> in place.
    /// </summary>
    /// <param name="data">Bytes of a sequence of elements. The length must be a multiple of <paramref name="elementSize"/>.</param>
    /// <param name="elementSize">Size in bytes of each element: 1, 2, 4 or 8.</param>
    internal static void ReverseEndiannessInPlace(Span<byte> data, int elementSize)
    {
        if (elementSize is not (1 or 2 or 4 or 8))
            throw new ArgumentOutOfRangeException(nameof(elementSize), elementSize, "Element size must be 1, 2, 4 or 8.");

        if (data.Length % elementSize != 0)
            throw new ArgumentException($"Data length {data.Length} is not a multiple of the element size {elementSize}.", nameof(data));

        switch (elementSize)
        {
            case 1:
                break;

            case 2:
                var shorts = MemoryMarshal.Cast<byte, ushort>(data);
                BinaryPrimitives.ReverseEndianness(shorts, shorts);
                break;

            case 4:
                var ints = MemoryMarshal.Cast<byte, uint>(data);
                BinaryPrimitives.ReverseEndianness(ints, ints);
                break;

            case 8:
                var longs = MemoryMarshal.Cast<byte, ulong>(data);
                BinaryPrimitives.ReverseEndianness(longs, longs);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(elementSize), elementSize, "Element size must be 1, 2, 4 or 8.");
        }
    }

    internal static void ReverseEndianness<T>(ReadOnlySpan<byte> source, Span<T> destination)
        where T : unmanaged
    {
        var bytesPerElement = Unsafe.SizeOf<T>();

        if (typeof(T) == typeof(ushort))
        {
            var shorts = MemoryMarshal.Cast<T, ushort>(destination);
            for (int i = 0; i < shorts.Length; i++)
            {
                shorts[i] = BinaryPrimitives.ReverseEndianness(
                    BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(i * 2, 2)));
            }
        }
        else if (typeof(T) == typeof(short))
        {
            var shorts = MemoryMarshal.Cast<T, short>(destination);
            for (int i = 0; i < shorts.Length; i++)
            {
                shorts[i] = BinaryPrimitives.ReverseEndianness(
                    BinaryPrimitives.ReadInt16LittleEndian(source.Slice(i * 2, 2)));
            }
        }
        else if (typeof(T) == typeof(uint))
        {
            var ints = MemoryMarshal.Cast<T, uint>(destination);
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = BinaryPrimitives.ReverseEndianness(
                    BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(i * 4, 4)));
            }
        }
        else if (typeof(T) == typeof(int))
        {
            var ints = MemoryMarshal.Cast<T, int>(destination);
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = BinaryPrimitives.ReverseEndianness(
                    BinaryPrimitives.ReadInt32LittleEndian(source.Slice(i * 4, 4)));
            }
        }
        else if (typeof(T) == typeof(ulong))
        {
            var longs = MemoryMarshal.Cast<T, ulong>(destination);
            for (int i = 0; i < longs.Length; i++)
            {
                longs[i] = BinaryPrimitives.ReverseEndianness(
                    BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(i * 8, 8)));
            }
        }
        else if (typeof(T) == typeof(long))
        {
            var longs = MemoryMarshal.Cast<T, long>(destination);
            for (int i = 0; i < longs.Length; i++)
            {
                longs[i] = BinaryPrimitives.ReverseEndianness(
                    BinaryPrimitives.ReadInt64LittleEndian(source.Slice(i * 8, 8)));
            }
        }
        else if (typeof(T) == typeof(float))
        {
            var floats = MemoryMarshal.Cast<T, float>(destination);
            for (int i = 0; i < floats.Length; i++)
            {
                var bits = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(i * 4, 4));
                var reversed = BinaryPrimitives.ReverseEndianness(bits);
                floats[i] = BitConverter.UInt32BitsToSingle(reversed);
            }
        }
        else if (typeof(T) == typeof(double))
        {
            var doubles = MemoryMarshal.Cast<T, double>(destination);
            for (int i = 0; i < doubles.Length; i++)
            {
                var bits = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(i * 8, 8));
                var reversed = BinaryPrimitives.ReverseEndianness(bits);
                doubles[i] = BitConverter.UInt64BitsToDouble(reversed);
            }
        }
        else
        {
            throw new NotImplementedException("Endianness reversal not implemented for type " + typeof(T).Name);
        }
    }
}