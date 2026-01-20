using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace XisfSharp.IO;

internal static class XisfBinaryReader
{
    public static T ReadElement<T>(ReadOnlySpan<byte> bytes, bool isLittleEndian)
        where T : unmanaged
    {
        bool needsReverse = isLittleEndian != BitConverter.IsLittleEndian;

        if (needsReverse)
        {
            Span<byte> reversed = new byte[bytes.Length];
            bytes.CopyTo(reversed);
            reversed.Reverse();
            bytes = reversed;
        }

        return typeof(T).Name switch
        {
            // TODO: Can I do this without boxing?
            nameof(Byte) => (T)(object)bytes[0],
            nameof(SByte) => (T)(object)(sbyte)bytes[0],
            nameof(UInt16) => (T)(object)BinaryPrimitives.ReadUInt16LittleEndian(bytes),
            nameof(Int16) => (T)(object)BinaryPrimitives.ReadInt16LittleEndian(bytes),
            nameof(UInt32) => (T)(object)BinaryPrimitives.ReadUInt32LittleEndian(bytes),
            nameof(Int32) => (T)(object)BinaryPrimitives.ReadInt32LittleEndian(bytes),
            nameof(UInt64) => (T)(object)BinaryPrimitives.ReadUInt64LittleEndian(bytes),
            nameof(Int64) => (T)(object)BinaryPrimitives.ReadInt64LittleEndian(bytes),
            nameof(Single) => (T)(object)BitConverter.ToSingle(bytes),
            nameof(Double) => (T)(object)BitConverter.ToDouble(bytes),
            _ => throw new NotSupportedException($"Type {typeof(T).Name} not supported"),
        };
    }

    public static async Task<T[]> ReadElementsAsync<T>(
        Stream stream,
        long position,
        int count,
        bool isLittleEndian,
        CancellationToken cancellation = default)
        where T : unmanaged
    {
        stream.Position = position;
        var elements = GC.AllocateUninitializedArray<T>(count);
        var bytesPerElement = Unsafe.SizeOf<T>();
        var buffer = ArrayPool<byte>.Shared.Rent(count * bytesPerElement);

        try
        {
            var bytesToRead = count * bytesPerElement;
            await stream.ReadExactlyAsync(buffer.AsMemory(0, bytesToRead), cancellation);

            if (isLittleEndian == BitConverter.IsLittleEndian)
            {
                // No conversion needed
                Buffer.BlockCopy(buffer, 0, elements, 0, bytesToRead);
            }
            else
            {
                EndianConverter.ReverseEndianness(buffer.AsSpan(0, bytesToRead), elements);
            }

            return elements;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
