// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Windows.Input;
using Ameko.Services;
using Avalonia.Platform;
using AvaloniaEdit.Document;
using Holo.Configuration;
using ReactiveUI;

namespace Ameko.ViewModels.Windows;

public class PlaygroundWindowViewModel : ViewModelBase
{
    private string _status;

    public ICommand ExecuteCommand { get; }
    public ICommand ResetCommand { get; }
    public TextDocument Document { get; }

    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public bool IsExecuting
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private void Reset()
    {
        var uri = new Uri("avares://Ameko/Assets/Text/Playground.cs.txt");
        using var reader = new StreamReader(AssetLoader.Open(uri));
        Document.Text = reader.ReadToEnd();
    }

    public PlaygroundWindowViewModel(IPersistence persistence, IScriptService scriptService)
    {
        _status = I18N.Playground.Playground_Status_Ready;
        Document = new TextDocument(persistence.PlaygroundCs);

        if (string.IsNullOrEmpty(Document.Text))
            Reset();

        ResetCommand = ReactiveCommand.Create(Reset);

        ExecuteCommand = ReactiveCommand.Create(() =>
        {
            IsExecuting = true;
            persistence.PlaygroundCs = Document.Text;
            Status = scriptService.ExecutePlaygroundScript(Document.Text, true);
            IsExecuting = false;
        });
    }
}
