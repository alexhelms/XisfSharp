using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace XisfSharp.IO;

/// <summary>
/// Converts between items and the byte planes of the XISF byte shuffling transform.
/// </summary>
/// <remarks>
/// Byte shuffling stores byte <c>j</c> of every item before byte <c>j + 1</c> of any item. The vectorized paths
/// treat items of 2 and 4 bytes as little-endian integers: widening a plane places its bytes in the low bits of
/// each integer, and shifting and narrowing extracts them again. Other item sizes, and big-endian hosts, use the
/// scalar paths.
/// </remarks>
internal static class ByteShuffle
{
    /// <summary>
    /// Rebuilds items from their byte planes: <c>output[i * itemSize + j] = planes[j][i]</c>.
    /// </summary>
    /// <param name="planes">One buffer per byte of an item, each holding at least <paramref name="count"/> bytes.</param>
    /// <param name="count">The number of items.</param>
    /// <param name="output">Receives <c>count * planes.Length</c> bytes.</param>
    public static void Interleave(byte[][] planes, int count, Span<byte> output)
    {
        int itemSize = planes.Length;
        output = output[..(count * itemSize)];

        if (BitConverter.IsLittleEndian && itemSize == 2)
        {
            Interleave2(planes[0].AsSpan(0, count), planes[1].AsSpan(0, count), MemoryMarshal.Cast<byte, ushort>(output));
        }
        else if (BitConverter.IsLittleEndian && itemSize == 4)
        {
            Interleave4(planes, count, MemoryMarshal.Cast<byte, uint>(output));
        }
        else
        {
            for (int j = 0; j < itemSize; j++)
            {
                var plane = planes[j].AsSpan(0, count);
                for (int i = 0, o = j; i < plane.Length; i++, o += itemSize)
                {
                    output[o] = plane[i];
                }
            }
        }
    }

    /// <summary>
    /// Copies byte <paramref name="byteIndex"/> of every item into <paramref name="plane"/>.
    /// </summary>
    /// <param name="items">Whole items of <paramref name="itemSize"/> bytes.</param>
    /// <param name="itemSize">The size of an item in bytes.</param>
    /// <param name="byteIndex">Which byte of each item to extract.</param>
    /// <param name="plane">Receives one byte per item.</param>
    public static void ExtractPlane(ReadOnlySpan<byte> items, int itemSize, int byteIndex, Span<byte> plane)
    {
        int count = items.Length / itemSize;
        plane = plane[..count];

        if (BitConverter.IsLittleEndian && itemSize == 2)
        {
            ExtractPlane2(MemoryMarshal.Cast<byte, ushort>(items[..(count * 2)]), byteIndex * 8, plane);
        }
        else if (BitConverter.IsLittleEndian && itemSize == 4)
        {
            ExtractPlane4(MemoryMarshal.Cast<byte, uint>(items[..(count * 4)]), byteIndex * 8, plane);
        }
        else
        {
            for (int i = 0, s = byteIndex; i < plane.Length; i++, s += itemSize)
            {
                plane[i] = items[s];
            }
        }
    }

    private static void Interleave2(ReadOnlySpan<byte> plane0, ReadOnlySpan<byte> plane1, Span<ushort> output)
    {
        int i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= plane0.Length - Vector128<byte>.Count; i += Vector128<byte>.Count)
            {
                var (a0, a1) = Vector128.Widen(Vector128.Create(plane0.Slice(i, Vector128<byte>.Count)));
                var (b0, b1) = Vector128.Widen(Vector128.Create(plane1.Slice(i, Vector128<byte>.Count)));

                (a0 | (b0 << 8)).CopyTo(output[i..]);
                (a1 | (b1 << 8)).CopyTo(output[(i + Vector128<ushort>.Count)..]);
            }
        }

        for (; i < plane0.Length; i++)
        {
            output[i] = (ushort)(plane0[i] | (plane1[i] << 8));
        }
    }

    private static void Interleave4(byte[][] planes, int count, Span<uint> output)
    {
        int i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            Span<Vector128<uint>> combined = stackalloc Vector128<uint>[4];

            for (; i <= count - Vector128<byte>.Count; i += Vector128<byte>.Count)
            {
                combined.Clear();

                for (int j = 0; j < 4; j++)
                {
                    var (lo, hi) = Vector128.Widen(Vector128.Create(planes[j].AsSpan(i, Vector128<byte>.Count)));
                    var (w0, w1) = Vector128.Widen(lo);
                    var (w2, w3) = Vector128.Widen(hi);
                    int shift = j * 8;

                    combined[0] |= w0 << shift;
                    combined[1] |= w1 << shift;
                    combined[2] |= w2 << shift;
                    combined[3] |= w3 << shift;
                }

                for (int k = 0; k < 4; k++)
                {
                    combined[k].CopyTo(output[(i + k * Vector128<uint>.Count)..]);
                }
            }
        }

        var p0 = planes[0];
        var p1 = planes[1];
        var p2 = planes[2];
        var p3 = planes[3];

        for (; i < count; i++)
        {
            output[i] = p0[i] | ((uint)p1[i] << 8) | ((uint)p2[i] << 16) | ((uint)p3[i] << 24);
        }
    }

    private static void ExtractPlane2(ReadOnlySpan<ushort> items, int shift, Span<byte> plane)
    {
        int i = 0;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= items.Length - Vector128<byte>.Count; i += Vector128<byte>.Count)
            {
                var lo = Vector128.ShiftRightLogical(Vector128.Create(items.Slice(i, Vector128<ushort>.Count)), shift);
                var hi = Vector128.ShiftRightLogical(Vector128.Create(items.Slice(i + Vector128<ushort>.Count, Vector128<ushort>.Count)), shift);
                Vector128.Narrow(lo, hi).CopyTo(plane[i..]);
            }
        }

        for (; i < items.Length; i++)
        {
            plane[i] = (byte)(items[i] >> shift);
        }
    }

    private static void ExtractPlane4(ReadOnlySpan<uint> items, int shift, Span<byte> plane)
    {
        int i = 0;
        int n = Vector128<uint>.Count;

        if (Vector128.IsHardwareAccelerated)
        {
            for (; i <= items.Length - Vector128<byte>.Count; i += Vector128<byte>.Count)
            {
                var v0 = Vector128.ShiftRightLogical(Vector128.Create(items.Slice(i, n)), shift);
                var v1 = Vector128.ShiftRightLogical(Vector128.Create(items.Slice(i + n, n)), shift);
                var v2 = Vector128.ShiftRightLogical(Vector128.Create(items.Slice(i + 2 * n, n)), shift);
                var v3 = Vector128.ShiftRightLogical(Vector128.Create(items.Slice(i + 3 * n, n)), shift);

                // Narrowing truncates, keeping the low byte that the shift moved into place.
                Vector128.Narrow(Vector128.Narrow(v0, v1), Vector128.Narrow(v2, v3)).CopyTo(plane[i..]);
            }
        }

        for (; i < items.Length; i++)
        {
            plane[i] = (byte)(items[i] >> shift);
        }
    }
}
