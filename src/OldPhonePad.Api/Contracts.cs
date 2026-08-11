using System.ComponentModel;

namespace OldPhonePad.Api;

public sealed record DecodeRequest(
    [property: Description("Keypad sequence terminated by '#'. Example: 4433555 555666#")]
    string Input,
    [property: Description("Optional custom keypad, e.g. { \"2\": \"ABC\" }. Omit for the standard layout.")]
    IReadOnlyDictionary<string, string>? Layout = null);

public sealed record DecodeResponse(
    string Input,
    string Output,
    int KeyPressCount);

public sealed record BatchDecodeRequest(
    IReadOnlyList<string> Inputs,
    IReadOnlyDictionary<string, string>? Layout = null);

public sealed record BatchDecodeItem(
    string Input,
    bool Succeeded,
    string? Output,
    string? Error,
    int? ErrorIndex);

public sealed record BatchDecodeResponse(
    IReadOnlyList<BatchDecodeItem> Results,
    int SucceededCount,
    int FailedCount);

public sealed record KeypadButton(string Button, string Characters);

public sealed record KeypadResponse(
    IReadOnlyList<KeypadButton> Buttons,
    string SendKey,
    string BackspaceKey);
