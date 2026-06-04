// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Runtime.InteropServices;
using Ameko.Services;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Ameko.Views.Windows;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        KeyDown += (_, e) =>
        {
            switch (e.Key)
            {
                case Key.Escape:
                case Key.W
                    when e.KeyModifiers.HasFlag(KeyModifiers.Control)
                        || e.KeyModifiers.HasFlag(KeyModifiers.Meta):
                    Close();
                    break;
            }
        };
    }

    private void VersionLabel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var window = GetTopLevel(this);
        _ = window!.Clipboard!.SetTextAsync(
            $"""
            Version: Ameko {VersionService.FullLabel}
            OS: {RuntimeInformation.OSDescription}
            Platform: {SystemService.Platform}
            Platform Architecture: {RuntimeInformation.OSArchitecture}
            Desktop Environment: {SystemService.DesktopEnvironment}
            Display Server: {SystemService.WindowManager}
            Framework: {RuntimeInformation.FrameworkDescription}
            """
        );
    }
}
