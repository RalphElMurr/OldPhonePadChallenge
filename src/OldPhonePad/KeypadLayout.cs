using System;
using System.Collections.Generic;

namespace OldPhonePad;

public sealed class KeypadLayout
{
    //to lookup which button is which letter
    private readonly Dictionary<char, string> _buttons;

    //setting up the buttons to the letters, 
    public static KeypadLayout Standard { get; } = new(new Dictionary<char, string>
    {
        ['1'] = "&'(",
        ['2'] = "ABC",
        ['3'] = "DEF",
        ['4'] = "GHI",
        ['5'] = "JKL",
        ['6'] = "MNO",
        ['7'] = "PQRS",
        ['8'] = "TUV",
        ['9'] = "WXYZ",
        ['0'] = " ",
    });

    public KeypadLayout(IReadOnlyDictionary<char, string> buttons)
    {
        //if no dictionary is passed
        ArgumentNullException.ThrowIfNull(buttons);

        var copy = new Dictionary<char, string>(buttons.Count);

        foreach (KeyValuePair<char, string> pair in buttons)
        {
            char button = pair.Key;
            string letters = pair.Value;

            //if button maps no letters
            if (string.IsNullOrEmpty(letters))
            {
                throw new ArgumentException(
                    $"Button '{button}' must map to at least one character.",
                    nameof(buttons));
            }

            //we cannot map a button reserved for something else with a letter
            if (button == KeypadKeys.Send || button == KeypadKeys.Backspace || char.IsWhiteSpace(button))
            {
                throw new ArgumentException(
                    $"Button '{button}' is a reserved control key and cannot be assigned characters.",
                    nameof(buttons));
            }

            copy[button] = letters;
        }

        if (copy.Count == 0)
        {
            throw new ArgumentException("A layout must define at least one button.", nameof(buttons));
        }

        //to copy the dictionary we got
        _buttons = copy;
    }

    public IReadOnlyDictionary<char, string> Buttons => _buttons;

    //check if button pressed (like the input) is present in the dictionary
    public bool IsButton(char character) => _buttons.ContainsKey(character);

    //to see how many times the user pressed a button
    public char Resolve(char button, int pressCount)
    {
        //in case he didnt press anything
        if (pressCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pressCount), pressCount, "A button must be pressed at least once.");
        }

        //get the letters associated with the pressed button
        if (!_buttons.TryGetValue(button, out string? letters))
        {
            throw new ArgumentException($"'{button}' is not a button in this layout.", nameof(button));
        }

        //for example if he pressed "2" (ABC) 2 times, its (2-1)%3=1 so it is B
        return letters[(pressCount - 1) % letters.Length];
    }

    public bool TryResolve(char button, int pressCount, out char character)
    {
        if (pressCount < 1 || !_buttons.TryGetValue(button, out string? letters))
        {
            character = '\0';
            return false;
        }

        character = letters[(pressCount - 1) % letters.Length];
        return true;
    }
}
