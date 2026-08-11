using System;
using System.Text;

namespace OldPhonePad;

public class OldPhonePadDecoder
{
    private readonly KeypadLayout _layout;

    public OldPhonePadDecoder(KeypadLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        _layout = layout;
    }

    public static OldPhonePadDecoder Default { get; } = new(KeypadLayout.Standard);

    public KeypadLayout Layout => _layout;

    public string Decode(string input)
    {
        DecodeResult result = TryDecode(input);

        if (!result.IsSuccess)
        {
            throw new OldPhonePadFormatException(result.ErrorKind, result.ErrorMessage!, result.ErrorIndex);
        }

        return result.Value;
    }

    public DecodeResult TryDecode(string input)
    {
        //in case input is empty
        ArgumentNullException.ThrowIfNull(input);

        //stringbuilder so we can change it (strings cant be changed)
        var output = new StringBuilder(Math.Max(4, input.Length / 2));

        //the button pressed
        char pendingButton = '\0';
        //how many times we pressed it
        int pressCount = 0;

        //looping over the entire input
        for (int i = 0; i < input.Length; i++)
        {
            char character = input[i];

            if (character == KeypadKeys.Send)
            {
                CommitPending(output, _layout, ref pendingButton, ref pressCount);
                return DecodeResult.Success(output.ToString());
            }

            if (_layout.IsButton(character))
            {
                if (character == pendingButton)
                {
                    pressCount++;
                }
                else
                {
                    CommitPending(output, _layout, ref pendingButton, ref pressCount);
                    pendingButton = character;
                    pressCount = 1;
                }
            }
            else if (char.IsWhiteSpace(character))
            {
                CommitPending(output, _layout, ref pendingButton, ref pressCount);
            }
            else if (character == KeypadKeys.Backspace)
            {
                CommitPending(output, _layout, ref pendingButton, ref pressCount);

                if (output.Length > 0)
                {
                    output.Length--;
                }
            }
            else
            {
                return DecodeResult.Failure(
                    DecodeErrorKind.UnsupportedCharacter,
                    $"Unexpected character '{character}' at {i}. Expected a keypad button, ");
            }
        }

        return DecodeResult.Failure(
            DecodeErrorKind.MissingSendKey,
            $"Input must befinished with the send key '{KeypadKeys.Send}'.");
    }

    private static void CommitPending(
        StringBuilder output,
        KeypadLayout layout,
        ref char pendingButton,
        ref int pressCount)
    {
        if (pressCount == 0)
        {
            return;
        }

        output.Append(layout.Resolve(pendingButton, pressCount));
        pendingButton = '\0';
        pressCount = 0;
    }
}
