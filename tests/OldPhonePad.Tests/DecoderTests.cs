namespace OldPhonePad.Tests;

public class ChallengeBriefTests
{
    [Theory]
    [InlineData("33#", "E")]
    [InlineData("227*#", "B")]
    [InlineData("4433555 555666#", "HELLO")]
    [InlineData("8 88777444666*664#", "TURING")]
    public void OldPhonePad_MatchesTheBriefExamples(string input, string expected)
    {
        Assert.Equal(expected, OldPhone.OldPhonePad(input));
    }

    [Fact]
    public void OldPhonePad_HandlesThePauseExampleFromTheBriefProse()
    {
        Assert.Equal("CAB", OldPhone.OldPhonePad("222 2 22#"));
    }

    [Fact]
    public void OldPhonePad_UsesTheStandardLayout()
    {
        Assert.Equal("A", OldPhone.OldPhonePad("2#"));
    }
}

public class OldPhonePadDecoderTests
{
    private static string Decode(string input) => OldPhonePadDecoder.Default.Decode(input);

    [Theory]
    [InlineData("2#", "A")]
    [InlineData("22#", "B")]
    [InlineData("222#", "C")]
    [InlineData("7777#", "S")]
    [InlineData("9999#", "Z")]
    [InlineData("1#", "&")]
    [InlineData("11#", "'")]
    [InlineData("111#", "(")]
    [InlineData("0#", " ")]
    public void Decode_ResolvesPressCountToCorrectCharacter(string input, string expected)
    {
        Assert.Equal(expected, Decode(input));
    }

    [Theory]
    [InlineData("2222#", "A")]
    [InlineData("22222#", "B")]
    [InlineData("77777#", "P")]
    [InlineData("00000#", " ")]
    public void Decode_WrapsAroundPastTheLastCharacter(string input, string expected)
    {
        Assert.Equal(expected, Decode(input));
    }

    [Fact]
    public void Decode_WrapsCorrectlyAtVeryHighPressCounts()
    {
        Assert.Equal("C", Decode(new string('2', 300) + "#"));
    }

    [Fact]
    public void Decode_TreatsSpaceAsAPauseBetweenCharactersOnTheSameButton()
    {
        Assert.Equal("AA", Decode("2 2#"));
    }

    [Fact]
    public void Decode_TreatsRepeatedPausesAsASinglePause()
    {
        Assert.Equal("BB", Decode("22   22#"));
    }

    [Theory]
    [InlineData("22\t22#")]
    [InlineData("22\n22#")]
    [InlineData("22\r\n22#")]
    public void Decode_TreatsAnyWhitespaceAsAPause(string input)
    {
        Assert.Equal("BB", Decode(input));
    }

    [Fact]
    public void Decode_DoesNotNeedAPauseBetweenDifferentButtons()
    {
        Assert.Equal("AD", Decode("23#"));
    }

    [Fact]
    public void Decode_IgnoresLeadingAndTrailingPauses()
    {
        Assert.Equal("A", Decode("  2  #"));
    }

    [Fact]
    public void Decode_BackspaceRemovesTheLastCommittedCharacter()
    {
        Assert.Equal("A", Decode("23*#"));
    }

    [Fact]
    public void Decode_BackspaceCommitsThePendingButtonBeforeDeletingIt()
    {
        Assert.Equal("", Decode("2*#"));
    }

    [Fact]
    public void Decode_BackspaceOnEmptyOutputIsANoOp()
    {
        Assert.Equal("", Decode("*#"));
        Assert.Equal("A", Decode("***2#"));
    }

    [Fact]
    public void Decode_ConsecutiveBackspacesRemoveMultipleCharacters()
    {
        Assert.Equal("A", Decode("234**#"));
    }

    [Fact]
    public void Decode_BackspaceAlsoActsAsAPause()
    {
        Assert.Equal("B", Decode("22*22#"));
    }

    [Fact]
    public void Decode_ReturnsEmptyStringForSendOnly()
    {
        Assert.Equal("", Decode("#"));
    }

    [Fact]
    public void Decode_StopsAtTheFirstSendAndIgnoresTrailingInput()
    {
        Assert.Equal("A", Decode("2#3333"));
    }

    [Fact]
    public void Decode_IgnoresInvalidCharactersThatAppearAfterSend()
    {
        Assert.Equal("A", Decode("2#!!!"));
    }

    [Fact]
    public void Decode_CommitsThePendingButtonWhenSendIsReached()
    {
        Assert.Equal("C", Decode("222#"));
    }

    [Fact]
    public void Decode_HandlesALongerMixedMessage()
    {
        Assert.Equal("HELLO WORLD", Decode("4433555 555666096667775553#"));
    }

    [Fact]
    public void Decode_HandlesLargeInputWithoutStackOrBufferProblems()
    {
        string input = string.Concat(Enumerable.Repeat("2 ", 5_000)) + "#";

        Assert.Equal(new string('A', 5_000), Decode(input));
    }

    [Fact]
    public void Decode_Throws_WhenInputIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => Decode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("22")]
    [InlineData("4433555 555666")]
    public void Decode_Throws_WhenInputIsNotTerminatedBySend(string input)
    {
        var exception = Assert.Throws<OldPhonePadFormatException>(() => Decode(input));

        Assert.Equal(DecodeErrorKind.MissingSendKey, exception.Kind);
    }

    [Theory]
    [InlineData("2a#")]
    [InlineData("A#")]
    [InlineData("2-2#")]
    [InlineData("+#")]
    public void Decode_Throws_OnUnsupportedCharacters(string input)
    {
        var exception = Assert.Throws<OldPhonePadFormatException>(() => Decode(input));

        Assert.Equal(DecodeErrorKind.UnsupportedCharacter, exception.Kind);
    }

    [Fact]
    public void Decode_StillThrowsPlainFormatException_ForCallersCatchingTheBaseType()
    {
        Assert.Throws<OldPhonePadFormatException>(() => Decode("2a#"));
        Assert.ThrowsAny<FormatException>(() => Decode("2a#"));
    }

    [Fact]
    public void Decode_ExceptionReportsTheOffendingCharacterAndPosition()
    {
        var exception = Assert.Throws<OldPhonePadFormatException>(() => Decode("22a#"));

        Assert.Equal(2, exception.Index);
        Assert.Contains("a", exception.Message, StringComparison.Ordinal);
        Assert.Contains("2", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryDecode_ReportsSuccessWithTheDecodedValue()
    {
        DecodeResult result = OldPhonePadDecoder.Default.TryDecode("4433555 555666#");

        Assert.True(result.IsSuccess);
        Assert.Equal("HELLO", result.Value);
        Assert.Equal(DecodeErrorKind.None, result.ErrorKind);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TryDecode_ReportsUnsupportedCharactersWithoutThrowing()
    {
        DecodeResult result = OldPhonePadDecoder.Default.TryDecode("22a#");

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeErrorKind.UnsupportedCharacter, result.ErrorKind);
        Assert.Equal(2, result.ErrorIndex);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void TryDecode_ReportsAMissingSendKeyWithoutAPosition()
    {
        DecodeResult result = OldPhonePadDecoder.Default.TryDecode("22");

        Assert.False(result.IsSuccess);
        Assert.Equal(DecodeErrorKind.MissingSendKey, result.ErrorKind);
        Assert.Equal(-1, result.ErrorIndex);
    }

    [Fact]
    public void TryDecode_StillThrowsOnNull_BecauseThatIsACallerBugNotBadUserInput()
    {
        Assert.Throws<ArgumentNullException>(() => OldPhonePadDecoder.Default.TryDecode(null!));
    }

    [Fact]
    public void Constructor_Throws_WhenLayoutIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new OldPhonePadDecoder(null!));
    }

    [Fact]
    public void Default_IsReusableAcrossCalls()
    {
        Assert.Equal("E", OldPhonePadDecoder.Default.Decode("33#"));
        Assert.Equal("E", OldPhonePadDecoder.Default.Decode("33#"));
    }

    [Fact]
    public void Default_IsSafeToCallFromManyThreadsAtOnce()
    {
        var inputs = new[]
        {
            ("4433555 555666#", "HELLO"),
            ("8 88777444666*664#", "TURING"),
            ("222 2 22#", "CAB"),
            ("33#", "E"),
        };

        Parallel.For(0, 4_000, i =>
        {
            (string input, string expected) = inputs[i % inputs.Length];
            Assert.Equal(expected, OldPhonePadDecoder.Default.Decode(input));
        });
    }

    [Fact]
    public void Decode_ExposesTheLayoutItWasBuiltWith()
    {
        Assert.Same(KeypadLayout.Standard, OldPhonePadDecoder.Default.Layout);
    }

    [Fact]
    public void Decode_WorksWithACustomLayout()
    {
        var layout = new KeypadLayout(new Dictionary<char, string>
        {
            ['2'] = "XY",
            ['3'] = "Z",
        });

        Assert.Equal("XYZ", new OldPhonePadDecoder(layout).Decode("2 22 3#"));
    }

    [Fact]
    public void Decode_SupportsNonLatinAlphabets()
    {
        var layout = new KeypadLayout(new Dictionary<char, string>
        {
            ['2'] = "ابت",
            ['3'] = "ثجح",
        });

        Assert.Equal("بح", new OldPhonePadDecoder(layout).Decode("22 333#"));
    }

    [Fact]
    public void Decode_Throws_ForButtonsMissingFromACustomLayout()
    {
        var layout = new KeypadLayout(new Dictionary<char, string> { ['2'] = "AB" });

        Assert.Throws<OldPhonePadFormatException>(() => new OldPhonePadDecoder(layout).Decode("3#"));
    }
}
