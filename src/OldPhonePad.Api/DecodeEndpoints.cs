using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace OldPhonePad.Api;

internal static class DecodeEndpoints
{
    internal static RouteGroupBuilder MapDecodeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/decode", DecodeOne)
            .WithName("Decode")
            .WithSummary("Decode a keypad sequence")
            .WithDescription(
                "Converts multi-tap keypad input into text. The sequence must end with '#'. "
                + "Supply an optional custom layout to decode against a non-standard keypad.");

        group.MapPost("/decode/batch", DecodeBatch)
            .WithName("DecodeBatch")
            .WithSummary("Decode many keypad sequences in one call")
            .WithDescription(
                "Decodes up to the configured batch limit in a single round trip. "
                + "Individual sequences may fail without failing the whole request; "
                + "inspect the 'succeeded' flag on each result.");

        return group;
    }

    private static Results<Ok<DecodeResponse>, ValidationProblem> DecodeOne(
        DecodeRequest request,
        KeypadLayoutFactory layoutFactory,
        IOptions<DecodeLimits> limits,
        ILogger<Program> logger)
    {
        if (request.Input is null)
        {
            return Problem("input", "An input sequence is required.");
        }

        if (request.Input.Length > limits.Value.MaxInputLength)
        {
            return Problem(
                "input",
                $"Input exceeds the maximum of {limits.Value.MaxInputLength} characters.");
        }

        if (!layoutFactory.TryCreate(request.Layout, out KeypadLayout? layout, out string? layoutError))
        {
            return Problem("layout", layoutError!);
        }

        var decoder = ReferenceEquals(layout, KeypadLayout.Standard)
            ? OldPhonePadDecoder.Default
            : new OldPhonePadDecoder(layout!);

        DecodeResult result = decoder.TryDecode(request.Input);

        if (!result.IsSuccess)
        {
            logger.LogDebug("Decode rejected: {ErrorKind} at index {Index}", result.ErrorKind, result.ErrorIndex);
            return Problem("input", result.ErrorMessage!);
        }

        return TypedResults.Ok(new DecodeResponse(
            request.Input,
            result.Value,
            CountKeyPresses(request.Input)));
    }

    private static Results<Ok<BatchDecodeResponse>, ValidationProblem> DecodeBatch(
        BatchDecodeRequest request,
        KeypadLayoutFactory layoutFactory,
        IOptions<DecodeLimits> limits)
    {
        if (request.Inputs is null || request.Inputs.Count == 0)
        {
            return Problem("inputs", "At least one input sequence is required.");
        }

        if (request.Inputs.Count > limits.Value.MaxBatchSize)
        {
            return Problem(
                "inputs",
                $"A batch may contain at most {limits.Value.MaxBatchSize} sequences.");
        }

        if (!layoutFactory.TryCreate(request.Layout, out KeypadLayout? layout, out string? layoutError))
        {
            return Problem("layout", layoutError!);
        }

        var decoder = ReferenceEquals(layout, KeypadLayout.Standard)
            ? OldPhonePadDecoder.Default
            : new OldPhonePadDecoder(layout!);

        var results = new List<BatchDecodeItem>(request.Inputs.Count);
        int succeeded = 0;

        foreach (string input in request.Inputs)
        {
            if (input is null || input.Length > limits.Value.MaxInputLength)
            {
                results.Add(new BatchDecodeItem(
                    input ?? string.Empty,
                    Succeeded: false,
                    Output: null,
                    Error: $"Input must be non-null and at most {limits.Value.MaxInputLength} characters.",
                    ErrorIndex: null));
                continue;
            }

            DecodeResult result = decoder.TryDecode(input);

            if (result.IsSuccess)
            {
                succeeded++;
                results.Add(new BatchDecodeItem(input, true, result.Value, null, null));
            }
            else
            {
                results.Add(new BatchDecodeItem(
                    input,
                    false,
                    null,
                    result.ErrorMessage,
                    result.ErrorIndex >= 0 ? result.ErrorIndex : null));
            }
        }

        return TypedResults.Ok(new BatchDecodeResponse(
            results,
            succeeded,
            results.Count - succeeded));
    }

    private static int CountKeyPresses(string input)
    {
        int count = 0;

        foreach (char character in input)
        {
            if (!char.IsWhiteSpace(character))
            {
                count++;
            }
        }

        return count;
    }

    private static ValidationProblem Problem(string field, string message) =>
        TypedResults.ValidationProblem(
            new Dictionary<string, string[]> { [field] = [message] },
            title: "The request could not be decoded.");
}
