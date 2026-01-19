using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class RGBWorkingSpaceTests
{
    [TestMethod]
    public async Task ReadWrite_RoundTrip()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.RGBWorkingSpace = new RGBWorkingSpace(
            "2.2",
            [0.11, 0.12, 0.13],
            [0.21, 0.22, 0.23],
            [0.31, 0.32, 0.33],
            "My RGBWS"
        );

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<RGBWorkingSpace x="0.110000:0.120000:0.130000" y="0.210000:0.220000:0.230000" Y="0.310000:0.320000:0.330000" gamma="2.2" name="My RGBWS" />""");

        image.RGBWorkingSpace.ShouldNotBeNull();
        image.RGBWorkingSpace.Name.ShouldBe("My RGBWS");
        image.RGBWorkingSpace.Gamma.ShouldBe("2.2");
        image.RGBWorkingSpace.ChromaticityX.ShouldBe([0.11, 0.12, 0.13]);
        image.RGBWorkingSpace.ChromaticityY.ShouldBe([0.21, 0.22, 0.23]);
        image.RGBWorkingSpace.Luminance.ShouldBe([0.31, 0.32, 0.33]);
    }

    [TestMethod]
    public async Task Read_InvalidRGBWorkingSpace_Ignored()
    {
        List<string> properties = [
            """<RGBWorkingSpace />"""
        ];

        using var stream = TestHelpers.CreateXisfStreamWith40x30Image(properties);
        using var reader = new XisfReader(stream);
        await reader.ReadAsync();
        var image = await reader.ReadImageAsync(0);

        image.RGBWorkingSpace.ShouldBeNull();
    }

    [TestMethod]
    public void Constructor_ValidParameters_ShouldNotThrow()
    {
        Should.NotThrow(() => new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        Should.NotThrow(() => new RGBWorkingSpace("sRGB", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        Should.NotThrow(() => new RGBWorkingSpace("1.8", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9], "My WS"));
        Should.NotThrow(() => new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9], null));
    }

    [TestMethod]
    public void Constructor_NullGamma_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new RGBWorkingSpace(null!, [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        ex.ParamName.ShouldBe("gamma");
    }

    [TestMethod]
    public void Constructor_EmptyGamma_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new RGBWorkingSpace("", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        ex.ParamName.ShouldBe("gamma");
    }

    [TestMethod]
    public void Constructor_WhitespaceGamma_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new RGBWorkingSpace("   ", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        ex.ParamName.ShouldBe("gamma");
    }

    [TestMethod]
    public void Constructor_NullChromaticityX_ShouldThrowArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new RGBWorkingSpace("2.2", null!, [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        ex.ParamName.ShouldBe("chromaticityX");
    }

    [TestMethod]
    public void Constructor_NullChromaticityY_ShouldThrowArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], null!, [0.7, 0.8, 0.9]));
        ex.ParamName.ShouldBe("chromaticityY");
    }

    [TestMethod]
    public void Constructor_NullLuminance_ShouldThrowArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], null!));
        ex.ParamName.ShouldBe("luminance");
    }

    [TestMethod]
    public void Constructor_ChromaticityXWrongLength_ShouldThrowArgumentOutOfRangeException()
    {
        // Too few elements
        var ex1 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        ex1.ParamName.ShouldBe("chromaticityX");

        // Too many elements
        var ex2 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3, 0.4], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        ex2.ParamName.ShouldBe("chromaticityX");

        // Empty array
        var ex3 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]));
        ex3.ParamName.ShouldBe("chromaticityX");
    }

    [TestMethod]
    public void Constructor_ChromaticityYWrongLength_ShouldThrowArgumentOutOfRangeException()
    {
        // Too few elements
        var ex1 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4], [0.7, 0.8, 0.9]));
        ex1.ParamName.ShouldBe("chromaticityY");

        // Too many elements
        var ex2 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6, 0.7, 0.8], [0.7, 0.8, 0.9]));
        ex2.ParamName.ShouldBe("chromaticityY");

        // Empty array
        var ex3 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [], [0.7, 0.8, 0.9]));
        ex3.ParamName.ShouldBe("chromaticityY");
    }

    [TestMethod]
    public void Constructor_LuminanceWrongLength_ShouldThrowArgumentOutOfRangeException()
    {
        // Too few elements
        var ex1 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8]));
        ex1.ParamName.ShouldBe("luminance");

        // Too many elements
        var ex2 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9, 1.0]));
        ex2.ParamName.ShouldBe("luminance");

        // Empty array
        var ex3 = Should.Throw<ArgumentOutOfRangeException>(() =>
            new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], []));
        ex3.ParamName.ShouldBe("luminance");
    }

    [TestMethod]
    public void Constructor_NullName_ShouldBeAllowed()
    {
        var rgbws = new RGBWorkingSpace("2.2", [0.1, 0.2, 0.3], [0.4, 0.5, 0.6], [0.7, 0.8, 0.9]);
        rgbws.Name.ShouldBeNull();
    }
}
