using System.Runtime.InteropServices;

namespace XisfSharp.Tests;

[TestClass]
public class ReadIntegrationTests
{
    [TestMethod]
    [DataRow("400x300-u8.xisf")]
    [DataRow("400x300-u16.xisf")]
    [DataRow("400x300-u32.xisf")]
    [DataRow("400x300-f32.xisf")]
    [DataRow("400x300-f64.xisf")]
    public async Task ReadImageAsync(string filename)
    {
        var image = await XisfFile.ReadImageAsync(TestHelpers.TestImagePath(filename));
        image.Width.ShouldBe(400);
        image.Height.ShouldBe(300);
        image.Channels.ShouldBe(1);

        // These were all created in PixInsight with pixelmath value of 0.5
        // and the output image of the appropriate type, saved to an xisf
        // file of the appropriate type.

        int i = 0;
        if (filename.Contains("u8"))
        {
            int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(byte);
            image.Data.Length.ShouldBe(expectedDataLength);

            foreach (var b in image.Data.Span)
            {
                b.ShouldBe((byte)(byte.MaxValue / 2 + 1), $"Mismatch at index {i}");
                i++;
            }
        }
        else if (filename.Contains("u16"))
        {
            int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(ushort);
            image.Data.Length.ShouldBe(expectedDataLength);

            var u16Span = MemoryMarshal.Cast<byte, ushort>(image.Data.Span);
            foreach (ushort value in u16Span)
            {
                value.ShouldBe((ushort)(ushort.MaxValue / 2 + 1), $"Mismatch at index {i}");
                i++;
            }
        }
        else if (filename.Contains("u32"))
        {
            int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(int);
            image.Data.Length.ShouldBe(expectedDataLength);

            var u32Span = MemoryMarshal.Cast<byte, uint>(image.Data.Span);
            foreach (uint value in u32Span)
            {
                value.ShouldBe(uint.MaxValue / 2 + 1, $"Mismatch at index {i}");
                i++;
            }
        }
        else if (filename.Contains("f32"))
        {
            int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(float);
            image.Data.Length.ShouldBe(expectedDataLength);

            var f32Span = MemoryMarshal.Cast<byte, float>(image.Data.Span);
            foreach (float value in f32Span)
            {
                value.ShouldBe(0.5f, $"Mismatch at index {i}");
                i++;
            }
        }
        else if (filename.Contains("f64"))
        {
            int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(double);
            image.Data.Length.ShouldBe(expectedDataLength);

            var f64Span = MemoryMarshal.Cast<byte, double>(image.Data.Span);
            foreach (double value in f64Span)
            {
                value.ShouldBe(0.5, $"Mismatch at index {i}");
                i++;
            }
        }
    }

    [TestMethod]
    [DataRow("400x300-u16-lz4.xisf")]
    [DataRow("400x300-u16-lz4hc.xisf")]
    [DataRow("400x300-u16-zlib.xisf")]
    [DataRow("400x300-u16-zstd.xisf")]
    public async Task ReadImageAsync_Compression(string filename)
    {
        var image = await XisfFile.ReadImageAsync(TestHelpers.TestImagePath(filename));
        image.Width.ShouldBe(400);
        image.Height.ShouldBe(300);
        image.Channels.ShouldBe(1);

        int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(ushort);
        image.Data.Length.ShouldBe(expectedDataLength);

        int i = 0;
        var u16Span = MemoryMarshal.Cast<byte, ushort>(image.Data.Span);
        foreach (ushort value in u16Span)
        {
            value.ShouldBe((ushort)42, $"Mismatch at index {i}");
            i++;
        }
    }

    [TestMethod]
    [DataRow("400x300-u16-lz4hc-shuffle.xisf")]
    [DataRow("400x300-u16-lz4-shuffle.xisf")]
    [DataRow("400x300-u16-zlib-shuffle.xisf")]
    [DataRow("400x300-u16-zstd-shuffle.xisf")]
    public async Task ReadImageAsync_ByteShuffle(string filename)
    {
        var image = await XisfFile.ReadImageAsync(TestHelpers.TestImagePath(filename));
        image.Width.ShouldBe(400);
        image.Height.ShouldBe(300);
        image.Channels.ShouldBe(1);

        int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(ushort);
        image.Data.Length.ShouldBe(expectedDataLength);

        int i = 0;
        var u16Span = MemoryMarshal.Cast<byte, ushort>(image.Data.Span);
        foreach (ushort value in u16Span)
        {
            value.ShouldBe((ushort)42, $"Mismatch at index {i}");
            i++;
        }
    }

    [TestMethod]
    [DataRow("400x300-u16-sha1.xisf")]
    [DataRow("400x300-u16-sha256.xisf")]
    [DataRow("400x300-u16-sha512.xisf")]
    public async Task ReadImageAsync_WithSignature(string filename)
    {
        var image = await XisfFile.ReadImageAsync(TestHelpers.TestImagePath(filename));
        image.Width.ShouldBe(400);
        image.Height.ShouldBe(300);
        image.Channels.ShouldBe(1);

        int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(ushort);
        image.Data.Length.ShouldBe(expectedDataLength);

        int i = 0;
        var u16Span = MemoryMarshal.Cast<byte, ushort>(image.Data.Span);
        foreach (ushort value in u16Span)
        {
            value.ShouldBe((ushort)42, $"Mismatch at index {i}");
            i++;
        }
    }

    [TestMethod]
    public async Task ReadImageAsync_EmbeddedImageData()
    {
        var image = await XisfFile.ReadImageAsync(TestHelpers.TestImagePath("40x30-u16-embedded.xisf"));
        image.Width.ShouldBe(40);
        image.Height.ShouldBe(30);
        image.Channels.ShouldBe(1);

        int expectedDataLength = image.Width * image.Height * image.Channels * sizeof(ushort);
        image.Data.Length.ShouldBe(expectedDataLength);

        int i = 0;
        var u16Span = MemoryMarshal.Cast<byte, ushort>(image.Data.Span);
        foreach (ushort value in u16Span)
        {
            value.ShouldBe((ushort)(ushort.MaxValue / 2 + 1), $"Mismatch at index {i}");
            i++;
        }
    }
}
