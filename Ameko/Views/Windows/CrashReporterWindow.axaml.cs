// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using Ameko.ViewModels.Windows;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace Ameko.Views.Windows;

public partial class CrashReporterWindow : ReactiveWindow<CrashReporterWindowViewModel>
{
    public CrashReporterWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel
                ?.OpenDirectory.RegisterHandler(context =>
                {
                    context.SetOutput(RxVoid.Default);

                    var launcher = GetTopLevel(this)?.Launcher;
                    if (launcher is null)
                        return;

                    var dir = new DirectoryInfo(context.Input);
                    if (!dir.Exists)
                        return;
                    launcher.LaunchDirectoryInfoAsync(dir);
                })
                .DisposeWith(disposables);
        });

        Closed += (_, _) =>
        {
            if (
                Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop
            )
                desktop.Shutdown();
            else
                Environment.Exit(0);
        };
    }
}
