# XisfSharp

[![NuGet](https://img.shields.io/nuget/v/Stylet.svg)](https://www.nuget.org/packages/XisfSharp/)

A .NET library for reading and writing XISF (Extensible Image Serialization Format) 
files, commonly used in astrophotography applications like PixInsight and N.I.N.A.

## Features

- Read and write XISF 1.0 files
- Support for multiple image formats (UInt8, UInt16, Float32, Float64)
- Multiple compression codecs (zlib, LZ4, LZ4HC, Zstd)
- Byte shuffling for improved compression
- Checksum verification (SHA-1, SHA-256, SHA-512, SHA3-256, SHA3-512)
- FITS keyword support
- Image metadata (Color Filter Array, RGB Working Space, Display Function, Resolution)
- Global and per-image properties
- Thumbnail support

## Requirements

- .NET 8.0, 10.0, or higher
- Platforms: Windows, Linux, MacOS
- Architectures: x64 and ARM64

## Installation 

```
dotnet add package XisfSharp
```

## Quickstart

### Reading an Image

```csharp
using XisfSharp;

// Simple load
var image = await XisfImage.LoadAsync("image.xisf");

// Using XisfReader for more control
using var reader = new XisfReader("image.xisf");
await reader.ReadHeaderAsync();
var image = await reader.ReadImageAsync(0);

// Read all images
var images = await reader.ReadAllImagesAsync();

// Enumerate images
await foreach (var img in reader.ReadImagesAsync())
{
    // Process each image
}
```

### Writing an Image

```csharp
// Create image from data
var data = new ushort[1920 * 1080];
var image = new XisfImage(data, 1920, 1080);

// Simple save
await image.SaveAsync("output.xisf");

// Save with compression
var options = new XisfWriterOptions
{
    CompressionCodec = CompressionCodec.Zstd,
    ShuffleBytes = true,
    ChecksumAlgorithm = ChecksumAlgorithm.SHA256
};
await image.SaveAsync("output.xisf", options);
```

### Multi-Channel Images

```csharp
// Create RGB image
var width = 1920;
var height = 1080;
var red = new ushort[width * height];
var green = new ushort[width * height];
var blue = new ushort[width * height];

var image = new XisfImage(width, height, 3);
image.SetData(width, height, red, green, blue);
image.ColorSpace = ColorSpace.RGB;
```

### Writing Properties

```csharp
// Add global properties
using var writer = new XisfWriter("output.xisf");
writer.AddProperty(new XisfStringProperty("Filter", "Ha"));
writer.AddProperty(new XisfScalarProperty("Exposure", XisfPropertyType.Float64, 300.0));
writer.AddProperty(XisfScalarProperty.Create<int>("Gain", 60));

// Add image properties
image.Properties.Add(new XisfTimePointProperty("DateTime", DateTimeOffset.UtcNow));
image.Properties.Add(XisfScalarProperty.Create<double>("Temperature", -10.5));

// Add vector property
var histogram = new int[256];
image.Properties.Add(XisfVectorProperty.Create("Histogram", histogram));

// Add matrix property
var matrix1 = new double[3, 3];
var matrix2 = new double[16];
image.Properties.Add(XisfMatrixProperty.Create("Matrix1", matrix1));
image.Properties.Add(XisfMatrixProperty.Create("Matrix2", matrix2, 4, 4));

writer.AddImage(image);
await writer.SaveAsync();
```

### Reading Properties

```csharp
using var reader = new XisfReader("image.xisf");
await reader.ReadHeaderAsync();

// Global properties
if (reader.Properties.TryGetProperty("Exposure", out var exposure))
{
    var exposureValue = ((XisfScalarProperty)exposure).ScalarValue;
}

// Image properties
var image = await reader.ReadImageAsync(0);
if (image.Properties.TryGetProperty("Temperature", out var temp))
{
    var temperature = ((XisfScalarProperty)temp).ScalarValue;
}
```

### FITS Keywords

```csharp
// Add FITS keywords
image.FITSKeywords.Add(new FITSKeyword("EXPTIME", "300.0", "Exposure time in seconds"));
image.FITSKeywords.Add(new FITSKeyword("FILTER", "Ha", "Filter name"));

// Read FITS keywords
foreach (var keyword in image.FITSKeywords)
{
    Console.WriteLine($"{keyword.Name} = {keyword.Value} / {keyword.Comment}");
}
```

### Common XISF Property Namespaces
```csharp
var dec = XisfScalarProperty.Create<double>(XisfNamespace.Observation.Center.Dec, 47.2661464);
        var ra = XisfScalarProperty.Create<double>(XisfNamespace.Observation.Center.RA, 195.4997911);
        var name = XisfStringProperty.Create(XisfNamespace.Observation.Object.Name, "NGC 1234");
```

### Image Metadata

```csharp
// Set image metadata
image.ImageType = ImageType.Light;
image.Offset = 100.0f;
image.ColorFilterArray = new ColorFilterArray("RGGB", 2, 2, "Bayer RGGB");
image.Resolution = new Resolution(72, 72, ResolutionUnit.Inch);
image.Orientation = ImageOrientation.Rotate90;
```

### Fluent API for Writing

```csharp
var data = new ushort[1920 * 1080];
var image = new XisfImage(data, 1920, 1080);

await using var writer = new XisfWriter("output.xisf");
await writer
    .WithImage(image)
    .WithProperties([
        new XisfStringProperty("Observation:Object", "M31"),
        new XisfScalarProperty("Observation:Exposure", 300.0),
        new XisfStringProperty("Instrument:Telescope", "My Telescope")
    ])
    .SaveAsync();
```

### Accessing Pixel Data

```csharp
// Get raw bytes
ReadOnlyMemory<byte> rawData = image.Data;

// Cast to specific type
var pixels = MemoryMarshal.Cast<byte, ushort>(image.Data.Span);

// Modify pixel data
var newData = new float[width * height];
image.SetData(newData);
```

### Set Image Data Without Extra Allocation
```csharp
// Existing image data
ushort[] existingData = new ushort[1920 * 1080];

// Create image and copy data without extra allocation
var image = new XisfImage();
image.SetData<ushort>(1920, 1080, 1, buffer =>
{
    existingData.AsSpan().CopyTo(buffer);
});
```

## Limitations

XisfSharp does not implement the complete XISF 1.0 specification.
The following XISF features are currently not supported:

- 2GiB max size for a single image
  - C# array length limitation because data is stored as a `byte[]`.
  - Future version of XisfSharp to explore memory mapped files, or other techniques.
- Any Complex types
- 128-bit types
- Table Properties
- Byte order (endianness)
- `ICCProfile`
- `Reference`
- Distributed XISF units

## AI Disclosure

Github Copilot with Claude Sonnet 4.5 was used for generating tests, comments, and general advising.
All code and comments produced by AI tools was manually reviewed.