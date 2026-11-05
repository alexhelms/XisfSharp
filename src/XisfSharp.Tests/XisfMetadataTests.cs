using System.Xml.Linq;
using XisfSharp.Properties;

namespace XisfSharp.Tests;

[TestClass]
public class XisfMetadataTests
{
    private static readonly string Module = $"XisfSharp {GitVersionInformation.MajorMinorPatch}";

    private static async Task<(XisfReader Reader, string Xml)> Save(XisfWriterOptions options, params XisfProperty[] properties)
    {
        var output = new MemoryStream();
        string xml;

        await using (var writer = new XisfWriter(output, options, leaveOpen: true))
        {
            writer.AddImage(new XisfImage(new ushort[4], 2, 2));
            writer.AddProperties(properties);
            await writer.SaveAsync();
            xml = writer.XmlText;
        }

        output.Position = 0;
        var reader = new XisfReader(output) { ThrowOnParsingFailure = true };
        await reader.ReadHeaderAsync(CancellationToken.None);
        return (reader, xml);
    }

    private static string? Text(XisfReader reader, string id) =>
        reader.Properties.TryGetProperty(id, out var property) ? ((XisfStringProperty)property!).StringValue : null;

    private static int Count(string xml, string id)
    {
        XNamespace ns = XisfBlockTestData.Namespace;
        return XDocument.Parse(xml).Root!.Element(ns + "Metadata")!.Elements(ns + "Property")
            .Count(element => element.Attribute("id")?.Value == id);
    }

    [TestMethod]
    public async Task CreatorApplication_IsTheOneGiven_AndTheModuleIsXisfSharp()
    {
        var (reader, _) = await Save(new XisfWriterOptions { CreatorApplication = "Proxima 0.4.0" });
        await using var opened = reader;

        Text(reader, "XISF:CreatorApplication").ShouldBe("Proxima 0.4.0");
        Text(reader, "XISF:CreatorModule").ShouldBe(Module);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public async Task CreatorApplication_WhenNoneIsGiven_IsXisfSharp(string? application)
    {
        var (reader, _) = await Save(new XisfWriterOptions { CreatorApplication = application });
        await using var opened = reader;

        Text(reader, "XISF:CreatorApplication").ShouldBe(Module);
    }

    [TestMethod]
    public async Task CreatorOS_NamesThisSystem()
    {
        var (reader, _) = await Save(new XisfWriterOptions());
        await using var opened = reader;

        var expected = OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsMacOS() ? "macOS" : "Linux";
        Text(reader, "XISF:CreatorOS").ShouldBe(expected);
    }

    [TestMethod]
    public async Task WriterProperties_GivenByTheCaller_AreIgnored_AndOthersAreKept()
    {
        var (reader, xml) = await Save(new XisfWriterOptions { CreatorApplication = "Proxima 0.4.0" },
            new XisfStringProperty("XISF:CreatorApplication", "PixInsight 1.9.6"),
            new XisfStringProperty("XISF:CreatorModule", "XISF module version 1.0.0"),
            new XisfStringProperty("XISF:CreatorOS", "FreeBSD"),
            new XisfTimePointProperty("XISF:CreationTime", new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            new XisfStringProperty("Observation:Object:Name", "M51"));
        await using var opened = reader;

        Text(reader, "XISF:CreatorApplication").ShouldBe("Proxima 0.4.0");
        Text(reader, "XISF:CreatorModule").ShouldBe(Module);
        Text(reader, "Observation:Object:Name").ShouldBe("M51");
        foreach (var id in new[] { "XISF:CreationTime", "XISF:CreatorApplication", "XISF:CreatorModule", "XISF:CreatorOS" })
            Count(xml, id).ShouldBe(1);
        ((XisfTimePointProperty)reader.Properties["XISF:CreationTime"]!).TimePointValue.Year.ShouldBeGreaterThan(2000);
    }

    [TestMethod]
    public async Task StreamedImages_WriteTheSameMetadata()
    {
        using var output = new MemoryStream();
        await using (var writer = new XisfWriter(output, new XisfWriterOptions { CreatorApplication = "Proxima 0.4.0" }, leaveOpen: true))
        {
            writer.AddImage<float>(new XisfImage(3, 2, 1), (_, _, row) => row.Fill(0.5f));
            await writer.SaveAsync();
        }

        output.Position = 0;
        await using var reader = new XisfReader(output) { ThrowOnParsingFailure = true };
        await reader.ReadHeaderAsync(CancellationToken.None);

        Text(reader, "XISF:CreatorApplication").ShouldBe("Proxima 0.4.0");
        Text(reader, "XISF:CreatorModule").ShouldBe(Module);
    }
}
