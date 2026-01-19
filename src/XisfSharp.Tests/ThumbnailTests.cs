using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class ThumbnailTests
{
    [TestMethod]
    public async Task ParseThumbnail()
    {
        var filename = TestHelpers.TestImagePath("200x150-u16-zstd-shuffle-process-history-display-function-icc-sha1.xisf");
        using var filestream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var reader = new XisfReader(filestream);
        
        await reader.ReadAsync();
        
        var image = await reader.ReadImageAsync(0);
        image.Thumbnail.ShouldNotBeNull();
        image.Thumbnail.Width.ShouldBe(400);
        image.Thumbnail.Height.ShouldBe(300);
        image.Thumbnail.Data.IsEmpty.ShouldBeTrue();

        var thumbnail = await reader.ReadThumbnailAsync(image);

        thumbnail.ShouldNotBeNull();
        thumbnail.Width.ShouldBe(400);
        thumbnail.Height.ShouldBe(300);
        thumbnail.Data.IsEmpty.ShouldBeFalse();
    }
}
