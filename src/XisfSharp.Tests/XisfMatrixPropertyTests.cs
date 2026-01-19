using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfMatrixPropertyTests
{
    [TestMethod]
    public void Create_SByteArray_CorrectType()
    {
        sbyte[] elements = [-1, 0, 1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 3);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I8Matrix);
        property.Rows.ShouldBe(2);
        property.Columns.ShouldBe(3);
        property.Length.ShouldBe(6);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_ByteArray_CorrectType()
    {
        byte[] elements = [0, 1, 2, 3, 4, 5];
        var property = XisfMatrixProperty.Create("test", elements, 2, 3);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.UI8Matrix);
        property.Rows.ShouldBe(2);
        property.Columns.ShouldBe(3);
        property.Length.ShouldBe(6);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_Int16Array_CorrectType()
    {
        short[] elements = [-100, 0, 100, 1000, -1000, 500];
        var property = XisfMatrixProperty.Create("test", elements, 3, 2);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I16Matrix);
        property.Rows.ShouldBe(3);
        property.Columns.ShouldBe(2);
        property.Length.ShouldBe(6);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_UInt16Array_CorrectType()
    {
        ushort[] elements = [0, 100, 1000, 10000];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.UI16Matrix);
        property.Rows.ShouldBe(2);
        property.Columns.ShouldBe(2);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_Int32Array_CorrectType()
    {
        int[] elements = [-1000, 0, 1000, 123456];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I32Matrix);
        property.Rows.ShouldBe(2);
        property.Columns.ShouldBe(2);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_UInt32Array_CorrectType()
    {
        uint[] elements = [0, 1000, 123456, 4294967295];
        var property = XisfMatrixProperty.Create("test", elements, 4, 1);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.UI32Matrix);
        property.Rows.ShouldBe(4);
        property.Columns.ShouldBe(1);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_Int64Array_CorrectType()
    {
        long[] elements = [-1000000, 0, 1000000, 9223372036854775807];
        var property = XisfMatrixProperty.Create("test", elements, 1, 4);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I64Matrix);
        property.Rows.ShouldBe(1);
        property.Columns.ShouldBe(4);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_UInt64Array_CorrectType()
    {
        ulong[] elements = [0, 1000000, 18446744073709551615];
        var property = XisfMatrixProperty.Create("test", elements, 1, 3);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.UI64Matrix);
        property.Rows.ShouldBe(1);
        property.Columns.ShouldBe(3);
        property.Length.ShouldBe(3);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_FloatArray_CorrectType()
    {
        float[] elements = [0.0f, 1.5f, 3.14159f, -2.71828f, 1.0f, 2.0f];
        var property = XisfMatrixProperty.Create("test", elements, 3, 2);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.F32Matrix);
        property.Rows.ShouldBe(3);
        property.Columns.ShouldBe(2);
        property.Length.ShouldBe(6);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_DoubleArray_CorrectType()
    {
        double[] elements = [0.0, 1.5, 3.141592653589793, -2.718281828459045];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.F64Matrix);
        property.Rows.ShouldBe(2);
        property.Columns.ShouldBe(2);
        property.Length.ShouldBe(4);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_SquareMatrix()
    {
        int[] elements = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        var property = XisfMatrixProperty.Create("test", elements, 3, 3);

        property.Rows.ShouldBe(3);
        property.Columns.ShouldBe(3);
        property.Length.ShouldBe(9);
    }

    [TestMethod]
    public void Create_SingleElementMatrix()
    {
        int[] elements = [42];
        var property = XisfMatrixProperty.Create("test", elements, 1, 1);

        property.Rows.ShouldBe(1);
        property.Columns.ShouldBe(1);
        property.Length.ShouldBe(1);
    }

    [TestMethod]
    public void Create_WithComment()
    {
        int[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2, "Test comment");

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.I32Matrix);
        property.Format.ShouldBeNull();
        property.Comment.ShouldBe("Test comment");
    }

    [TestMethod]
    public void Create_WithFormatAndComment()
    {
        double[] elements = [1.1, 2.2, 3.3, 4.4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2, "Formatted matrix", "%.2f");

        property.Id.ShouldBe("test");
        property.Elements.ShouldBe(elements);
        property.Type.ShouldBe(XisfPropertyType.F64Matrix);
        property.Format.ShouldBe("%.2f");
        property.Comment.ShouldBe("Formatted matrix");
    }

    [TestMethod]
    public void Create_WithNullComment()
    {
        int[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2, null);

        property.Id.ShouldBe("test");
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_WithNullFormatAndComment()
    {
        int[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2, null, null);

        property.Id.ShouldBe("test");
        property.Format.ShouldBeNull();
        property.Comment.ShouldBeNull();
    }

    [TestMethod]
    public void Create_MismatchedDimensions_Throws()
    {
        int[] elements = [1, 2, 3, 4, 5];

        Should.Throw<ArgumentException>(() =>
            XisfMatrixProperty.Create("test", elements, 2, 2))
            .Message.ShouldContain("rows x columns");
    }

    [TestMethod]
    public void Create_TooFewElements_Throws()
    {
        int[] elements = [1, 2];

        Should.Throw<ArgumentException>(() =>
            XisfMatrixProperty.Create("test", elements, 2, 3))
            .Message.ShouldContain("rows x columns");
    }

    [TestMethod]
    public void Create_TooManyElements_Throws()
    {
        int[] elements = [1, 2, 3, 4, 5, 6, 7];

        Should.Throw<ArgumentException>(() =>
            XisfMatrixProperty.Create("test", elements, 2, 3))
            .Message.ShouldContain("rows x columns");
    }

    [TestMethod]
    public void Constructor_I8Matrix()
    {
        sbyte[] elements = [1, 2, 3, 4];
        var property = new XisfMatrixProperty("test", XisfPropertyType.I8Matrix, elements, 2, 2);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.I8Matrix);
        property.Elements.ShouldBe(elements);
        property.Rows.ShouldBe(2);
        property.Columns.ShouldBe(2);
    }

    [TestMethod]
    public void Constructor_UI32Matrix()
    {
        uint[] elements = [1, 2, 3, 4, 5, 6];
        var property = new XisfMatrixProperty("test", XisfPropertyType.UI32Matrix, elements, 3, 2);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.UI32Matrix);
        property.Elements.ShouldBe(elements);
        property.Rows.ShouldBe(3);
        property.Columns.ShouldBe(2);
    }

    [TestMethod]
    public void Constructor_F64Matrix()
    {
        double[] elements = [1.1, 2.2, 3.3, 4.4];
        var property = new XisfMatrixProperty("test", XisfPropertyType.F64Matrix, elements, 2, 2);

        property.Id.ShouldBe("test");
        property.Type.ShouldBe(XisfPropertyType.F64Matrix);
        property.Elements.ShouldBe(elements);
        property.Rows.ShouldBe(2);
        property.Columns.ShouldBe(2);
    }

    [TestMethod]
    public void Constructor_NonMatrixType_Throws()
    {
        int[] elements = [1, 2, 3, 4];

        Should.Throw<ArgumentException>(() =>
            new XisfMatrixProperty("test", XisfPropertyType.Int32, elements, 2, 2))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void Constructor_VectorType_Throws()
    {
        int[] elements = [1, 2, 3, 4];

        Should.Throw<ArgumentException>(() =>
            new XisfMatrixProperty("test", XisfPropertyType.I32Vector, elements, 2, 2))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void Constructor_StringType_Throws()
    {
        int[] elements = [1, 2, 3, 4];

        Should.Throw<ArgumentException>(() =>
            new XisfMatrixProperty("test", XisfPropertyType.String, elements, 2, 2))
            .ParamName.ShouldBe("type");
    }

    [TestMethod]
    public void Constructor_MismatchedDimensions_Throws()
    {
        int[] elements = [1, 2, 3, 4, 5];

        Should.Throw<ArgumentException>(() =>
            new XisfMatrixProperty("test", XisfPropertyType.I32Matrix, elements, 2, 2))
            .ParamName.ShouldBe("elements");
    }

    [TestMethod]
    public void GetElements_ReturnsTypedArray()
    {
        int[] elements = [1, 2, 3, 4, 5, 6];
        var property = XisfMatrixProperty.Create("test", elements, 2, 3);

        var retrieved = property.GetElements<int>();

        retrieved.ShouldBe(elements);
    }

    [TestMethod]
    public void GetElements_Float_ReturnsTypedArray()
    {
        float[] elements = [1.1f, 2.2f, 3.3f, 4.4f];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        var retrieved = property.GetElements<float>();

        retrieved.ShouldBe(elements);
    }

    [TestMethod]
    public void GetBytes_UI8Matrix()
    {
        byte[] elements = [1, 2, 3, 4, 5, 6];
        var property = XisfMatrixProperty.Create("test", elements, 2, 3);

        var bytes = property.GetBytes();

        bytes.ShouldBe(elements);
    }

    [TestMethod]
    public void GetBytes_I8Matrix()
    {
        sbyte[] elements = [-1, 0, 1, 2];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(4);
        bytes[0].ShouldBe((byte)255); // -1 as unsigned byte
        bytes[1].ShouldBe((byte)0);
        bytes[2].ShouldBe((byte)1);
        bytes[3].ShouldBe((byte)2);
    }

    [TestMethod]
    public void GetBytes_I16Matrix()
    {
        short[] elements = [256, 512, 1024, 2048];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(8); // 4 elements * 2 bytes each
    }

    [TestMethod]
    public void GetBytes_UI16Matrix()
    {
        ushort[] elements = [256, 512];
        var property = XisfMatrixProperty.Create("test", elements, 1, 2);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(4); // 2 elements * 2 bytes each
    }

    [TestMethod]
    public void GetBytes_I32Matrix()
    {
        int[] elements = [1000, 2000, 3000, 4000];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(16); // 4 elements * 4 bytes each
    }

    [TestMethod]
    public void GetBytes_UI32Matrix()
    {
        uint[] elements = [1000, 2000];
        var property = XisfMatrixProperty.Create("test", elements, 1, 2);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(8); // 2 elements * 4 bytes each
    }

    [TestMethod]
    public void GetBytes_I64Matrix()
    {
        long[] elements = [1000, 2000];
        var property = XisfMatrixProperty.Create("test", elements, 2, 1);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(16); // 2 elements * 8 bytes each
    }

    [TestMethod]
    public void GetBytes_UI64Matrix()
    {
        ulong[] elements = [1000, 2000];
        var property = XisfMatrixProperty.Create("test", elements, 1, 2);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(16); // 2 elements * 8 bytes each
    }

    [TestMethod]
    public void GetBytes_F32Matrix()
    {
        float[] elements = [1.5f, 2.5f, 3.5f, 4.5f];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(16); // 4 elements * 4 bytes each
    }

    [TestMethod]
    public void GetBytes_F64Matrix()
    {
        double[] elements = [1.5, 2.5];
        var property = XisfMatrixProperty.Create("test", elements, 1, 2);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(16); // 2 elements * 8 bytes each
    }

    [TestMethod]
    public void GetBytes_SingleElement()
    {
        int[] elements = [42];
        var property = XisfMatrixProperty.Create("test", elements, 1, 1);

        var bytes = property.GetBytes();

        bytes.Length.ShouldBe(4); // 1 element * 4 bytes
    }

    [TestMethod]
    public void ToString_ReturnsDescription()
    {
        int[] elements = [1, 2, 3, 4, 5, 6];
        var property = XisfMatrixProperty.Create("test", elements, 2, 3);

        var str = property.ToString();

        str.ShouldBe("Matrix<I32Matrix> [2×3]");
    }

    [TestMethod]
    public void ToString_SquareMatrix()
    {
        double[] elements = [1.1, 2.2, 3.3, 4.4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        var str = property.ToString();

        str.ShouldBe("Matrix<F64Matrix> [2×2]");
    }

    [TestMethod]
    public void ToString_SingleElement()
    {
        int[] elements = [42];
        var property = XisfMatrixProperty.Create("test", elements, 1, 1);

        var str = property.ToString();

        str.ShouldBe("Matrix<I32Matrix> [1×1]");
    }

    [TestMethod]
    public void Length_ReturnsElementCount()
    {
        int[] elements = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        var property = XisfMatrixProperty.Create("test", elements, 3, 4);

        property.Length.ShouldBe(12);
        property.Length.ShouldBe(property.Rows * property.Columns);
    }

    [TestMethod]
    public void Length_SquareMatrix()
    {
        int[] elements = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        var property = XisfMatrixProperty.Create("test", elements, 3, 3);

        property.Length.ShouldBe(9);
        property.Length.ShouldBe(property.Rows * property.Columns);
    }

    [TestMethod]
    public void IXisfProperty_Value_ReturnsElements()
    {
        int[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.Value.ShouldBe(elements);
    }

    [TestMethod]
    public void RowsAndColumns_DifferentDimensions()
    {
        int[] elements = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var property = XisfMatrixProperty.Create("test", elements, 2, 5);

        property.Rows.ShouldBe(2);
        property.Columns.ShouldBe(5);
    }

    [TestMethod]
    public void RowsAndColumns_SingleRow()
    {
        int[] elements = [1, 2, 3, 4, 5];
        var property = XisfMatrixProperty.Create("test", elements, 1, 5);

        property.Rows.ShouldBe(1);
        property.Columns.ShouldBe(5);
    }

    [TestMethod]
    public void RowsAndColumns_SingleColumn()
    {
        int[] elements = [1, 2, 3, 4, 5];
        var property = XisfMatrixProperty.Create("test", elements, 5, 1);

        property.Rows.ShouldBe(5);
        property.Columns.ShouldBe(1);
    }

    [TestMethod]
    public void GetBytesPerElement_I8Matrix()
    {
        sbyte[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(sbyte));
        property.GetBytesPerElement().ShouldBe(1);
    }

    [TestMethod]
    public void GetBytesPerElement_UI8Matrix()
    {
        byte[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(byte));
        property.GetBytesPerElement().ShouldBe(1);
    }

    [TestMethod]
    public void GetBytesPerElement_I16Matrix()
    {
        short[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(short));
        property.GetBytesPerElement().ShouldBe(2);
    }

    [TestMethod]
    public void GetBytesPerElement_UI16Matrix()
    {
        ushort[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(ushort));
        property.GetBytesPerElement().ShouldBe(2);
    }

    [TestMethod]
    public void GetBytesPerElement_I32Matrix()
    {
        int[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(int));
        property.GetBytesPerElement().ShouldBe(4);
    }

    [TestMethod]
    public void GetBytesPerElement_UI32Matrix()
    {
        uint[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(uint));
        property.GetBytesPerElement().ShouldBe(4);
    }

    [TestMethod]
    public void GetBytesPerElement_I64Matrix()
    {
        long[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(long));
        property.GetBytesPerElement().ShouldBe(8);
    }

    [TestMethod]
    public void GetBytesPerElement_UI64Matrix()
    {
        ulong[] elements = [1, 2, 3, 4];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(ulong));
        property.GetBytesPerElement().ShouldBe(8);
    }

    [TestMethod]
    public void GetBytesPerElement_F32Matrix()
    {
        float[] elements = [1.0f, 2.0f, 3.0f, 4.0f];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(float));
        property.GetBytesPerElement().ShouldBe(4);
    }

    [TestMethod]
    public void GetBytesPerElement_F64Matrix()
    {
        double[] elements = [1.0, 2.0, 3.0, 4.0];
        var property = XisfMatrixProperty.Create("test", elements, 2, 2);

        property.GetBytesPerElement().ShouldBe(sizeof(double));
        property.GetBytesPerElement().ShouldBe(8);
    }

    [TestMethod]
    public void GetBytesPerElement_MatchesTotalByteSize()
    {
        int[] elements = [1, 2, 3, 4, 5, 6];
        var property = XisfMatrixProperty.Create("test", elements, 2, 3);

        var totalBytes = property.GetBytes().Length;
        var bytesPerElement = property.GetBytesPerElement();

        totalBytes.ShouldBe(property.Length * bytesPerElement);
    }

    [TestMethod]
    public void GetBytesPerElement_DifferentMatrixSizes()
    {
        int[] elements1 = [1, 2, 3, 4];
        int[] elements2 = [1, 2, 3, 4, 5, 6, 7, 8, 9];

        var property1 = XisfMatrixProperty.Create("test1", elements1, 2, 2);
        var property2 = XisfMatrixProperty.Create("test2", elements2, 3, 3);

        // Different matrix sizes but same element type should have same bytes per element
        property1.GetBytesPerElement().ShouldBe(property2.GetBytesPerElement());
        property1.GetBytesPerElement().ShouldBe(4);
    }
}
