namespace XisfSharp.IO;

internal class ImageInputBlock : InputBlock
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int Channels { get; set; }

    public ColorSpace ColorSpace { get; set; }

    public PixelStorage PixelStorage { get; set; }
}
