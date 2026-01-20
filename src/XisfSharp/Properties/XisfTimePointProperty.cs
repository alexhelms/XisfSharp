using System.Diagnostics;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public class XisfTimePointProperty : XisfProperty
{
    /// <summary>
    /// Gets the time point value of the property.
    /// </summary>
    public DateTimeOffset TimePointValue { get; }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public override object Value => TimePointValue;

    public override string ToString() => TimePointValue.ToString("O");

    /// <summary>
    /// Create a new <see cref="XisfTimePointProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
    public XisfTimePointProperty(string id, DateTimeOffset value)
        : this(id, value, null, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfTimePointProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public XisfTimePointProperty(string id, DateTimeOffset value, string? comment)
        : this(id, value, comment, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfTimePointProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
    public XisfTimePointProperty(string id, DateTimeOffset value, string? comment, string? format)
        : base(id, XisfPropertyType.TimePoint, comment, format)
    {
        TimePointValue = value;
    }

    /// <summary>
    /// Creates a new <see cref="XisfTimePointProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
    public static XisfTimePointProperty Create(string id, DateTimeOffset value)
        => Create(id, value, null, null);

    /// <summary>
    /// Creates a new <see cref="XisfTimePointProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public static XisfTimePointProperty Create(string id, DateTimeOffset value, string? comment)
        => Create(id, value, comment, null);

    /// <summary>
    /// Creates a new <see cref="XisfTimePointProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The <see cref="DateTimeOffset"/> value.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
    public static XisfTimePointProperty Create(string id, DateTimeOffset value, string? comment, string? format)
    {
        return new XisfTimePointProperty(id, value, comment, format);
    }
}