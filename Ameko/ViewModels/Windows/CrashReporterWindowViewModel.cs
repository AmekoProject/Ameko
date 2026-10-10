// SPDX-License-Identifier: GPL-3.0-only

using System.IO;
using System.Windows.Input;
using Holo.IO;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace Ameko.ViewModels.Windows;

public class CrashReporterWindowViewModel : ViewModelBase
{
    public CrashReporterWindowViewModel(string content)
    {
        Content = content;
        OpenDirectory = new Interaction<string, RxVoid>();
        OpenDirectoryCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await OpenDirectory.Handle(Path.Combine(Directories.DataHome, "crash-reports"));
        });
    }

    public string Content { get; }

    public Interaction<string, RxVoid> OpenDirectory { get; }

    public ICommand OpenDirectoryCommand { get; }
}
