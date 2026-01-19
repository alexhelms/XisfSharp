using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfScalarPropertyIntegrationTests
{
    [TestMethod]
    public async Task WriteRead_CommentAndFormat()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<int>("test", 123, "comment", "format"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="Int32" format="format" comment="comment" value="123" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.Int32);
        image.Properties[0].Value.ShouldBe(123);
        image.Properties[0].Format.ShouldBe("format");
        image.Properties[0].Comment.ShouldBe("comment");
    }

    [TestMethod]
    public async Task WriteRead_Boolean()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<bool>("test", true));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="Boolean" value="1" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.Boolean);
        image.Properties[0].Value.ShouldBe(true);
    }

    [TestMethod]
    public async Task WriteRead_UInt8()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<byte>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UInt8" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UInt8);
        image.Properties[0].Value.ShouldBe((byte)42);
    }

    [TestMethod]
    public async Task WriteRead_Int8()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<sbyte>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="Int8" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.Int8);
        image.Properties[0].Value.ShouldBe((sbyte)42);
    }

    [TestMethod]
    public async Task WriteRead_UInt16()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<ushort>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UInt16" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UInt16);
        image.Properties[0].Value.ShouldBe((ushort)42);
    }

    [TestMethod]
    public async Task WriteRead_Int16()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<short>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="Int16" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.Int16);
        image.Properties[0].Value.ShouldBe((short)42);
    }

    [TestMethod]
    public async Task WriteRead_UInt32()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<uint>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UInt32" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UInt32);
        image.Properties[0].Value.ShouldBe((uint)42);
    }

    [TestMethod]
    public async Task WriteRead_Int32()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<int>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="Int32" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.Int32);
        image.Properties[0].Value.ShouldBe((int)42);
    }

    [TestMethod]
    public async Task WriteRead_UInt64()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<ulong>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="UInt64" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.UInt64);
        image.Properties[0].Value.ShouldBe((ulong)42);
    }

    [TestMethod]
    public async Task WriteRead_Int64()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<long>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="Int64" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.Int64);
        image.Properties[0].Value.ShouldBe((long)42);
    }

    [TestMethod]
    public async Task WriteRead_Float32()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<float>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="Float32" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.Float32);
        image.Properties[0].Value.ShouldBe((float)42);
    }

    [TestMethod]
    public async Task WriteRead_Float64()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(XisfScalarProperty.Create<double>("test", 42));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="Float64" value="42" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.Float64);
        image.Properties[0].Value.ShouldBe((double)42);
    }

    [TestMethod]
    public async Task WriteRead_TimePoint()
    {
        var utcNow = new DateTimeOffset(2026, 1, 17, 23, 0, 0, TimeSpan.FromHours(-7));
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(new XisfTimePointProperty("test", utcNow));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="TimePoint" value="2026-01-17T23:00:00.0000000-07:00" />""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.TimePoint);
        image.Properties[0].Value.ShouldBe(utcNow);
    }

    [TestMethod]
    public async Task WriteRead_String()
    {
        var originalImage = new XisfImage([1, 2, 3, 4, 5, 6], 3, 2, 1, SampleFormat.UInt8);
        originalImage.Properties.Add(new XisfStringProperty("test", "42"));

        var (image, xml) = await TestHelpers.WriteAndReadImageWithXml(originalImage);

        xml.ShouldContain("""<Property id="test" type="String">42</Property>""");

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Id.ShouldBe("test");
        image.Properties[0].Type.ShouldBe(XisfPropertyType.String);
        image.Properties[0].Value.ShouldBe("42");
    }
}
