using System.Diagnostics;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public class XisfTimePointProperty : XisfProperty
{
    public DateTimeOffset TimePointValue { get; }

    public override object Value => TimePointValue;

    public override string ToString() => TimePointValue.ToString("O");

    public XisfTimePointProperty(string id, DateTimeOffset value)
        : this(id, value, null, null)
    {
    }

    public XisfTimePointProperty(string id, DateTimeOffset value, string? comment)
        : this(id, value, comment, null)
    {
    }

    public XisfTimePointProperty(string id, DateTimeOffset value, string? comment, string? format)
        : base(id, XisfPropertyType.TimePoint, comment, format)
    {
        TimePointValue = value;
    }

    public static XisfTimePointProperty Create(string id, DateTimeOffset value)
        => Create(id, value, null, null);

    public static XisfTimePointProperty Create(string id, DateTimeOffset value, string? comment)
        => Create(id, value, comment, null);

    public static XisfTimePointProperty Create(string id, DateTimeOffset value, string? comment, string? format)
    {
        return new XisfTimePointProperty(id, value, comment, format);
    }
}