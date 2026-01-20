using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfWriterIntegrationTests
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

            var image2 = new XisfImage(data, width, height, channels, SampleFormat.Float32);
            image2.ColorSpace = ColorSpace.RGB;

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
            
            await reader.ReadHeaderAsync();
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

    [TestMethod]
    public async Task Write_TopLevelProperties_AreWrittenToXisfElement()
    {
        int width = 10;
        int height = 10;
        byte[] data = new byte[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1, SampleFormat.UInt8);
            image.Properties.Add(new XisfStringProperty("Image:Filter", "Luminance"));

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream);

            writer.AddProperty(new XisfStringProperty("Session:Target", "M31"));
            writer.AddProperty(new XisfScalarProperty("Session:ExposureCount", XisfPropertyType.Int32, 25));
            writer.AddImage(image);

            await writer.SaveAsync();
            await stream.FlushAsync();
            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);

            await reader.ReadHeaderAsync();

            // Verify top-level properties exist
            reader.Properties.ContainsId("Session:Target").ShouldBeTrue();
            reader.Properties.ContainsId("Session:ExposureCount").ShouldBeTrue();

            ((XisfStringProperty)reader.Properties["Session:Target"]!).Value.ShouldBe("M31");
            ((XisfScalarProperty)reader.Properties["Session:ExposureCount"]!).Value.ShouldBe(25);

            // Verify image property is on the image, not top-level
            var image = await reader.ReadImageAsync(0);
            image.Properties.ContainsId("Image:Filter").ShouldBeTrue();
            reader.Properties.ContainsId("Image:Filter").ShouldBeFalse();
        }
    }

    [TestMethod]
    public async Task Write_FluentApi_WithImagePropertiesAndGlobalProperties()
    {
        int width = 100;
        int height = 100;
        int channels = 1;
        byte[] data1 = Enumerable.Range(0, width * height * 2).Select(x => (byte)(x % 256)).ToArray();
        byte[] data2 = Enumerable.Range(0, width * height * 2).Select(x => (byte)((x + 128) % 256)).ToArray();

        byte[] fileBytes;
        {
            var image1 = new XisfImage(data1, width, height, channels, SampleFormat.UInt16);
            image1.ColorSpace = ColorSpace.Gray;
            image1.Id = "Image1";

            var image2 = new XisfImage(data2, width, height, channels, SampleFormat.UInt16);
            image2.ColorSpace = ColorSpace.Gray;
            image2.Id = "Image2";

            var writerOptions = new XisfWriterOptions
            {
                CompressionCodec = CompressionCodec.Zlib,
                ChecksumAlgorithm = ChecksumAlgorithm.SHA256,
            };

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream, writerOptions);

            await writer
                .WithProperties([
                    new XisfStringProperty("Session:Target", "NGC7000"),
                    new XisfStringProperty("Session:Telescope", "Refractor")
                ])
                .WithProperties([
                    new XisfScalarProperty("Session:Temperature", XisfPropertyType.Float32, 15.5f),
                    new XisfScalarProperty("Session:TotalFrames", XisfPropertyType.Int32, 2)
                ])
                .WithImage(image1, [
                    new XisfStringProperty("Frame:Filter", "Luminance"),
                    new XisfScalarProperty("Frame:Exposure", XisfPropertyType.Float32, 300.0f)
                ])
                .WithImage(image2, [
                    new XisfStringProperty("Frame:Filter", "Ha"),
                    new XisfScalarProperty("Frame:Exposure", XisfPropertyType.Float32, 600.0f)
                ])
                .SaveAsync();

            await stream.FlushAsync();
            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);

            await reader.ReadHeaderAsync();

            reader.Images.Count.ShouldBe(2);

            reader.Properties.ContainsId("Session:Target").ShouldBeTrue();
            reader.Properties.ContainsId("Session:Telescope").ShouldBeTrue();
            reader.Properties.ContainsId("Session:Temperature").ShouldBeTrue();
            reader.Properties.ContainsId("Session:TotalFrames").ShouldBeTrue();

            ((XisfStringProperty)reader.Properties["Session:Target"]!).StringValue.ShouldBe("NGC7000");
            ((XisfStringProperty)reader.Properties["Session:Telescope"]!).StringValue.ShouldBe("Refractor");
            ((XisfScalarProperty)reader.Properties["Session:Temperature"]!).Value.ShouldBe(15.5f);
            ((XisfScalarProperty)reader.Properties["Session:TotalFrames"]!).Value.ShouldBe(2);

            var image1 = await reader.ReadImageAsync(0);
            image1.Id.ShouldBe("Image1");
            image1.Properties.ContainsId("Frame:Filter").ShouldBeTrue();
            image1.Properties.ContainsId("Frame:Exposure").ShouldBeTrue();
            ((XisfStringProperty)image1.Properties["Frame:Filter"]!).StringValue.ShouldBe("Luminance");
            ((XisfScalarProperty)image1.Properties["Frame:Exposure"]!).Value.ShouldBe(300.0f);
            image1.Data.ToArray().ShouldBe(data1);

            var image2 = await reader.ReadImageAsync(1);
            image2.Id.ShouldBe("Image2");
            image2.Properties.ContainsId("Frame:Filter").ShouldBeTrue();
            image2.Properties.ContainsId("Frame:Exposure").ShouldBeTrue();
            ((XisfStringProperty)image2.Properties["Frame:Filter"]!).StringValue.ShouldBe("Ha");
            ((XisfScalarProperty)image2.Properties["Frame:Exposure"]!).Value.ShouldBe(600.0f);
            image2.Data.ToArray().ShouldBe(data2);
        }
    }
}
