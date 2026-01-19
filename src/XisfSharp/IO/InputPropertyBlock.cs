using XisfSharp.Properties;

namespace XisfSharp.IO;

internal class InputPropertyBlock : InputBlock
{
    public string Id { get; set; } = string.Empty;

    public XisfPropertyType Type { get; set; }

    public int[] Dimensions { get; set; } = [];

    public object? Value { get; set; }
}
