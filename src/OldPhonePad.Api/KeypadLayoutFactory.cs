using Microsoft.Extensions.Options;

namespace OldPhonePad.Api;

public sealed class KeypadLayoutFactory(IOptions<DecodeLimits> limits)
{
    private readonly DecodeLimits _limits = limits.Value;

    public bool TryCreate(
        IReadOnlyDictionary<string, string>? source,
        out KeypadLayout? layout,
        out string? error)
    {
        layout = null;
        error = null;

        if (source is null || source.Count == 0)
        {
            layout = KeypadLayout.Standard;
            return true;
        }

        if (source.Count > _limits.MaxCustomLayoutButtons)
        {
            error = $"A custom layout may define at most {_limits.MaxCustomLayoutButtons} buttons.";
            return false;
        }

        var buttons = new Dictionary<char, string>(source.Count);

        foreach ((string key, string characters) in source)
        {
            if (key.Length != 1)
            {
                error = $"Layout key '{key}' must be exactly one character.";
                return false;
            }

            if (string.IsNullOrEmpty(characters))
            {
                error = $"Button '{key}' must map to at least one character.";
                return false;
            }

            buttons[key[0]] = characters;
        }

        try
        {
            layout = new KeypadLayout(buttons);
            return true;
        }
        catch (ArgumentException ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
