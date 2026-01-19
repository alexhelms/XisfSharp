using XisfSharp.FITS;
using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class FITSKeywordTests
{
    [TestMethod]
    public async Task ReadWrite_RoundTrip()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.FITSKeywords.Add(new FITSKeyword("TESTKEY", "TestValue", "This is a test keyword"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<FITSKeyword name="TESTKEY" value="TestValue" comment="This is a test keyword" />""");

        image.FITSKeywords.Count.ShouldBe(1);
        image.FITSKeywords[0].Name.ShouldBe("TESTKEY");
        image.FITSKeywords[0].Value.ShouldBe("TestValue");
        image.FITSKeywords[0].Comment.ShouldBe("This is a test keyword");
    }

    [TestMethod]
    public async Task Read_InvalidFITSKeyword_Ignored()
    {
        List<string> properties = [
            """<FITSKeyword name="TESTKEY" value="A Value" comment="A Comment" />""",
            """<FITSKeyword name="NAMEISTOOLONG" value="A Value" comment="A Comment" />"""
        ];
        
        using var stream = TestHelpers.CreateXisfStreamWith40x30Image(properties);
        using var reader = new XisfReader(stream);
        await reader.ReadAsync();
        var image = await reader.ReadImageAsync(0);

        image.FITSKeywords.Count.ShouldBe(1);
        image.FITSKeywords[0].Name.ShouldBe("TESTKEY");
    }

    [TestMethod]
    public void ValidateName_ValidNames_ShouldNotThrow()
    {
        Should.NotThrow(() => new FITSKeyword("SIMPLE", "value"));
        Should.NotThrow(() => new FITSKeyword("BITPIX", "value"));
        Should.NotThrow(() => new FITSKeyword("NAXIS", "value"));
        Should.NotThrow(() => new FITSKeyword("NAXIS1", "value"));
        Should.NotThrow(() => new FITSKeyword("TEST_KEY", "value"));
        Should.NotThrow(() => new FITSKeyword("TEST-KEY", "value"));
        Should.NotThrow(() => new FITSKeyword("A", "value"));
        Should.NotThrow(() => new FITSKeyword("12345678", "value"));
        Should.NotThrow(() => new FITSKeyword("ABCDEFGH", "value"));
        Should.NotThrow(() => new FITSKeyword("_TEST_", "value"));
        Should.NotThrow(() => new FITSKeyword("-TEST-", "value"));
        Should.NotThrow(() => new FITSKeyword("TEST_-1", "value"));
    }

    [TestMethod]
    public void ValidateName_NullName_ShouldThrowArgumentNullException()
    {
        var ex = Should.Throw<ArgumentException>(() => new FITSKeyword(null!, "value"));
        ex.Message.ShouldContain("cannot be empty or whitespace");
    }

    [TestMethod]
    public void ValidateName_EmptyName_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => new FITSKeyword("", "value"));
        ex.Message.ShouldContain("cannot be empty or whitespace");
    }

    [TestMethod]
    public void ValidateName_WhitespaceName_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => new FITSKeyword("   ", "value"));
        ex.Message.ShouldContain("cannot be empty or whitespace");
    }

    [TestMethod]
    public void ValidateName_NameTooLong_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => new FITSKeyword("TOOLONG99", "value"));
        ex.Message.ShouldContain("cannot exceed 8 characters");
    }

    [TestMethod]
    public void ValidateName_LowercaseLetters_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => new FITSKeyword("testkey", "value"));
        ex.Message.ShouldContain("invalid character");
    }

    [TestMethod]
    public void ValidateName_MixedCaseLetters_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => new FITSKeyword("TestKey", "value"));
        ex.Message.ShouldContain("invalid character");
    }

    [TestMethod]
    public void ValidateName_EmbeddedSpace_ShouldThrowArgumentException()
    {
        var ex = Should.Throw<ArgumentException>(() => new FITSKeyword("TEST KEY", "value"));
        ex.Message.ShouldContain("invalid character");
    }

    [TestMethod]
    public void ValidateName_SpecialCharacters_ShouldThrowArgumentException()
    {
        Should.Throw<ArgumentException>(() => new FITSKeyword("TEST@KEY", "value"));
        Should.Throw<ArgumentException>(() => new FITSKeyword("TEST#KEY", "value"));
        Should.Throw<ArgumentException>(() => new FITSKeyword("TEST$KEY", "value"));
        Should.Throw<ArgumentException>(() => new FITSKeyword("TEST%KEY", "value"));
        Should.Throw<ArgumentException>(() => new FITSKeyword("TEST&KEY", "value"));
        Should.Throw<ArgumentException>(() => new FITSKeyword("TEST*KEY", "value"));
        Should.Throw<ArgumentException>(() => new FITSKeyword("TEST.KEY", "value"));
        Should.Throw<ArgumentException>(() => new FITSKeyword("TEST/KEY", "value"));
    }
}
