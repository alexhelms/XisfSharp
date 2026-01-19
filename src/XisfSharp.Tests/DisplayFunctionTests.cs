using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class DisplayFunctionTests
{
    [TestMethod]
    public async Task ParseDisplayFunction()
    {
        var image = await XisfFile.ReadImageAsync(TestHelpers.TestImagePath("200x150-u16-zstd-shuffle-process-history-display-function-icc-sha1.xisf"));

        image.DisplayFunction.ShouldNotBeNull();
        image.DisplayFunction.Midtones.ShouldBe([0.25d, 0.25d, 0.25d, 0.5d], 1e-5);
        image.DisplayFunction.Shadows.ShouldBe([0d, 0d, 0d, 0d], 1e-5);
        image.DisplayFunction.Highlights.ShouldBe([1d, 1d, 1d, 1d], 1e-5);
        image.DisplayFunction.ShadowDynamicRange.ShouldBe([0d, 0d, 0d, 0d], 1e-5);
        image.DisplayFunction.HighlightDynamicRange.ShouldBe([1d, 1d, 1d, 1d], 1e-5);
    }

    [TestMethod]
    public async Task ReadWrite_DisplayFunction()
    {
        double[] midtones = [0.10, 0.11, 0.12, 0.13];
        double[] shadows = [0.20, 0.21, 0.22, 0.23];
        double[] highlights = [0.30, 0.31, 0.32, 0.33];
        double[] shadowDynamicRange = [0.40, 0.41, 0.42, 0.43];
        double[] highlightDynamicRange = [0.50, 0.51, 0.52, 0.53];

        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.DisplayFunction = new DisplayFunction(midtones, shadows, highlights, shadowDynamicRange, highlightDynamicRange);

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain(
            """
            <DisplayFunction m="0.100000:0.110000:0.120000:0.130000" s="0.200000:0.210000:0.220000:0.230000" h="0.300000:0.310000:0.320000:0.330000" l="0.400000:0.410000:0.420000:0.430000" r="0.500000:0.510000:0.520000:0.530000" />
            """);

        image.DisplayFunction.ShouldNotBeNull();
        image.DisplayFunction.Midtones.ShouldBe(midtones, 1e-5);
        image.DisplayFunction.Shadows.ShouldBe(shadows, 1e-5);
        image.DisplayFunction.Highlights.ShouldBe(highlights, 1e-5);
        image.DisplayFunction.ShadowDynamicRange.ShouldBe(shadowDynamicRange, 1e-5);
        image.DisplayFunction.HighlightDynamicRange.ShouldBe(highlightDynamicRange, 1e-5);
    }

    [TestMethod]
    public async Task Read_InvalidDisplayFunction_Ignored()
    {
        List<string> properties = [
            """<DisplayFunction />"""
        ];

        using var stream = TestHelpers.CreateXisfStreamWith40x30Image(properties);
        using var reader = new XisfReader(stream);
        await reader.ReadAsync();
        var image = await reader.ReadImageAsync(0);

        image.DisplayFunction.ShouldBeNull();
    }
}
