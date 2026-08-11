using Microsoft.AspNetCore.Http.HttpResults;

namespace OldPhonePad.Api;

internal static class KeypadEndpoints
{
    internal static RouteGroupBuilder MapKeypadEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/keypad", GetStandardKeypad)
            .WithName("GetKeypad")
            .WithSummary("Describe the standard keypad")
            .WithDescription(
                "Returns every button and the characters it cycles through, plus the "
                + "reserved send and backspace keys. Use it to build a keypad UI without "
                + "duplicating the layout on the client.")
            .CacheOutput(policy => policy.Expire(TimeSpan.FromHours(1)));

        return group;
    }

    private static Ok<KeypadResponse> GetStandardKeypad()
    {
        List<KeypadButton> buttons = [.. KeypadLayout.Standard.Buttons
            .Select(pair => new KeypadButton(pair.Key.ToString(), pair.Value))
            .OrderBy(button => button.Button, StringComparer.Ordinal)];

        return TypedResults.Ok(new KeypadResponse(
            buttons,
            KeypadKeys.Send.ToString(),
            KeypadKeys.Backspace.ToString()));
    }
}
