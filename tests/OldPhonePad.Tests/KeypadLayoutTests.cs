namespace OldPhonePad.Tests;

public class KeypadLayoutTests
{
    [Fact]
    public void Constructor_Throws_WhenButtonsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => new KeypadLayout(null!));
    }

    [Fact]
    public void Constructor_Throws_WhenNoButtonsAreDefined()
    {
        Assert.Throws<ArgumentException>(() => new KeypadLayout(new Dictionary<char, string>()));
    }

    [Theory]
    [InlineData('2', "")]
    [InlineData('5', null)]
    public void Constructor_Throws_WhenAButtonMapsToNoCharacters(char button, string? characters)
    {
        var buttons = new Dictionary<char, string> { [button] = characters! };

        Assert.Throws<ArgumentException>(() => new KeypadLayout(buttons));
    }

    [Theory]
    [InlineData('#')]
    [InlineData('*')]
    [InlineData(' ')]
    [InlineData('\t')]
    public void Constructor_Throws_WhenAReservedKeyIsAssignedCharacters(char reserved)
    {
        var buttons = new Dictionary<char, string> { [reserved] = "ABC" };

        Assert.Throws<ArgumentException>(() => new KeypadLayout(buttons));
    }

    [Fact]
    public void Constructor_CopiesTheDictionary_SoLaterMutationsDoNotLeakIn()
    {
        var buttons = new Dictionary<char, string> { ['2'] = "AB" };
        var layout = new KeypadLayout(buttons);

        buttons['3'] = "CD";

        Assert.False(layout.IsButton('3'));
    }

    [Fact]
    public void Buttons_ExposesTheLayoutForUiRendering()
    {
        Assert.Equal(10, KeypadLayout.Standard.Buttons.Count);
        Assert.Equal("PQRS", KeypadLayout.Standard.Buttons['7']);
    }

    [Theory]
    [InlineData('0')]
    [InlineData('1')]
    [InlineData('5')]
    [InlineData('9')]
    public void IsButton_ReturnsTrue_ForEveryKeyOnTheStandardLayout(char button)
    {
        Assert.True(KeypadLayout.Standard.IsButton(button));
    }

    [Theory]
    [InlineData('#')]
    [InlineData('*')]
    [InlineData(' ')]
    [InlineData('A')]
    public void IsButton_ReturnsFalse_ForNonButtonCharacters(char character)
    {
        Assert.False(KeypadLayout.Standard.IsButton(character));
    }

    [Theory]
    [InlineData('2', 1, 'A')]
    [InlineData('2', 2, 'B')]
    [InlineData('2', 3, 'C')]
    [InlineData('2', 4, 'A')]
    [InlineData('7', 4, 'S')]
    [InlineData('7', 5, 'P')]
    [InlineData('0', 1, ' ')]
    [InlineData('0', 7, ' ')]
    public void Resolve_MapsPressCountToTheCorrectCharacter(char button, int pressCount, char expected)
    {
        Assert.Equal(expected, KeypadLayout.Standard.Resolve(button, pressCount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolve_Throws_WhenPressCountIsLessThanOne(int pressCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => KeypadLayout.Standard.Resolve('2', pressCount));
    }

    [Fact]
    public void Resolve_Throws_ArgumentException_ForAButtonNotInTheLayout()
    {
        Assert.Throws<ArgumentException>(() => KeypadLayout.Standard.Resolve('!', 1));
    }

    [Fact]
    public void TryResolve_ReturnsFalseInsteadOfThrowing()
    {
        Assert.False(KeypadLayout.Standard.TryResolve('!', 1, out char missing));
        Assert.Equal('\0', missing);

        Assert.False(KeypadLayout.Standard.TryResolve('2', 0, out _));

        Assert.True(KeypadLayout.Standard.TryResolve('2', 2, out char found));
        Assert.Equal('B', found);
    }
}

public class DecodeResultTests
{
    [Fact]
    public void Success_CarriesTheValueAndNoError()
    {
        DecodeResult result = DecodeResult.Success("HELLO");

        Assert.True(result.IsSuccess);
        Assert.Equal("HELLO", result.Value);
        Assert.Equal(DecodeErrorKind.None, result.ErrorKind);
        Assert.Equal(-1, result.ErrorIndex);
    }

    [Fact]
    public void Failure_CarriesTheErrorAndAnEmptyValue()
    {
        DecodeResult result = DecodeResult.Failure(DecodeErrorKind.MissingSendKey, "nope");

        Assert.False(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
        Assert.Equal("nope", result.ErrorMessage);
    }

    [Fact]
    public void EqualityComparesByValue()
    {
        Assert.Equal(DecodeResult.Success("A"), DecodeResult.Success("A"));
        Assert.NotEqual(DecodeResult.Success("A"), DecodeResult.Success("B"));
        Assert.True(DecodeResult.Success("A") == DecodeResult.Success("A"));
        Assert.True(DecodeResult.Success("A") != DecodeResult.Failure(DecodeErrorKind.MissingSendKey, "x"));
    }

    [Fact]
    public void EqualResultsShareAHashCode()
    {
        Assert.Equal(
            DecodeResult.Success("HELLO").GetHashCode(),
            DecodeResult.Success("HELLO").GetHashCode());
    }
}
