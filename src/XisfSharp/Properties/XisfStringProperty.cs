using System.Diagnostics;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public class XisfStringProperty : XisfProperty
{
    public string StringValue { get; }

    public override object Value => StringValue;

    public override string ToString() => StringValue;

    public XisfStringProperty(string id, string value)
        : this(id, value, null, null)
    {
    }

    public XisfStringProperty(string id, string value, string? comment)
        : this(id, value, comment, null)
    {
    }

    public XisfStringProperty(string id, string value, string? comment, string? format)
        : base(id, XisfPropertyType.String, comment, format)
    {
        StringValue = value;
    }

    public static XisfStringProperty Create(string id, string value)
        => Create(id, value, null, null);

    public static XisfStringProperty Create(string id, string value, string? comment)
        => Create(id, value, comment, null);

    public static XisfStringProperty Create(string id, string value, string? comment, string? format)
    {
        return new XisfStringProperty(id, value, comment, format);
    }
}