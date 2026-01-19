
using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfScalarPropertyTests
{
    [TestMethod]
    public void Create_Boolean_CorrectType()
    {
        var property = XisfScalarProperty.Create("test", true);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(true);
        property.Type.ShouldBe(XisfPropertyType.Boolean);
    }

    [TestMethod]
    public void Create_SByte_CorrectType()
    {
        sbyte value = -42;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.Int8);
    }

    [TestMethod]
    public void Create_Byte_CorrectType()
    {
        byte value = 255;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.UInt8);
    }

    [TestMethod]
    public void Create_Int16_CorrectType()
    {
        short value = -1234;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.Int16);
    }

    [TestMethod]
    public void Create_UInt16_CorrectType()
    {
        ushort value = 65535;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.UInt16);
    }

    [TestMethod]
    public void Create_Int32_CorrectType()
    {
        int value = -123456;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.Int32);
    }

    [TestMethod]
    public void Create_UInt32_CorrectType()
    {
        uint value = 4294967295;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.UInt32);
    }

    [TestMethod]
    public void Create_Int64_CorrectType()
    {
        long value = -9223372036854775807;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.Int64);
    }

    [TestMethod]
    public void Create_UInt64_CorrectType()
    {
        ulong value = 18446744073709551615;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.UInt64);
    }

    [TestMethod]
    public void Create_Float_CorrectType()
    {
        float value = 3.14159f;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.Float32);
    }

    [TestMethod]
    public void Create_Double_CorrectType()
    {
        double value = 3.141592653589793;
        var property = XisfScalarProperty.Create("test", value);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(value);
        property.Type.ShouldBe(XisfPropertyType.Float64);
    }

    [TestMethod]
    public void Create_WithComment()
    {
        var property = XisfScalarProperty.Create("test", 42, "This is a comment");

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(42);
        property.Type.ShouldBe(XisfPropertyType.Int32);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBe("This is a comment");
    }

    [TestMethod]
    public void Create_WithFormatAndComment()
    {
        var property = XisfScalarProperty.Create("test", 3.14, "Pi approximation", "%.2f");

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(3.14);
        property.Type.ShouldBe(XisfPropertyType.Float64);
        property.Format.ShouldBe("%.2f");
        property.Comment.ShouldBe("Pi approximation");
    }

    [TestMethod]
    public void Create_WithNullComment()
    {
        var property = XisfScalarProperty.Create("test", 123, null);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(123);
        property.Type.ShouldBe(XisfPropertyType.Int32);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_WithNullFormatAndComment()
    {
        var property = XisfScalarProperty.Create("test", 456, null, null);

        property.Id.ShouldBe("test");
        property.Value.ShouldBe(456);
        property.Type.ShouldBe(XisfPropertyType.Int32);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Constructor_Scalar_Boolean()
    {
        var property = new XisfScalarProperty("test", XisfPropertyType.Boolean, true);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.Boolean);
        property.Value.ShouldBe(true);
    }

    [TestMethod]
    public void Constructor_Scalar_Int32()
    {
        var property = new XisfScalarProperty("test", XisfPropertyType.Int32, 42);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.Int32);
        property.Value.ShouldBe(42);
    }

    [TestMethod]
    public void Constructor_Scalar_Float64()
    {
        var property = new XisfScalarProperty("test", XisfPropertyType.Float64, 3.14);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.Float64);
        property.Value.ShouldBe(3.14);
    }

    [TestMethod]
    public void Constructor_NonScalarType_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new XisfScalarProperty("test", XisfPropertyType.String, "value"))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void Constructor_ComplexType_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new XisfScalarProperty("test", XisfPropertyType.Complex32, 0))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void Constructor_VectorType_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new XisfScalarProperty("test", XisfPropertyType.I32Vector, new int[0]))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void ToString_ReturnsValueAsString()
    {
        var property = XisfScalarProperty.Create("test", 42);

        property.ToString().ShouldBe("42");
    }

    [TestMethod]
    public void ToString_Boolean_ReturnsValueAsString()
    {
        var property = XisfScalarProperty.Create("test", true);

        property.ToString().ShouldBe("True");
    }

    [TestMethod]
    public void ToString_Double_ReturnsValueAsString()
    {
        var property = XisfScalarProperty.Create("test", 3.14);

        property.ToString().ShouldBe("3.14");
    }
}
