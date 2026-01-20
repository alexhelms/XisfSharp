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
        await reader.ReadHeaderAsync();
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

    [TestMethod]
    [DataRow("RGGB", 2, 2, null, new[] { CFAElement.R, CFAElement.G, CFAElement.G, CFAElement.B })]
    [DataRow("BGGR", 2, 2, null, new[] { CFAElement.B, CFAElement.G, CFAElement.G, CFAElement.R })]
    [DataRow("GRBG", 2, 2, null, new[] { CFAElement.G, CFAElement.R, CFAElement.B, CFAElement.G })]
    [DataRow("GBRG", 2, 2, null, new[] { CFAElement.G, CFAElement.B, CFAElement.R, CFAElement.G })]
    [DataRow("rggb", 2, 2, null, new[] { CFAElement.R, CFAElement.G, CFAElement.G, CFAElement.B })]
    [DataRow("RgGb", 2, 2, null, new[] { CFAElement.R, CFAElement.G, CFAElement.G, CFAElement.B })]
    [DataRow("RGGB", 2, 2, "Bayer RGGB", new[] { CFAElement.R, CFAElement.G, CFAElement.G, CFAElement.B })]
    [DataRow("RGBGRGBG", 4, 2, null, new[] { CFAElement.R, CFAElement.G, CFAElement.B, CFAElement.G, CFAElement.R, CFAElement.G, CFAElement.B, CFAElement.G })]
    [DataRow("WWWW", 2, 2, "Mono", new[] { CFAElement.W, CFAElement.W, CFAElement.W, CFAElement.W })]
    [DataRow("CMYG", 2, 2, null, new[] { CFAElement.C, CFAElement.M, CFAElement.Y, CFAElement.G })]
    [DataRow("RGBW", 2, 2, null, new[] { CFAElement.R, CFAElement.G, CFAElement.B, CFAElement.W })]
    [DataRow("RGB", 3, 1, null, new[] { CFAElement.R, CFAElement.G, CFAElement.B })]
    [DataRow("RGBRGB", 3, 2, "Trilinear", new[] { CFAElement.R, CFAElement.G, CFAElement.B, CFAElement.R, CFAElement.G, CFAElement.B })]
    public void Constructor_WithStringPattern_CreatesValidColorFilterArray(string pattern, int width, int height, string? name, CFAElement[] expectedPattern)
    {
        var cfa = new ColorFilterArray(pattern, width, height, name);

        cfa.Pattern.ShouldBe(expectedPattern);
        cfa.Width.ShouldBe(width);
        cfa.Height.ShouldBe(height);
        cfa.Name.ShouldBe(name);
    }

    [TestMethod]
    [DataRow("RGGB", 3, 2, "Pattern length does not match width * height.")]
    [DataRow("RGB", 2, 2, "Pattern length does not match width * height.")]
    [DataRow("", 2, 2, "Pattern cannot be empty.")]
    [DataRow("RGGB", -1, 2, "width")]
    [DataRow("RGGB", 2, -1, "height")]
    [DataRow("RXGB", 2, 2, "Invalid character 'X' in pattern string.")]
    [DataRow("RG GB", 2, 2, "Invalid character ' ' in pattern string.")]
    [DataRow("RG1B", 2, 2, "Invalid character '1' in pattern string.")]
    public void Constructor_WithStringPattern_ThrowsOnInvalidInput(string pattern, int width, int height, string expectedErrorFragment)
    {
        var exception = Should.Throw<ArgumentException>(() => new ColorFilterArray(pattern, width, height));
        exception.Message.ShouldContain(expectedErrorFragment);
    }

    [TestMethod]
    public void Constructor_WithStringPattern_NullThrows()
    {
        Should.Throw<ArgumentNullException>(() => new ColorFilterArray((string)null!, 2, 2));
    }
}
