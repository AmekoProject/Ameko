// SPDX-License-Identifier: GPL-3.0-only

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Ameko.Views.Controls;

public class TimeTextBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <summary>
    /// Text input handler
    /// </summary>
    /// <param name="e">Input event args</param>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Text))
            InsertText(e.Text);

        e.Handled = true;
    }

    /// <summary>
    /// Navigation handler
    /// </summary>
    /// <param name="e">Key event args</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Movement
        switch (e.Key)
        {
            // Left
            case Key.Back:
            case Key.Left:
            case Key.Down:
                CaretIndex -= 1;
                e.Handled = true;
                return;
            // Start
            case Key.Home:
                CaretIndex = 0;
                e.Handled = true;
                return;
            // End
            case Key.End:
                CaretIndex = Text!.Length;
                e.Handled = true;
                return;
            // Right
            case Key.Right:
            case Key.Up:
                CaretIndex += 1;
                e.Handled = true;
                return;
            // Paste
            case Key.V when (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0:
                ClipboardPaste();
                e.Handled = true;
                return;
            // Copy
            case Key.C when (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0:
                ClipboardCopy();
                e.Handled = true;
                return;
            // Select all
            case Key.A when (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0:
                SelectionStart = 0;
                SelectionEnd = Text?.Length ?? 0;
                e.Handled = true;
                return;
        }
    }

    /// <summary>
    /// Try to get the selected text and copy it
    /// </summary>
    private void ClipboardCopy()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            // There is an actual selection
            if (SelectionStart != SelectionEnd)
            {
                clipboard?.SetTextAsync(SelectedText);
                return;
            }

            // Copy the whole box
            clipboard?.SetTextAsync(Text);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    /// <summary>
    /// Try to get the text in the clipboard and paste it
    /// </summary>
    private void ClipboardPaste()
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            var text = clipboard?.TryGetTextAsync().Result;
            if (text is null)
                return;

            InsertText(text);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    /// <summary>
    /// Insert the text
    /// </summary>
    /// <param name="input">Text to insert</param>
    private void InsertText(string input)
    {
        foreach (var c in input)
        {
            // Discard input if at the end of the box
            if (CaretIndex >= Text!.Length)
                return;

            // Discard input if invalid character
            if (c is < '0' or > '9' and not ';' and not '.' and not ',' and not ':')
                continue;

            // Move forward if at punctuation
            var next = Text?[CaretIndex] ?? '-';
            if (next is ':' or '.' or ',')
                CaretIndex += 1;

            // Ignore punctuation keys
            if (c is ';' or '.' or ',' or ':')
                continue;

            // Good input
            Text = Text!.Remove(CaretIndex, 1).Insert(CaretIndex, c.ToString());

            CaretIndex += 1;
        }
    }
}
