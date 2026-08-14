// SPDX-License-Identifier: GPL-3.0-only

using Avalonia.Controls;
using AvaloniaEdit.Indentation;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace Ameko.Views.Windows;

public partial class PlaygroundWindow : Window
{
    public PlaygroundWindow()
    {
        InitializeComponent();

        var registryOptions = new RegistryOptions(ThemeName.Monokai);
        var textMateInstallation = PlaygroundEditor.InstallTextMate(registryOptions);

        textMateInstallation.SetGrammar(registryOptions.GetScopeByExtension(".js"));

        PlaygroundEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
    }
}
