using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XisfSharp.IO;

internal static class EndianConverter
{
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