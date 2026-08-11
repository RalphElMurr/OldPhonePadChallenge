using System.ComponentModel.DataAnnotations;

namespace OldPhonePad.Api;

public sealed class DecodeLimits
{
    public const string SectionName = "Decode";

    [Range(1, 1_000_000)]
    public int MaxInputLength { get; init; } = 10_000;

    [Range(1, 10_000)]
    public int MaxBatchSize { get; init; } = 100;

    [Range(1, 1_000)]
    public int MaxCustomLayoutButtons { get; init; } = 64;
}
