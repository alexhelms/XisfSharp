using System.Diagnostics;

namespace XisfSharp.Properties;

[DebuggerDisplay("{Id} = {Value}")]
public class XisfStringProperty : XisfProperty
{
    /// <summary>
    /// Gets the string value of the property.
    /// </summary>
    public string StringValue { get; }

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public override object Value => StringValue;

    public override string ToString() => StringValue;

    /// <summary>
    /// Create a new <see cref="XisfStringProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The string value. Cannot be null.</param>
    public XisfStringProperty(string id, string value)
        : this(id, value, null, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfStringProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The string value. Cannot be null.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public XisfStringProperty(string id, string value, string? comment)
        : this(id, value, comment, null)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfStringProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The string value. Cannot be null.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
    public XisfStringProperty(string id, string value, string? comment, string? format)
        : base(id, XisfPropertyType.String, comment, format)
    {
        ArgumentNullException.ThrowIfNull(value);

        StringValue = value;
    }

    /// <summary>
    /// Creates a new <see cref="XisfStringProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The string value.</param>
    public static XisfStringProperty Create(string id, string value)
        => Create(id, value, null, null);

    /// <summary>
    /// Creates a new <see cref="XisfStringProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The string value.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    public static XisfStringProperty Create(string id, string value, string? comment)
        => Create(id, value, comment, null);

    /// <summary>
    /// Creates a new <see cref="XisfStringProperty"/>.
    /// </summary>
    /// <param name="id">The unique identifier for the property.</param>
    /// <param name="value">The string value.</param>
    /// <param name="comment">An optional comment describing the property.</param>
    /// <param name="format">An optional format hint for the property value.</param>
    public static XisfStringProperty Create(string id, string value, string? comment, string? format)
    {
        return new XisfStringProperty(id, value, comment, format);
    }
}