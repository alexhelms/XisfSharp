using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class WriteIntegrationTests
{
    [TestMethod]
    public async Task Write_MultiImage_RGB_Compression_ByteShuffling_Signature()
    {
        int width = 320;
        int height = 240;
        int channels = 3;
        byte[] channelData = Enumerable.Range(0, width * height)
            .SelectMany(x => BitConverter.GetBytes((float)x / (width * height)))
            .ToArray();

        byte[] data = new byte[channelData.Length * channels];
        Array.Copy(channelData, 0, data, 0 * channelData.Length, channelData.Length);
        Array.Copy(channelData, 0, data, 1 * channelData.Length, channelData.Length);
        Array.Copy(channelData, 0, data, 2 * channelData.Length, channelData.Length);

        byte[] fileBytes;
        {
            var image1 = new XisfImage(data, width, height, channels, SampleFormat.Float32);
            image1.ColorSpace = ColorSpace.RGB;
            image1.Bounds = new(0, 1);

            var image2 = new XisfImage(data, width, height, channels, SampleFormat.Float32);
            image2.ColorSpace = ColorSpace.RGB;
            image2.Bounds = new(0, 1);

            var writerOptions = new XisfWriterOptions
            {
                ChecksumAlgorithm = ChecksumAlgorithm.SHA256,
                CompressionCodec = CompressionCodec.Lz4Hc,
                ShuffleBytes = true,
            };

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream, writerOptions);
            writer.AddImage(image1);
            writer.AddImage(image2);
            await writer.SaveAsync();
            await stream.FlushAsync();
            fileBytes = stream.ToArray();
        }

        // Verify compression
        fileBytes.Length.ShouldBe(43585);

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            
            await reader.ReadAsync();
            reader.Images.Count.ShouldBe(2);

            var image1 = await reader.ReadImageAsync(0);
            image1.Width.ShouldBe(width);
            image1.Height.ShouldBe(height);
            image1.Channels.ShouldBe(channels);

            var image2 = await reader.ReadImageAsync(1);
            image2.Width.ShouldBe(width);
            image2.Height.ShouldBe(height);
            image2.Channels.ShouldBe(channels);
        }
    }
}
