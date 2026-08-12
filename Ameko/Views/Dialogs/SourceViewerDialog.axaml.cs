// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using TextMateSharp.Grammars;

namespace Ameko.Views.Dialogs;

public partial class SourceViewerDialog : ReactiveWindow<SourceViewerDialogViewModel>
{
    public SourceViewerDialog()
    {
        InitializeComponent();

        var registryOptions = new RegistryOptions(ThemeName.Monokai);
        var textMateInstallation = ScriptSourceEditor.InstallTextMate(registryOptions);

        textMateInstallation.SetGrammar(registryOptions.GetScopeByExtension(".cs"));

        ScriptSourceEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(
            ScriptSourceEditor.Options
        );

        this.WhenActivated(disposables =>
        {
            ViewModel
                ?.LoadSourceCommand.Execute(RxVoid.Default)
                .Subscribe()
                .DisposeWith(disposables);
            ViewModel?.SaveCommand.Subscribe(Close).DisposeWith(disposables);
        });
    }
}
