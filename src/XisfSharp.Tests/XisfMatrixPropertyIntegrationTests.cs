using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfMatrixPropertyIntegrationTests
{
    [TestMethod]
    public async Task WriteRead_CommentAndFormat()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<int>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I32Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQAAAAIAAAADAAAABAAAAAUAAAAGAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I32Matrix);
        image.Properties[0].Value.ShouldBe(new int[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_UI8Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<byte>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UI8Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQIDBAUG</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UI8Matrix);
        image.Properties[0].Value.ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_I8Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<sbyte>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I8Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQIDBAUG</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I8Matrix);
        image.Properties[0].Value.ShouldBe(new sbyte[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_UI16Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<ushort>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UI16Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQACAAMABAAFAAYA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UI16Matrix);
        image.Properties[0].Value.ShouldBe(new ushort[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_I16Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<short>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I16Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQACAAMABAAFAAYA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I16Matrix);
        image.Properties[0].Value.ShouldBe(new short[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_UI32Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<uint>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UI32Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQAAAAIAAAADAAAABAAAAAUAAAAGAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UI32Matrix);
        image.Properties[0].Value.ShouldBe(new uint[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_I32Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<int>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I32Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQAAAAIAAAADAAAABAAAAAUAAAAGAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I32Matrix);
        image.Properties[0].Value.ShouldBe(new int[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_UI64Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<ulong>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UI64Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQAAAAAAAAACAAAAAAAAAAMAAAAAAAAABAAAAAAAAAAFAAAAAAAAAAYAAAAAAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UI64Matrix);
        image.Properties[0].Value.ShouldBe(new ulong[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_I64Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<long>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="I64Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AQAAAAAAAAACAAAAAAAAAAMAAAAAAAAABAAAAAAAAAAFAAAAAAAAAAYAAAAAAAAA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.I64Matrix);
        image.Properties[0].Value.ShouldBe(new long[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_F32Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<float>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="F32Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AACAPwAAAEAAAEBAAACAQAAAoEAAAMBA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.F32Matrix);
        image.Properties[0].Value.ShouldBe(new float[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_F64Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<double>("test", [1, 2, 3, 4, 5, 6], 2, 3, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="F64Matrix" format="format" comment="comment" rows="2" columns="3" location="inline:base64">AAAAAAAA8D8AAAAAAAAAQAAAAAAAAAhAAAAAAAAAEEAAAAAAAAAUQAAAAAAAABhA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.F64Matrix);
        image.Properties[0].Value.ShouldBe(new double[] { 1, 2, 3, 4, 5, 6 });
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(6);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }

    [TestMethod]
    public async Task WriteRead_2DArray_F32Matrix()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        float[,] matrix = { { 1f, 2f, 3f }, { 4f, 5f, 6f } };
        originalImage.Properties.Add(XisfMatrixProperty.Create("test", matrix));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="F32Matrix" rows="2" columns="3" location="inline:base64">AACAPwAAAEAAAEBAAACAQAAAoEAAAMBA</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Type.ShouldBe(XisfPropertyType.F32Matrix);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(2);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(3);
    }
}
