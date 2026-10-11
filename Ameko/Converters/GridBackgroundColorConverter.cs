// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Globalization;
using AssCS;
using Avalonia.Data.Converters;
using Avalonia.Media.Immutable;
using Holo.Configuration;
using Holo.Models;

namespace Ameko.Converters;

/// <summary>
/// Converter for displaying event state in the grid
/// </summary>
public class GridBackgroundColorConverter(IConfiguration configuration) : IMultiValueConverter
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

    private readonly ImmutableSolidColorBrush DefaultBrush = CreateBrush(
        configuration.Colors.Default
    );
    private readonly ImmutableSolidColorBrush SelectionBrush = CreateBrush(
        configuration.Colors.Selection
    );
    private readonly ImmutableSolidColorBrush CommentBrush = CreateBrush(
        configuration.Colors.Comment
    );
    private readonly ImmutableSolidColorBrush CommentSelectionBrush = CreateBrush(
        configuration.Colors.CommentSelection
    );
    private readonly ImmutableSolidColorBrush InFrameBrush = CreateBrush(
        configuration.Colors.InFrame
    );
    private readonly ImmutableSolidColorBrush CollisionBrush = CreateBrush(
        configuration.Colors.Collision
    );
    private readonly ImmutableSolidColorBrush CollisionInFrameBrush = CreateBrush(
        configuration.Colors.CollisionInFrame
    );

    public object Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (values.Count != ExpectedValueCount || values[EventIndex] is not Event @event)
            return DefaultBrush;

        var isComment = values[IsCommentIndex] is true;
        var isSelected =
            values[SelectedEventsIndex] is IList<Event> selection && selection.Contains(@event);

        // Selection and comment state take priority over timing-based states
        switch (isSelected, isComment)
        {
            case (true, true):
                return CommentSelectionBrush;
            case (true, false):
                return SelectionBrush;
            case (false, true):
                return CommentBrush;
        }

        // Timing-based states need the row's time range
        if (values[RowStartIndex] is not Time rowStart || values[RowEndIndex] is not Time rowEnd)
            return DefaultBrush;

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
            (true, true) => CollisionInFrameBrush,
            (false, true) => InFrameBrush,
            (true, false) => CollisionBrush,
            _ => DefaultBrush,
        };
    }

    private static ImmutableSolidColorBrush CreateBrush(HoloColor color) =>
        new(Avalonia.Media.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
}
