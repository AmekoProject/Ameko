// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Ameko.ViewModels.Windows;
using Avalonia.Input.Platform;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace Ameko.Views.Windows;

public partial class LogWindow : ReactiveWindow<LogWindowViewModel>
{
    private async Task DoCopyLogsAsync(IInteractionContext<string, RxVoid> interaction)
    {
        interaction.SetOutput(RxVoid.Default);
        if (Clipboard is null)
            return;
        await Clipboard.SetTextAsync(interaction.Input);
    }

    public LogWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel?.CopySelection.RegisterHandler(DoCopyLogsAsync).DisposeWith(disposables);
        });
    }
}
