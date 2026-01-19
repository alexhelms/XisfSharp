using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfVectorPropertyIntegrationTests
{
    [TestMethod]
    public async Task WriteRead_CommentAndFormat()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<int>("test", [1, 2, 3], "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I32Vector" format="format" comment="comment" length="3" location="inline:base64">AQAAAAIAAAADAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I32Vector);
        image.Properties[0].Value.ShouldBe(new int[] { 1, 2, 3 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
    }

    [TestMethod]
    public async Task WriteRead_UI8Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<byte>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UI8Vector" length="3" location="inline:base64">AQID</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UI8Vector);
        image.Properties[0].Value.ShouldBe(new byte[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_I8Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<sbyte>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I8Vector" length="3" location="inline:base64">AQID</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I8Vector);
        image.Properties[0].Value.ShouldBe(new sbyte[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_UI16Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<ushort>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UI16Vector" length="3" location="inline:base64">AQACAAMA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UI16Vector);
        image.Properties[0].Value.ShouldBe(new ushort[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_I16Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<short>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I16Vector" length="3" location="inline:base64">AQACAAMA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I16Vector);
        image.Properties[0].Value.ShouldBe(new short[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_UI32Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<uint>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UI32Vector" length="3" location="inline:base64">AQAAAAIAAAADAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UI32Vector);
        image.Properties[0].Value.ShouldBe(new uint[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_I32Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<int>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I32Vector" length="3" location="inline:base64">AQAAAAIAAAADAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I32Vector);
        image.Properties[0].Value.ShouldBe(new int[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_UI64Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<ulong>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UI64Vector" length="3" location="inline:base64">AQAAAAAAAAACAAAAAAAAAAMAAAAAAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UI64Vector);
        image.Properties[0].Value.ShouldBe(new ulong[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_I64Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<long>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I64Vector" length="3" location="inline:base64">AQAAAAAAAAACAAAAAAAAAAMAAAAAAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I64Vector);
        image.Properties[0].Value.ShouldBe(new long[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_F32Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<float>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="F32Vector" length="3" location="inline:base64">AACAPwAAAEAAAEBA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.F32Vector);
        image.Properties[0].Value.ShouldBe(new float[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task WriteRead_F64Vector()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<double>("test", [1, 2, 3]));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="F64Vector" length="3" location="inline:base64">AAAAAAAA8D8AAAAAAAAAQAAAAAAAAAhA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.F64Vector);
        image.Properties[0].Value.ShouldBe(new double[] { 1, 2, 3 });
    }
}
