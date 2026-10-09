// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using Avalonia.Input;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace Ameko.Views.Dialogs;

public partial class ResolutionDialog : ReactiveWindow<ResolutionDialogViewModel>
{
    public ResolutionDialog()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            OkButton.Focus();
            ViewModel?.OkCommand.Subscribe(Close).DisposeWith(disposables);

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
        });
    }
}
