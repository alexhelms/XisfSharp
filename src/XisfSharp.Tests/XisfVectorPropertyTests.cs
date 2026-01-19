using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfVectorPropertyTests
{
    [TestMethod]
    public void Create_SByteArray_CorrectType()
    {
        sbyte[] elements = [-1, 0, 1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I8Vector);
        property.Length.ShouldBe(5);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_ByteArray_CorrectType()
    {
        byte[] elements = [0, 1, 2, 255];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.UI8Vector);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_Int16Array_CorrectType()
    {
        short[] elements = [-100, 0, 100, 1000];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I16Vector);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_UInt16Array_CorrectType()
    {
        ushort[] elements = [0, 100, 1000, 65535];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.UI16Vector);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_Int32Array_CorrectType()
    {
        int[] elements = [-1000, 0, 1000, 123456];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I32Vector);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_UInt32Array_CorrectType()
    {
        uint[] elements = [0, 1000, 123456, 4294967295];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.UI32Vector);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_Int64Array_CorrectType()
    {
        long[] elements = [-1000000, 0, 1000000, 9223372036854775807];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I64Vector);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_UInt64Array_CorrectType()
    {
        ulong[] elements = [0, 1000000, 18446744073709551615];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.UI64Vector);
        property.Length.ShouldBe(3);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_FloatArray_CorrectType()
    {
        float[] elements = [0.0f, 1.5f, 3.14159f, -2.71828f];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.F32Vector);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_DoubleArray_CorrectType()
    {
        double[] elements = [0.0, 1.5, 3.141592653589793, -2.718281828459045];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.F64Vector);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_EmptyArray()
    {
        int[] elements = [];
        var property = XisfVectorProperty.Create("test", elements);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I32Vector);
        property.Length.ShouldBe(0);
    }

    [TestMethod]
    public void Create_WithComment()
    {
        int[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements, "Test comment");

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I32Vector);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBe("Test comment");
    }

    [TestMethod]
    public void Create_WithFormatAndComment()
    {
        double[] elements = [1.1, 2.2, 3.3];
        var property = XisfVectorProperty.Create("test", elements, "Formatted values", "%.2f");

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.F64Vector);
        property.Format.ShouldBe("%.2f");
        property.Comment.ShouldBe("Formatted values");
    }

    [TestMethod]
    public void Create_WithNullComment()
    {
        int[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements, null);

        property.Id.ShouldBe("test");
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_WithNullFormatAndComment()
    {
        int[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements, null, null);

        property.Id.ShouldBe("test");
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Constructor_I8Vector()
    {
        sbyte[] elements = [1, 2, 3];
        var property = new XisfVectorProperty("test", XisfPropertyType.I8Vector, elements);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.I8Vector);
        property.Elements.ShouldBe(elements);
    }

    [TestMethod]
    public void Constructor_UI32Vector()
    {
        uint[] elements = [1, 2, 3];
        var property = new XisfVectorProperty("test", XisfPropertyType.UI32Vector, elements);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.UI32Vector);
        property.Elements.ShouldBe(elements);
    }

    [TestMethod]
    public void Constructor_F64Vector()
    {
        double[] elements = [1.1, 2.2, 3.3];
        var property = new XisfVectorProperty("test", XisfPropertyType.F64Vector, elements);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.F64Vector);
        property.Elements.ShouldBe(elements);
    }

    [TestMethod]
    public void Constructor_NonVectorType_Throws()
    {
        int[] elements = [1, 2, 3];

        Should.Throw<ArgumentException>(() =>
            new XisfVectorProperty("test", XisfPropertyType.Int32, elements))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void Constructor_MatrixType_Throws()
    {
        int[] elements = [1, 2, 3];

        Should.Throw<ArgumentException>(() =>
            new XisfVectorProperty("test", XisfPropertyType.I32Matrix, elements))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void Constructor_StringType_Throws()
    {
        int[] elements = [1, 2, 3];

        Should.Throw<ArgumentException>(() =>
            new XisfVectorProperty("test", XisfPropertyType.String, elements))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void GetElements_ReturnsTypedArray()
    {
        int[] elements = [1, 2, 3, 4, 5];
        var property = XisfVectorProperty.Create("test", elements);

        var retrieved = property.GetElements<int>();

        retrieved.ShouldBe(elements);
    }

    [TestMethod]
    public void GetElements_Float_ReturnsTypedArray()
    {
        float[] elements = [1.1f, 2.2f, 3.3f];
        var property = XisfVectorProperty.Create("test", elements);

        var retrieved = property.GetElements<float>();

        retrieved.ShouldBe(elements);
    }

    [TestMethod]
    public void GetBytes_UI8Vector()
    {
        byte[] elements = [1, 2, 3, 4, 5];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.ShouldBe(elements);
    }

    [TestMethod]
    public void GetBytes_I8Vector()
    {
        sbyte[] elements = [-1, 0, 1, 2];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(4);
        bytes[0].ShouldBe((byte)255); // -1 as unsigned byte
        bytes[1].ShouldBe((byte)0);
        bytes[2].ShouldBe((byte)1);
        bytes[3].ShouldBe((byte)2);
    }

    [TestMethod]
    public void GetBytes_I16Vector()
    {
        short[] elements = [256, 512];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(4); // 2 elements * 2 bytes each
    }

    [TestMethod]
    public void GetBytes_UI16Vector()
    {
        ushort[] elements = [256, 512];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(4); // 2 elements * 2 bytes each
    }

    [TestMethod]
    public void GetBytes_I32Vector()
    {
        int[] elements = [1000, 2000];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(8); // 2 elements * 4 bytes each
    }

    [TestMethod]
    public void GetBytes_UI32Vector()
    {
        uint[] elements = [1000, 2000];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(8); // 2 elements * 4 bytes each
    }

    [TestMethod]
    public void GetBytes_I64Vector()
    {
        long[] elements = [1000, 2000];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(16); // 2 elements * 8 bytes each
    }

    [TestMethod]
    public void GetBytes_UI64Vector()
    {
        ulong[] elements = [1000, 2000];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(16); // 2 elements * 8 bytes each
    }

    [TestMethod]
    public void GetBytes_F32Vector()
    {
        float[] elements = [1.5f, 2.5f];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(8); // 2 elements * 4 bytes each
    }

    [TestMethod]
    public void GetBytes_F64Vector()
    {
        double[] elements = [1.5, 2.5];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(16); // 2 elements * 8 bytes each
    }

    [TestMethod]
    public void GetBytes_EmptyVector()
    {
        int[] elements = [];
        var property = XisfVectorProperty.Create("test", elements);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(0);
    }

    [TestMethod]
    public void ToString_ReturnsDescription()
    {
        int[] elements = [1, 2, 3, 4, 5];
        var property = XisfVectorProperty.Create("test", elements);

        var str = property.ToString();

        str.ShouldBe("Vector<I32Vector> of 5 elements");
    }

    [TestMethod]
    public void ToString_EmptyVector()
    {
        double[] elements = [];
        var property = XisfVectorProperty.Create("test", elements);

        var str = property.ToString();

        str.ShouldBe("Vector<F64Vector> of 0 elements");
    }

    [TestMethod]
    public void Length_ReturnsElementCount()
    {
        int[] elements = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var property = XisfVectorProperty.Create("test", elements);

        property.Length.ShouldBe(10);
    }

    [TestMethod]
    public void IXisfProperty_Value_ReturnsElements()
    {
        int[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.Value.ShouldBe(elements);
    }

    [TestMethod]
    public void GetBytesPerElement_I8Vector()
    {
        sbyte[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(sbyte));
        property.GetBytesPerElement().ShouldBe(1);
    }

    [TestMethod]
    public void GetBytesPerElement_UI8Vector()
    {
        byte[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(byte));
        property.GetBytesPerElement().ShouldBe(1);
    }

    [TestMethod]
    public void GetBytesPerElement_I16Vector()
    {
        short[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(short));
        property.GetBytesPerElement().ShouldBe(2);
    }

    [TestMethod]
    public void GetBytesPerElement_UI16Vector()
    {
        ushort[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(ushort));
        property.GetBytesPerElement().ShouldBe(2);
    }

    [TestMethod]
    public void GetBytesPerElement_I32Vector()
    {
        int[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(int));
        property.GetBytesPerElement().ShouldBe(4);
    }

    [TestMethod]
    public void GetBytesPerElement_UI32Vector()
    {
        uint[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(uint));
        property.GetBytesPerElement().ShouldBe(4);
    }

    [TestMethod]
    public void GetBytesPerElement_I64Vector()
    {
        long[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(long));
        property.GetBytesPerElement().ShouldBe(8);
    }

    [TestMethod]
    public void GetBytesPerElement_UI64Vector()
    {
        ulong[] elements = [1, 2, 3];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(ulong));
        property.GetBytesPerElement().ShouldBe(8);
    }

    [TestMethod]
    public void GetBytesPerElement_F32Vector()
    {
        float[] elements = [1.0f, 2.0f, 3.0f];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(float));
        property.GetBytesPerElement().ShouldBe(4);
    }

    [TestMethod]
    public void GetBytesPerElement_F64Vector()
    {
        double[] elements = [1.0, 2.0, 3.0];
        var property = XisfVectorProperty.Create("test", elements);

        property.GetBytesPerElement().ShouldBe(sizeof(double));
        property.GetBytesPerElement().ShouldBe(8);
    }

    [TestMethod]
    public void GetBytesPerElement_MatchesTotalByteSize()
    {
        int[] elements = [1, 2, 3, 4, 5];
        var property = XisfVectorProperty.Create("test", elements);

        var totalBytes = property.GetBytes().Length;
        var bytesPerElement = property.GetBytesPerElement();

        totalBytes.ShouldBe(property.Length * bytesPerElement);
    }
}
