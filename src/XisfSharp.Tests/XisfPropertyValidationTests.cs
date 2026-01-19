using System.Buffers.Binary;
using System.Data.SqlTypes;
using System.Reflection.PortableExecutable;
using XisfSharp.IO;
using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfPropertyValidationTests
{
    [TestMethod]
    public void Create_DuplicatePropertyIds_ThrowsException()
    {
        var image = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        image.Properties.Add(XisfScalarProperty.Create<int>("test", 1));

        Should.Throw<ArgumentException>(() => image.Properties.Add(XisfScalarProperty.Create<int>("test", 2)));
    }

    [TestMethod]
    public void Create_InvalidPropertyId_ThrowsException()
    {
        // Test invalid characters in property IDs
        var image = new XisfImage([1, 2, 3], 3, 1, 1, SampleFormat.UInt8);
        Should.Throw<ArgumentException>(() => image.Properties.Add(XisfScalarProperty.Create<int>("test id with spaces", 1)));
    }

    [TestMethod]
    public void CreateMatrix_MismatchedDimensions_ThrowsException()
    {
        // 3 elements, 2x3=6 expected
        Should.Throw<ArgumentException>(() => XisfMatrixProperty.Create<int>("test", [1, 2, 3], 2, 3));
    }

    [TestMethod]
    public void CreateVector_NullArray_ThrowsException()
    {
        Should.Throw<ArgumentException>(() => XisfVectorProperty.Create<int>("test", null!));
    }

    [TestMethod]
    public void CreateMatrix_NullArray_ThrowsException()
    {
        Should.Throw<ArgumentException>(() => XisfMatrixProperty.Create<int>("test", null!, 0, 0));
    }

    [TestMethod]
    public async Task ReadMetadata_DuplicatePropertyId_OnlyLastPropertyKept()
    {
        List<string> properties = [
            """<Property id="Property1" type="String">Property 1 content</Property>""",
            """<Property id="Property1" type="String">Property 2 content</Property>""",
        ];
        using var stream = TestHelpers.CreateEmptyXisfStream(properties);
        using var reader = new XisfReader(stream);
        
        await reader.ReadAsync();

        reader.Properties.Count.ShouldBe(1);
        reader.Properties[0].Value.ShouldBe("Property 2 content");
    }

    [TestMethod]
    public async Task ReadImage_DuplicatePropertyId_OnlyLastPropertyKept()
    {
        List<string> properties = [
            """<Property id="Property1" type="String">Property 1 content</Property>""",
            """<Property id="Property1" type="String">Property 2 content</Property>""",
        ];
        using var stream = TestHelpers.CreateXisfStreamWith40x30Image(properties);
        using var reader = new XisfReader(stream);

        await reader.ReadAsync();
        var image = await reader.ReadImageAsync(0);

        image.Properties.Count.ShouldBe(1);
        image.Properties[0].Value.ShouldBe("Property 2 content");
    }
}
