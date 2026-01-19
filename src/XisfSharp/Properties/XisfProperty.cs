using System.Text.RegularExpressions;

namespace XisfSharp.Properties;

public abstract partial class XisfProperty
{
    [GeneratedRegex(@"[_a-zA-Z][_a-zA-Z0-9]*(:([_a-zA-Z][_a-zA-Z0-9])+)*")]
    private static partial Regex IdRegex();

    public string Id { get; }

    public XisfPropertyType Type { get; }

    public abstract object Value { get; }

    public string? Comment { get; }

    public string? Format { get; }

    protected XisfProperty(string id, XisfPropertyType type, string? comment, string? format)
    {
        ValidateId(id);

        Id = id;
        Type = type;
        Comment = comment;
        Format = format;
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Property Id cannot be null or whitespace.", nameof(id));

        if (id.Contains(' '))
            throw new ArgumentException("Property Id cannot contain whitespace.", nameof(id));

        if (!IdRegex().IsMatch(id))
            throw new ArgumentException("Property Id contains invalid characters. Id can only contain alphanumeric characters, underscore, and colon.", nameof(id));
    }
}