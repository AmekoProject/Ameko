// SPDX-License-Identifier: GPL-3.0-only

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Holo.Models;
using Material.Icons;
using Material.Icons.Avalonia;

namespace Ameko.Views.Sdk;

/// <summary>
/// A simple message box API
/// </summary>
public class MessageBox : Window
{
    /// <summary>
    /// A box for displaying a message to the user
    /// </summary>
    /// <param name="title">Window title</param>
    /// <param name="text">Body</param>
    /// <param name="buttonSet">Which buttons to use</param>
    /// <param name="primary">Which button is default</param>
    /// <param name="iconKind">Which icon to use</param>
    public MessageBox(
        string title,
        string text,
        MsgBoxButtonSet buttonSet = MsgBoxButtonSet.Ok,
        MsgBoxButton primary = MsgBoxButton.Ok,
        MaterialIconKind iconKind = MaterialIconKind.Info
    )
    {
        Title = title;
        Icon = new WindowIcon(
            AssetLoader.Open(new Uri("avares://Ameko/Assets/Ameko-Simplified-BG-64.ico"))
        );
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        CanResize = false;
        Topmost = true;
        MaxWidth = 500;

        var grid = new Grid
        {
            Margin = new Thickness(20),
            ColumnSpacing = 10,
            RowSpacing = 10,
            ColumnDefinitions =
            [
                new ColumnDefinition { Width = GridLength.Auto }, // Icon
                new ColumnDefinition { Width = GridLength.Star }, // Content
            ],
            RowDefinitions =
            [
                new RowDefinition { Height = GridLength.Auto }, // Text
                new RowDefinition { Height = GridLength.Auto }, // Buttons
            ],
        };
        Content = grid;

        var icon = new MaterialIcon
        {
            Kind = iconKind,
            Width = 32,
            Height = 32,
        };
        icon.SetValue(Grid.ColumnProperty, 0);
        icon.SetValue(Grid.RowProperty, 0);
        grid.Children.Add(icon);

        var headerLabel = new TextBlock
        {
            Text = text,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        headerLabel.SetValue(Grid.ColumnProperty, 1);
        headerLabel.SetValue(Grid.RowProperty, 0);
        grid.Children.Add(headerLabel);

        var okButton = new Button
        {
            Content = new TextBlock { Text = I18N.Other.MsgBox_Btn_OK },
            Width = 75,
            IsDefault = primary == MsgBoxButton.Ok,
        };
        var yesButton = new Button
        {
            Content = new TextBlock { Text = I18N.Other.MsgBox_Btn_Yes },
            Width = 75,
            IsDefault = primary == MsgBoxButton.Yes,
        };
        var noButton = new Button
        {
            Content = new TextBlock { Text = I18N.Other.MsgBox_Btn_No },
            Width = 75,
            IsDefault = primary == MsgBoxButton.No,
        };
        var cancelButton = new Button
        {
            Content = new TextBlock { Text = I18N.Other.MsgBox_Btn_Cancel },
            Width = 75,
            IsDefault = primary == MsgBoxButton.Cancel,
        };

        okButton.Classes.Add(primary == MsgBoxButton.Ok ? "Primary" : "Tertiary");
        yesButton.Classes.Add(primary == MsgBoxButton.Yes ? "Primary" : "Tertiary");
        noButton.Classes.Add(primary == MsgBoxButton.No ? "Primary" : "Tertiary");
        cancelButton.Classes.Add(primary == MsgBoxButton.Cancel ? "Primary" : "Tertiary");

        okButton.Click += (_, _) => Close(MsgBoxButton.Ok);
        yesButton.Click += (_, _) => Close(MsgBoxButton.Yes);
        noButton.Click += (_, _) => Close(MsgBoxButton.No);
        cancelButton.Click += (_, _) => Close(MsgBoxButton.Cancel);

        var buttons = new ReversibleStackPanel()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = OperatingSystem.IsMacOS()
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left,
        };
        buttons.SetValue(Grid.ColumnProperty, 1);
        buttons.SetValue(Grid.RowProperty, 1);
        buttons.ReverseOrder = OperatingSystem.IsMacOS();

        switch (buttonSet)
        {
            case MsgBoxButtonSet.Ok:
                buttons.Children.Add(okButton);
                break;
            case MsgBoxButtonSet.YesNo:
                buttons.Children.AddRange([yesButton, noButton]);
                break;
            case MsgBoxButtonSet.OkCancel:
                buttons.Children.AddRange([okButton, cancelButton]);
                break;
            case MsgBoxButtonSet.YesNoCancel:
                buttons.Children.AddRange([yesButton, noButton, cancelButton]);
                break;
        }

        grid.Children.Add(buttons);
    }
}
