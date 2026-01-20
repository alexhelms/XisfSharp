using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfWriterTests
{
    [TestMethod]
    public void WithImage_AddsPropertiesToImage()
    {
        using var stream = new MemoryStream();
        using var writer = new XisfWriter(stream, leaveOpen: true);
        var image = new XisfImage();
        var properties = new[]
        {
            new XisfStringProperty("Test:Property1", "Value1"),
            new XisfStringProperty("Test:Property2", "Value2")
        };

        writer.WithImage(image, properties);

        image.Properties.Count.ShouldBe(2);
        image.Properties.ContainsId("Test:Property1").ShouldBeTrue();
        image.Properties.ContainsId("Test:Property2").ShouldBeTrue();
    }

    [TestMethod]
    public void WithImage_IgnoresDuplicateProperties()
    {
        using var stream = new MemoryStream();
        using var writer = new XisfWriter(stream, leaveOpen: true);
        var image = new XisfImage();

        var existingProperty = new XisfStringProperty("Test:Duplicate", "OriginalValue");
        image.Properties.Add(existingProperty);

        var duplicateProperty = new XisfStringProperty("Test:Duplicate", "NewValue");
        var newProperty = new XisfStringProperty("Test:New", "NewValue");

        writer.WithImage(image, [duplicateProperty, newProperty]);

        image.Properties.Count.ShouldBe(2);
        var property = image.Properties.First(p => p.Id == "Test:Duplicate");
        ((XisfStringProperty)property).StringValue.ShouldBe("OriginalValue");
        image.Properties.ContainsId("Test:New").ShouldBeTrue();
    }

    [TestMethod]
    public void WithImage_ReturnsWriterForFluentChaining()
    {
        using var stream = new MemoryStream();
        using var writer = new XisfWriter(stream, leaveOpen: true);
        var image = new XisfImage();
        var properties = new[] { new XisfStringProperty("Test:Property", "Value") };

        var result = writer.WithImage(image, properties);

        result.ShouldBeSameAs(writer);
    }

    [TestMethod]
    public void WithImage_HandlesEmptyPropertyCollection()
    {
        using var stream = new MemoryStream();
        using var writer = new XisfWriter(stream, leaveOpen: true);
        var image = new XisfImage();
        var properties = Array.Empty<XisfProperty>();

        writer.WithImage(image, properties);

        image.Properties.Count.ShouldBe(0);
    }

    [TestMethod]
    public void WithImage_HandlesMultipleDuplicatesAndNewProperties()
    {
        using var stream = new MemoryStream();
        using var writer = new XisfWriter(stream, leaveOpen: true);
        var image = new XisfImage();

        image.Properties.Add(new XisfStringProperty("Test:Existing1", "Original1"));
        image.Properties.Add(new XisfStringProperty("Test:Existing2", "Original2"));

        var properties = new XisfProperty[]
        {
            new XisfStringProperty("Test:Existing1", "Duplicate1"),
            new XisfStringProperty("Test:New1", "New1"),
            new XisfStringProperty("Test:Existing2", "Duplicate2"),
            new XisfStringProperty("Test:New2", "New2")
        };

        writer.WithImage(image, properties);

        image.Properties.Count.ShouldBe(4);
        image.Properties.ContainsId("Test:Existing1").ShouldBeTrue();
        image.Properties.ContainsId("Test:Existing2").ShouldBeTrue();
        image.Properties.ContainsId("Test:New1").ShouldBeTrue();
        image.Properties.ContainsId("Test:New2").ShouldBeTrue();

        var prop1 = (XisfStringProperty)image.Properties.First(p => p.Id == "Test:Existing1");
        prop1.StringValue.ShouldBe("Original1");
        var prop2 = (XisfStringProperty)image.Properties.First(p => p.Id == "Test:Existing2");
        prop2.StringValue.ShouldBe("Original2");
    }
}
