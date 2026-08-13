// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace Ameko.Views.Dialogs;

public partial class KeybindsDialog : ReactiveWindow<KeybindsDialogViewModel>
{
    public KeybindsDialog()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel
                ?.SaveCommand.Where(success => success)
                .Subscribe(_ => Close())
                .DisposeWith(disposables);
        });
    }
}
