namespace XisfSharp.Properties;

public static class XisfPropertyExtensions
{
    public static int GetBytesPerElement(this XisfVectorProperty property)
        => XisfUtil.GetBytesPerElement(property.Type);

    public static int GetBytesPerElement(this XisfMatrixProperty property)
        => XisfUtil.GetBytesPerElement(property.Type);
}