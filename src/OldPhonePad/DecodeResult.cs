using System;

namespace OldPhonePad;

public readonly struct DecodeResult : IEquatable<DecodeResult>
{
    private DecodeResult(bool isSuccess, string value, DecodeErrorKind errorKind, string? errorMessage, int errorIndex)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorKind = errorKind;
        ErrorMessage = errorMessage;
        ErrorIndex = errorIndex;
    }

    public bool IsSuccess { get; }

    public string Value { get; }

    public DecodeErrorKind ErrorKind { get; }

    public string? ErrorMessage { get; }

    public int ErrorIndex { get; }

    public static DecodeResult Success(string value) =>
        new(isSuccess: true, value, DecodeErrorKind.None, errorMessage: null, errorIndex: -1);

    public static DecodeResult Failure(DecodeErrorKind kind, string message, int index = -1) =>
        new(isSuccess: false, string.Empty, kind, message, index);

    public bool Equals(DecodeResult other) =>
        IsSuccess == other.IsSuccess
        && string.Equals(Value, other.Value, StringComparison.Ordinal)
        && ErrorKind == other.ErrorKind
        && string.Equals(ErrorMessage, other.ErrorMessage, StringComparison.Ordinal)
        && ErrorIndex == other.ErrorIndex;

    public override bool Equals(object? obj) => obj is DecodeResult other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + IsSuccess.GetHashCode();
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Value);
            hash = (hash * 31) + (int)ErrorKind;
            hash = (hash * 31) + ErrorIndex;
            return hash;
        }
    }

    public static bool operator ==(DecodeResult left, DecodeResult right) => left.Equals(right);

    public static bool operator !=(DecodeResult left, DecodeResult right) => !left.Equals(right);

    public override string ToString() =>
        IsSuccess ? $"Success(\"{Value}\")" : $"Failure({ErrorKind}: {ErrorMessage})";
}
