// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using AvaloniaEdit.Indentation;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;
using Holo.Scripting.Models;
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

        this.WhenActivated(disposables =>
        {
            ViewModel?.SaveCommand.Subscribe(Close).DisposeWith(disposables);

            // Trigger source loading, then use the resulting PackageType to configure the highlighting
            ViewModel
                ?.LoadSourceCommand.Execute(RxVoid.Default)
                .Subscribe(packageType =>
                {
                    var registryOptions = new RegistryOptions(ThemeName.Monokai);
                    var textMateInstallation = ScriptSourceEditor.InstallTextMate(registryOptions);

                    // Configure the indention strategy
                    ScriptSourceEditor.TextArea.IndentationStrategy = packageType
                        is PackageType.Script
                            or PackageType.Library
                        ? new CSharpIndentationStrategy(ScriptSourceEditor.Options)
                        : new DefaultIndentationStrategy();

                    // Configure the syntax highlighting
                    textMateInstallation.SetGrammar(
                        packageType is PackageType.Script or PackageType.Library
                            ? registryOptions.GetScopeByExtension(".cs")
                            : registryOptions.GetScopeByExtension(".js")
                    );
                })
                .DisposeWith(disposables);
        });
    }
}
