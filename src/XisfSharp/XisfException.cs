namespace XisfSharp;

public class XisfException : Exception
{

    public XisfException(string? message)
        : base(message)
    {
    }

    public XisfException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
