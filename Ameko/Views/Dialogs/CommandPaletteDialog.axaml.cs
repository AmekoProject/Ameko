// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace Ameko.Views.Dialogs;

public partial class CommandPaletteDialog : ReactiveWindow<CommandPaletteDialogViewModel>
{
    public CommandPaletteDialog()
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

            QueryBox.Focus();
            QueryBox.SelectionStart = 0;
            QueryBox.SelectionEnd = QueryBox.Text.Length;
            ViewModel?.GenerateCommandSuggestions();
        };

        this.WhenActivated(disposables =>
        {
            QueryBox.Text ??= string.Empty;

            QueryBox.Focus();
            QueryBox.SelectionStart = 0;
            QueryBox.SelectionEnd = QueryBox.Text.Length;

            ViewModel?.GoCommand.Subscribe(Close).DisposeWith(disposables);
            ViewModel?.GenerateCommandSuggestions();

            Deactivated += (_, _) =>
            {
                Close();
            };
        });
    }

    private void QueryBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        ViewModel?.GenerateSuggestions();
    }

    private void QueryBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is null)
            return;
        switch (e.Key)
        {
            case Key.Down:
                if (ViewModel.SelectedIndex >= ViewModel.Results.Count - 1)
                    ViewModel.SelectedIndex = 0;
                else
                    ViewModel.SelectedIndex++;
                break;
            case Key.Up:
                if (ViewModel.SelectedIndex <= 0)
                    ViewModel.SelectedIndex = ViewModel.Results.Count - 1;
                else
                    ViewModel.SelectedIndex--;
                break;
        }
    }
}
