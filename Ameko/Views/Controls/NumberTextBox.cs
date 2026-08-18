// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using ReactiveUI.Primitives;

namespace Ameko.Views.Controls;

public class NumberTextBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <summary>
    /// Minimum allowed value
    /// </summary>
    public static readonly StyledProperty<decimal> MinimumProperty = AvaloniaProperty.Register<
        NumberTextBox,
        decimal
    >(nameof(Minimum), defaultValue: int.MinValue);

    /// <summary>
    /// Maximum allowed value
    /// </summary>
    public static readonly StyledProperty<decimal> MaximumProperty = AvaloniaProperty.Register<
        NumberTextBox,
        decimal
    >(nameof(Maximum), defaultValue: int.MaxValue);

    /// <summary>
    /// Amount to increment or decrement by
    /// </summary>
    public static readonly StyledProperty<decimal> IncrementProperty = AvaloniaProperty.Register<
        NumberTextBox,
        decimal
    >(nameof(Increment), defaultValue: 1m);

    /// <summary>
    /// Display format
    /// </summary>
    public static readonly StyledProperty<string> FormatStringProperty = AvaloniaProperty.Register<
        NumberTextBox,
        string
    >(nameof(FormatString), defaultValue: "0");

    /// <summary>
    /// Whether to allow decimal numbers or not
    /// </summary>
    public static readonly StyledProperty<bool> AllowDecimalProperty = AvaloniaProperty.Register<
        NumberTextBox,
        bool
    >(nameof(AllowDecimal), defaultValue: false);

    public static readonly StyledProperty<decimal> ValueProperty = AvaloniaProperty.Register<
        NumberTextBox,
        decimal
    >(nameof(Value), defaultValue: 0m, defaultBindingMode: BindingMode.TwoWay);

    public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent = RoutedEvent.Register<
        NumberTextBox,
        RoutedEventArgs
    >(nameof(ValueChanged), RoutingStrategies.Bubble);

    /// <summary>
    /// Minimum allowed value
    /// </summary>
    public decimal Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// Maximum allowed value
    /// </summary>
    public decimal Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// Amount to increment or decrement by
    /// </summary>
    public decimal Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>
    /// Display format
    /// </summary>
    public string FormatString
    {
        get => GetValue(FormatStringProperty);
        set
        {
            SetValue(FormatStringProperty, value);
            Text = Value.ToString(FormatString, NumberFormatInfo.InvariantInfo);
        }
    }

    /// <summary>
    /// Whether to allow decimal numbers or not
    /// </summary>
    public bool AllowDecimal
    {
        get => GetValue(AllowDecimalProperty);
        set => SetValue(AllowDecimalProperty, value);
    }

    /// <summary>
    /// Current value
    /// </summary>
    public decimal Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event EventHandler<RoutedEventArgs> ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public NumberTextBox()
    {
        Text = Value.ToString(FormatString, NumberFormatInfo.InvariantInfo);
    }

    static NumberTextBox()
    {
        ValueProperty.Changed.Subscribe(OnValueChanged);
    }

    private static void OnValueChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Sender is not NumberTextBox ntb)
            return;
        var newValue = (decimal?)e.NewValue ?? 0;
        var expected = newValue.ToString(ntb.FormatString, NumberFormatInfo.InvariantInfo);
        if (ntb.Text != expected)
        {
            ntb.Text = expected;
        }
        ntb.RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
    }

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
            // Backspace, Delete
            case Key.Back:
            case Key.Delete:
                base.OnKeyDown(e);
                if (Text?.Length == 0)
                {
                    Value = 0;
                    Text = Value.ToString(FormatString, NumberFormatInfo.InvariantInfo);
                    CaretIndex = Text.Length;
                }
                RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
                return;
            // Left
            case Key.Left:
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
                CaretIndex += 1;
                e.Handled = true;
                return;
            // Increment
            case Key.Up:
                if (decimal.TryParse(Text, out var dec))
                {
                    var val = Math.Min(dec + Increment, Maximum);
                    Text = val.ToString(FormatString, NumberFormatInfo.InvariantInfo);
                    Value = val;
                }
                e.Handled = true;
                return;
            // Decrement
            case Key.Down:
                if (decimal.TryParse(Text, out dec))
                {
                    var val = Math.Max(dec - Increment, Minimum);
                    Text = val.ToString(FormatString, NumberFormatInfo.InvariantInfo);
                    Value = val;
                }
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
            // Select All
            case Key.A when (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0:
                SelectionStart = 0;
                SelectionEnd = Text?.Length ?? 0;
                e.Handled = true;
                return;
        }
    }

    /// <summary>
    /// Scroll handler
    /// </summary>
    /// <param name="e">Scroll event args</param>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // Increment
        if (e.Delta.Y > 0)
        {
            if (decimal.TryParse(Text, out var dec))
            {
                var val = Math.Min(dec + Increment, Maximum);
                Text = val.ToString(FormatString, NumberFormatInfo.InvariantInfo);
                Value = val;
            }
        }
        // Decrement
        else
        {
            if (decimal.TryParse(Text, out var dec))
            {
                var val = Math.Max(dec - Increment, Minimum);
                Text = val.ToString(FormatString, NumberFormatInfo.InvariantInfo);
                Value = val;
            }
        }
        e.Handled = true;
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
        var inserting = string.Empty;
        foreach (var c in input)
        {
            // Discard input if invalid character
            if (c is (< '0' or > '9') and not '.' and not ',' and not '-')
                continue;

            if (!AllowDecimal && c is '.' or ',')
                continue;

            // Move forward if at punctuation
            if (CaretIndex < Text?.Length)
            {
                var next = Text?[CaretIndex] ?? '!';
                if (c is '.' or ',' && next is '.' or ',')
                    CaretIndex += 1;
            }

            // Ignore punctuation keys
            if (c is '.' or ',' && (Text?.Contains('.') ?? true))
                continue;

            // Good input
            inserting += c;
        }

        // Insert the text, minding selection
        if (SelectionStart == SelectionEnd)
        {
            Text = Text?.Insert(CaretIndex, inserting);
            CaretIndex += 1;
        }
        else
        {
            if (Text is null)
            {
                Text = inserting;
                CaretIndex += 1;
            }
            else
            {
                // Swap if needed
                if (SelectionEnd < SelectionStart)
                    (SelectionStart, SelectionEnd) = (SelectionEnd, SelectionStart);

                var start = SelectionStart;
                var end = SelectionEnd;
                Text = Text[..start] + inserting + Text[end..];
                CaretIndex = start + inserting.Length;
            }
        }

        if (!decimal.TryParse(Text, out var dec))
            dec = 0;

        var val = Math.Clamp(dec, Minimum, Maximum);
        Text = val.ToString(FormatString, NumberFormatInfo.InvariantInfo);
        Value = val;
        RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
    }
}
