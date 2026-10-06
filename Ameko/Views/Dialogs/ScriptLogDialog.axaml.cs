// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace Ameko.Views.Dialogs;

public partial class ScriptLogDialog : ReactiveWindow<ScriptLogDialogViewModel>
{
    public ScriptLogDialog()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel?.OkCommand.Subscribe(Close).DisposeWith(disposables);
        });
    }
}
