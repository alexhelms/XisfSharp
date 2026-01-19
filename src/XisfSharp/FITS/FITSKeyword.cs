namespace XisfSharp.FITS;

public class FITSKeyword
{
    public string Name { get; }
    public string? Value { get; }
    public string? Comment { get; }

    public FITSKeyword(string name, string? value)
        : this(name, value, null)
    {
    }

    public FITSKeyword(string name, string? value, string? comment)
    {
        ValidateName(name);

        Name = name;
        Value = value;
        Comment = comment;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("FITS keyword name cannot be empty or whitespace.", nameof(name));
        }

        if (name.Length > 8)
        {
            throw new ArgumentException("FITS keyword name cannot exceed 8 characters.", nameof(name));
        }

        foreach (char c in name)
        {
            bool isValid = (c >= '0' && c <= '9') ||
                           (c >= 'A' && c <= 'Z') ||
                           c == '_' ||
                           c == '-';

            if (!isValid)
            {
                throw new ArgumentException(
                    $"FITS keyword name contains invalid character '{c}'. Only uppercase A-Z, digits 0-9, underscore '_', and hyphen '-' are allowed.",
                    nameof(name));
            }
        }
    }
}
