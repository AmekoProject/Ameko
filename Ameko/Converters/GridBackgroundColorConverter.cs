// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AssCS;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Color = Avalonia.Media.Color;

namespace Ameko.Converters;

/// <summary>
/// Converter for displaying event state in the grid
/// </summary>
public class GridBackgroundColorConverter : IMultiValueConverter
{
    // Expected binding order
    private const int ExpectedValueCount = 9;
    private const int IsCommentIndex = 0;
    private const int EventIndex = 1;
    private const int SelectedEventsIndex = 2;
    private const int ActiveEventIndex = 3;
    private const int CurrentTimeIndex = 4;
    private const int RowStartIndex = 5;
    private const int RowEndIndex = 6;
    private const int ActiveStartIndex = 7;
    private const int ActiveEndIndex = 8;

    private static readonly IBrush SelectedBrush = Tint(Colors.DodgerBlue, 0.25);
    private static readonly IBrush CommentBrush = Tint(Colors.Gray, 0.5);
    private static readonly IBrush CommentSelectedBrush = Tint(Colors.SlateGray, 0.5);
    private static readonly IBrush FrameActiveBrush = Tint(Colors.MediumSeaGreen, 0.25);
    private static readonly IBrush CollisionBrush = Tint(Colors.Orange, 0.25);
    private static readonly IBrush ActiveCollisionBrush = Tint(Colors.MediumOrchid, 0.25);

    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (values.Count != ExpectedValueCount || values[EventIndex] is not Event @event)
            return Brushes.Transparent;

        var isComment = values[IsCommentIndex] is true;
        var isSelected =
            values[SelectedEventsIndex] is IList<Event> selection && selection.Contains(@event);

        // Selection and comment state take priority over timing-based states
        switch (isSelected, isComment)
        {
            case (true, true):
                return CommentSelectedBrush;
            case (true, false):
                return SelectedBrush;
            case (false, true):
                return CommentBrush;
        }

        // Timing-based states need the row's time range
        if (values[RowStartIndex] is not Time rowStart || values[RowEndIndex] is not Time rowEnd)
            return Brushes.Transparent;

        var isCollision =
            values[ActiveEventIndex] is Event activeEvent
            && @event != activeEvent
            && values[ActiveStartIndex] is Time activeStart
            && values[ActiveEndIndex] is Time activeEnd
            && rowStart < activeEnd
            && activeStart < rowEnd;

        var isFrameActive =
            values[CurrentTimeIndex] is Time currentTime
            && rowStart <= currentTime
            && currentTime < rowEnd;

        return (isCollision, isFrameActive) switch
        {
            (true, true) => ActiveCollisionBrush,
            (false, true) => FrameActiveBrush,
            (true, false) => CollisionBrush,
            _ => Brushes.Transparent,
        };
    }

    private static ImmutableSolidColorBrush Tint(Color color, double opacity) =>
        new(color, opacity);
}
