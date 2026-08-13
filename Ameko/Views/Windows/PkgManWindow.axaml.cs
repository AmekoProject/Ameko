// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Ameko.ViewModels;
using Ameko.ViewModels.Dialogs;
using Ameko.ViewModels.Windows;
using Ameko.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace Ameko.Views.Windows;

public partial class PkgManWindow : ReactiveWindow<PkgManWindowViewModel>
{
    /// <summary>
    /// Show an async dialog window
    /// </summary>
    /// <param name="interaction">Interaction</param>
    /// <typeparam name="TDialog">Dialog type</typeparam>
    /// <typeparam name="TViewModel">ViewModel type</typeparam>
    private async Task DoShowDialogAsync<TDialog, TViewModel>(
        IInteractionContext<TViewModel, RxVoid> interaction
    )
        where TDialog : Window, new()
        where TViewModel : ViewModelBase
    {
        var dialog = new TDialog { DataContext = interaction.Input };
        await dialog.ShowDialog(this);
        interaction.SetOutput(RxVoid.Default);
    }

    public PkgManWindow()
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
            ViewModel
                ?.ShowChangelog.RegisterHandler(
                    DoShowDialogAsync<ChangelogDialog, ChangelogDialogViewModel>
                )
                .DisposeWith(disposables);

            ViewModel
                ?.ShowSourceViewer.RegisterHandler(
                    DoShowDialogAsync<SourceViewerDialog, SourceViewerDialogViewModel>
                )
                .DisposeWith(disposables);
        });
    }
}
