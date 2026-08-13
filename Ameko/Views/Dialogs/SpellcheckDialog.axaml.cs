// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using Avalonia.Input;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace Ameko.Views.Dialogs;

public partial class SpellcheckDialog : ReactiveWindow<JumpDialogViewModel>
{
    public SpellcheckDialog()
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

        this.WhenActivated(disposables =>
        {
            MisspellingBox.Focus();
            new ActionDisposable(() => { }).DisposeWith(disposables);
        });
    }
}
