// SPDX-License-Identifier: GPL-3.0-only

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using Ameko.ViewModels.Windows;
using Avalonia.Input.Platform;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Ameko.Views.Windows;

public partial class LogWindow : ReactiveWindow<LogWindowViewModel>
{
    private async Task DoCopyLogsAsync(IInteractionContext<string, Unit> interaction)
    {
        interaction.SetOutput(Unit.Default);
        if (Clipboard is null)
            return;
        await Clipboard.SetTextAsync(interaction.Input);
    }

    public LogWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel?.CopySelection.RegisterHandler(DoCopyLogsAsync);
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }
}
