// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using Avalonia.Input;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace Ameko.Views.Dialogs;

public partial class SearchDialog : ReactiveWindow<SearchDialogViewModel>
{
    public SearchDialog()
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

        GotFocus += (_, _) =>
        {
            QueryBox.Text ??= string.Empty;
            QueryBox.SelectionStart = 0;
            QueryBox.SelectionEnd = QueryBox.Text.Length;
            QueryBox.Focus();
        };

        this.WhenActivated(disposables =>
        {
            QueryBox.Text ??= string.Empty;
            QueryBox.SelectionStart = 0;
            QueryBox.SelectionEnd = QueryBox.Text.Length;
            QueryBox.Focus();
            new ActionDisposable(() => { }).DisposeWith(disposables);
        });
    }
}
