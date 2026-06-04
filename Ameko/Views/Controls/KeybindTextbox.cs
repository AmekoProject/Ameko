// SPDX-License-Identifier: GPL-3.0-only

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Ameko.Views.Controls;

public class KeybindTextbox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    // Store the current modifier for macOS
    private KeyModifiers _modifiers = KeyModifiers.None;

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        _modifiers = e.KeyModifiers;

        Text = new KeyGesture(e.Key, e.KeyModifiers).ToString();
        _modifiers = KeyModifiers.None;
    }

    /// <summary>
    /// Handle the TextInput event if we're on a Mac
    /// </summary>
    /// <param name="e">TextInput event arguments</param>
    /// <remarks>
    /// This is needed because macOS doesn't send KeyDown events for normal keys.
    /// "A", for example, will not fire OnKeyDown, but "Cmd" or "Cmd+A" will.
    /// Thus, we need to manually construct the input on Macs if we're to allow
    /// non-modified keybinds.
    /// </remarks>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        e.Handled = true;
        if (!OperatingSystem.IsMacOS())
            return;

        var input = e.Text;
        if (string.IsNullOrEmpty(input))
            return;

        // We're going to assume the user is using the num-row here since
        // most Macs don't have a numpad.
        // Unfortunately we don't have a way to distinguish between the two,
        // so if the user wants to bind, e.g. Numpad1 to something, they'll
        // have to edit the JSON file manually. This, of course, doesn't apply
        // if they're using modified keybind like Shift+Numpad1, since that'll
        // be handled by OnKeyDown.
        // Thanks, macOS, very cool!
        if (char.IsDigit(input[0]))
            input = $"D{input[0]}";

        // Next, we need to test for a bunch of options that are under an "OEM"
        // block for some reason. Includes brackets, space, etc.
        input = input[0] switch
        {
            ';' => "OemSemicolon",
            '=' => "OemPlus",
            '-' => "OemMinus",
            '/' => "OemQuestion",
            '`' => "OemTilde",
            '[' => "OemOpenBrackets",
            '\\' => "OemPipe",
            ']' => "OemCloseBrackets",
            '\'' => "OemQuotes",
            ' ' => "Space",
            _ => input,
        };

        var key = KeyGesture.Parse(input);
        if (key.Key == Key.None)
            return;

        Text = new KeyGesture(key.Key, _modifiers).ToString();
        _modifiers = KeyModifiers.None;
    }

    /// <inheritdoc />
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _modifiers = KeyModifiers.None;
    }

    /// <inheritdoc />
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _modifiers = KeyModifiers.None;
    }
}
