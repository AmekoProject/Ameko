// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Ameko.Messages;
using Ameko.ViewModels.Dialogs;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using Color = AssCS.Color;

namespace Ameko.Views.Dialogs;

public partial class StyleEditorDialog : ReactiveWindow<StyleEditorDialogViewModel>
{
    private async Task DoShowColorDialogAsync(
        IInteractionContext<ColorDialogViewModel, Color?> interaction
    )
    {
        var dialog = new ColorDialog { DataContext = interaction.Input };
        var result = await dialog.ShowDialog<ColorDialogClosedMessage?>(this);
        interaction.SetOutput(result?.Color);
    }

    public StyleEditorDialog()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is null)
                return;

            ViewModel.SaveCommand.Subscribe(Close).DisposeWith(disposables);
            ViewModel
                .ShowColorDialog.RegisterHandler(DoShowColorDialogAsync)
                .DisposeWith(disposables);

            Closing += (_, e) =>
            {
                // Try to commit the style name,
                // and cancel closing the window if the name is invalid
                if (!ViewModel.CommitNameChange() && !ViewModel.IsNewStyle)
                    e.Cancel = true;
            };
        });
    }
}
