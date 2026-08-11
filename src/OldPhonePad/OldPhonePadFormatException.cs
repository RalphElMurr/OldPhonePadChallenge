using System;

namespace OldPhonePad;

public class OldPhonePadFormatException : FormatException
{
    public OldPhonePadFormatException(string message)
        : this(DecodeErrorKind.UnsupportedCharacter, message, index: -1)
    {
    }

    public OldPhonePadFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
        Kind = DecodeErrorKind.UnsupportedCharacter;
        Index = -1;
    }

    public OldPhonePadFormatException(DecodeErrorKind kind, string message, int index)
        : base(message)
    {
        Kind = kind;
        Index = index;
    }

    public DecodeErrorKind Kind { get; }

    public int Index { get; }
}
