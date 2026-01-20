using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfPropertyEdgeCaseTests
{
    [TestMethod]
    public async Task WriteRead_StringWithXmlSpecialCharacters()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(new XisfStringProperty("test", "<>&\"'"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="String">&lt;&gt;&amp;"'</Property>""");

        image.Properties[0].Value.ShouldBe("<>&\"'");
    }

    [TestMethod]
    public async Task WriteRead_FloatSpecialValues()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<float>("test", [float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.NegativeZero, 0.0f]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties.Count.ShouldBe(1);
        var values = (float[])image.Properties[0].Value!;
        float.IsNaN(values[0]).ShouldBeTrue();
        values[1].ShouldBe(float.PositiveInfinity);
        values[2].ShouldBe(float.NegativeInfinity);
        values[3].ShouldBe(float.NegativeZero);
        values[4].ShouldBe(0.0f);
    }

    [TestMethod]
    public async Task WriteRead_DoubleSpecialValues()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<double>("test", [double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.NegativeZero, 0.0d]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties.Count.ShouldBe(1);
        var values = (double[])image.Properties[0].Value!;
        double.IsNaN(values[0]).ShouldBeTrue();
        values[1].ShouldBe(double.PositiveInfinity);
        values[2].ShouldBe(double.NegativeInfinity);
        values[3].ShouldBe(double.NegativeZero);
        values[4].ShouldBe(0.0f);
    }

    [TestMethod]
    public async Task WriteRead_MultiplePropertiesOfDifferentTypes()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<bool>("bool", true));
        originalImage.Properties.Add(XisfScalarProperty.Create<int>("int", 42));
        originalImage.Properties.Add(new XisfStringProperty("string", "test"));
        originalImage.Properties.Add(XisfVectorProperty.Create<float>("vector", [1.0f, 2.0f, 3.0f]));
        originalImage.Properties.Add(XisfMatrixProperty.Create<double>("matrix", [1, 2, 3, 4], 2, 2));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        image.Properties.Count.ShouldBe(5);
        image.Properties.FirstOrDefault(p => p.Id == "bool")?.Value.ShouldBe(true);
        image.Properties.FirstOrDefault(p => p.Id == "int")?.Value.ShouldBe(42);
        image.Properties.FirstOrDefault(p => p.Id == "string")?.Value.ShouldBe("test");
        image.Properties.FirstOrDefault(p => p.Id == "vector")?.Value.ShouldBe(new float[] { 1.0f, 2.0f, 3.0f });
        image.Properties.FirstOrDefault(p => p.Id == "matrix")?.Value.ShouldBe(new double[] { 1, 2, 3, 4 });
    }

    [TestMethod]
    public async Task WriteRead_EmptyVector()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<int>("test", []));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Value.ShouldBe(Array.Empty<int>());
        ((XisfVectorProperty)image.Properties[0]).Length.ShouldBe(0);
    }

    [TestMethod]
    public async Task WriteRead_EmptyMatrix()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<double>("test", [], 0, 0));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Value.ShouldBe(Array.Empty<double>());
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(0);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(0);
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(0);
    }

    [TestMethod]
    public async Task WriteRead_BoundaryValues_Byte()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<byte>("test", [byte.MinValue, byte.MaxValue, 127]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new byte[] { 0, 255, 127 });
    }

    [TestMethod]
    public async Task WriteRead_BoundaryValues_SByte()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<sbyte>("test", [sbyte.MinValue, sbyte.MaxValue, 0]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new sbyte[] { -128, 127, 0 });
    }

    [TestMethod]
    public async Task WriteRead_BoundaryValues_Int16()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<short>("test", [short.MinValue, short.MaxValue, 0]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new short[] { -32768, 32767, 0 });
    }

    [TestMethod]
    public async Task WriteRead_BoundaryValues_UInt16()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<ushort>("test", [ushort.MinValue, ushort.MaxValue, 32767]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new ushort[] { 0, 65535, 32767 });
    }

    [TestMethod]
    public async Task WriteRead_BoundaryValues_Int32()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<int>("test", [int.MinValue, int.MaxValue, 0]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new int[] { -2147483648, 2147483647, 0 });
    }

    [TestMethod]
    public async Task WriteRead_BoundaryValues_UInt32()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<uint>("test", [uint.MinValue, uint.MaxValue, 2147483647]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new uint[] { 0, 4294967295, 2147483647 });
    }

    [TestMethod]
    public async Task WriteRead_BoundaryValues_Int64()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<long>("test", [long.MinValue, long.MaxValue, 0]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new long[] { -9223372036854775808, 9223372036854775807, 0 });
    }

    [TestMethod]
    public async Task WriteRead_BoundaryValues_UInt64()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<ulong>("test", [ulong.MinValue, ulong.MaxValue, 9223372036854775807]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new ulong[] { 0, 18446744073709551615, 9223372036854775807 });
    }

    [TestMethod]
    public async Task WriteRead_StringWithUnicode()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(new XisfStringProperty("test", "Hello 世界 🌍 مرحبا Здравствуйте"));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe("Hello 世界 🌍 مرحبا Здравствуйте");
    }

    [TestMethod]
    public async Task WriteRead_StringWithWhitespace()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(new XisfStringProperty("test", "  leading and trailing  \n\t\r"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        image.Properties[0].Value.ShouldBe("  leading and trailing  \n\t\r");
    }

    [TestMethod]
    public async Task WriteRead_EmptyString()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(new XisfStringProperty("test", ""));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe("");
    }

    [TestMethod]
    public async Task WriteRead_VeryLongString()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        var longString = new string('a', 10000);
        originalImage.Properties.Add(new XisfStringProperty("test", longString));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(longString);
    }

    [TestMethod]
    public async Task WriteRead_FormatAndCommentWithSpecialCharacters()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<int>("test", 42, "Comment with <>&\"'", "Format with <>&\"'"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        image.Properties[0].Comment.ShouldBe("Comment with <>&\"'");
        image.Properties[0].Format.ShouldBe("Format with <>&\"'");
    }

    [TestMethod]
    public async Task WriteRead_NullFormatAndComment()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<int>("test", 42, null, null));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Comment.ShouldBeNull();
        image.Properties[0].Format.ShouldBeNull();
    }

    [TestMethod]
    public async Task WriteRead_Matrix_1x1()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<int>("test", [42], 1, 1));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new int[] { 42 });
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(1);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(1);
    }

    [TestMethod]
    public async Task WriteRead_Matrix_1xN()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<int>("test", [1, 2, 3, 4, 5], 1, 5));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new int[] { 1, 2, 3, 4, 5 });
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(1);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(5);
    }

    [TestMethod]
    public async Task WriteRead_Matrix_Nx1()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<int>("test", [1, 2, 3, 4, 5], 5, 1));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new int[] { 1, 2, 3, 4, 5 });
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(5);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(1);
    }

    [TestMethod]
    public async Task WriteRead_Matrix_NonSquare()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfMatrixProperty.Create<int>("test", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12], 3, 4));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(3);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(4);
    }

    [TestMethod]
    public async Task WriteRead_LargeVector()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        var largeArray = Enumerable.Range(0, 10000).ToArray();
        originalImage.Properties.Add(XisfVectorProperty.Create<int>("test", largeArray));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(largeArray);
        ((XisfVectorProperty)image.Properties[0]).Length.ShouldBe(10000);
    }

    [TestMethod]
    public async Task WriteRead_LargeMatrix()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        var largeArray = Enumerable.Range(0, 10000).Select(i => (double)i).ToArray();
        originalImage.Properties.Add(XisfMatrixProperty.Create<double>("test", largeArray, 100, 100));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Value.ShouldBe(largeArray);
        ((XisfMatrixProperty)image.Properties[0]).Rows.ShouldBe(100);
        ((XisfMatrixProperty)image.Properties[0]).Columns.ShouldBe(100);
        ((XisfMatrixProperty)image.Properties[0]).Length.ShouldBe(10000);
    }

    [TestMethod]
    public async Task WriteRead_FloatPrecision()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        var preciseValues = new float[] { 3.14159265f, 2.71828182f, 1.41421356f, 0.000001f, 999999.9f };
        originalImage.Properties.Add(XisfVectorProperty.Create<float>("test", preciseValues));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        var result = (float[])image.Properties[0].Value!;
        for (int i = 0; i < preciseValues.Length; i++)
        {
            result[i].ShouldBe(preciseValues[i]);
        }
    }

    [TestMethod]
    public async Task WriteRead_DoublePrecision()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        var preciseValues = new double[] { Math.PI, Math.E, Math.Sqrt(2), 1e-15, 1e15 };
        originalImage.Properties.Add(XisfVectorProperty.Create<double>("test", preciseValues));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        var result = (double[])image.Properties[0].Value!;
        for (int i = 0; i < preciseValues.Length; i++)
        {
            result[i].ShouldBe(preciseValues[i]);
        }
    }

    [TestMethod]
    public async Task WriteRead_NegativeValues_Comprehensive()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfVectorProperty.Create<int>("int", [-1, -100, -1000000]));
        originalImage.Properties.Add(XisfVectorProperty.Create<float>("float", [-1.5f, -0.001f, -999999.9f]));
        originalImage.Properties.Add(XisfVectorProperty.Create<double>("double", [-Math.PI, -1e-10, -1e10]));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties.Count.ShouldBe(3);
        image.Properties[0].Value.ShouldBe(new int[] { -1, -100, -1000000 });
        image.Properties[1].Value.ShouldBe(new float[] { -1.5f, -0.001f, -999999.9f });
        image.Properties[2].Value.ShouldBe(new double[] { -Math.PI, -1e-10, -1e10 });
    }

    [TestMethod]
    public async Task WriteRead_ManyProperties()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);

        for (int i = 0; i < 100; i++)
        {
            originalImage.Properties.Add(XisfScalarProperty.Create<int>($"property_{i}", i));
        }

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties.Count.ShouldBe(100);
        for (int i = 0; i < 100; i++)
        {
            image.Properties[i].Id.ShouldBe($"property_{i}");
            image.Properties[i].Value.ShouldBe(i);
        }
    }

    [TestMethod]
    public async Task WriteRead_PropertiesPreserveOrder()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<int>("first", 1));
        originalImage.Properties.Add(XisfScalarProperty.Create<int>("second", 2));
        originalImage.Properties.Add(XisfScalarProperty.Create<int>("third", 3));

        var image = await TestHelpers.WriteAndReadImage(originalImage);

        image.Properties[0].Id.ShouldBe("first");
        image.Properties[1].Id.ShouldBe("second");
        image.Properties[2].Id.ShouldBe("third");
    }

    [TestMethod]
    public async Task WriteRead_Boolean_BothValues()
    {
        var originalImage = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<bool>("true", true));
        originalImage.Properties.Add(XisfScalarProperty.Create<bool>("false", false));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("value=\"1\"");
        xml.ShouldContain("value=\"0\"");
        image.Properties[0].Value.ShouldBe(true);
        image.Properties[1].Value.ShouldBe(false);
    }
}
