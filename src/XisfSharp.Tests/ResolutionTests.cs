using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class ResolutionTests
{
    [TestMethod]
    public async Task ReadWrite_RoundTrip()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Resolution = new Resolution(100, 200, ResolutionUnit.Inch);

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Resolution horizontal="100" vertical="200" unit="inch" />""");

        image.Resolution.ShouldNotBeNull();
        image.Resolution.Horizontal.ShouldBe(100);
        image.Resolution.Vertical.ShouldBe(200);
        image.Resolution.Unit.ShouldBe(ResolutionUnit.Inch);
    }

    [TestMethod]
    public async Task Read_InvalidResolution_Ignored()
    {
        List<string> properties = [
            """<Resolution />"""
        ];

        using var stream = TestHelpers.CreateXisfStreamWith40x30Image(properties);
        using var reader = new XisfReader(stream);
        await reader.ReadAsync();
        var image = await reader.ReadImageAsync(0);

        image.Resolution.ShouldBeNull();
    }
}
