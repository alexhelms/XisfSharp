using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class ColorFilterArrayTests
{
    [TestMethod]
    public async Task ReadWrite_RoundTrip()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.ColorFilterArray = new ColorFilterArray([CFAElement.R, CFAElement.G, CFAElement.G, CFAElement.B], 2, 2, "My RGGB CFA");

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<ColorFilterArray pattern="RGGB" width="2" height="2" name="My RGGB CFA" />""");

        image.ColorFilterArray.ShouldNotBeNull();
        image.ColorFilterArray.Pattern.ShouldBe([CFAElement.R, CFAElement.G, CFAElement.G, CFAElement.B]);
        image.ColorFilterArray.Width.ShouldBe(2);
        image.ColorFilterArray.Height.ShouldBe(2);
        image.ColorFilterArray.Name.ShouldBe("My RGGB CFA");
    }

    [TestMethod]
    public async Task Read_InvalidColorFilterArray_Ignored()
    {
        List<string> properties = [
            """<ColorFilterArray />"""
        ];

        using var stream = TestHelpers.CreateXisfStreamWith40x30Image(properties);
        using var reader = new XisfReader(stream);
        await reader.ReadAsync();
        var image = await reader.ReadImageAsync(0);

        image.ColorFilterArray.ShouldBeNull();
    }

    [TestMethod]
    public void Width_LessThan0_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ColorFilterArray([CFAElement.R], -1, 1));
    }

    [TestMethod]
    public void Height_LessThan0_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ColorFilterArray([CFAElement.R], 1, -1));
    }

    [TestMethod]
    public void EmptyPattern_Throws()
    {
        Should.Throw<ArgumentException>(() => new ColorFilterArray([], 1, 1));
    }

    [TestMethod]
    public void PatternNotEqualToWidthTimesHeight_Throws()
    {
        Should.Throw<ArgumentException>(() => new ColorFilterArray([CFAElement.R, CFAElement.G, CFAElement.G, CFAElement.B], 1, 1));
    }
}
