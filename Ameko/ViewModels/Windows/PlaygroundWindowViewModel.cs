// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Windows.Input;
using Ameko.DataModels;
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
        var uri = new Uri("avares://Ameko/Assets/Text/Playground.js.txt");
        using var reader = new StreamReader(AssetLoader.Open(uri));
        Document.Text = reader.ReadToEnd();
    }

    public PlaygroundWindowViewModel(IPersistence persistence, IScriptService scriptService)
    {
        _status = I18N.Playground.Playground_Status_Ready;
        Document = new TextDocument(persistence.PlaygroundJs);

        if (string.IsNullOrEmpty(Document.Text))
            Reset();

        ResetCommand = ReactiveCommand.Create(Reset);

        ExecuteCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            IsExecuting = true;
            persistence.PlaygroundJs = Document.Text;
            Status = await scriptService.ExecutePlaygroundScriptAsync(
                Document.Text,
                PlaygroundLanguage.JavaScript
            );
            IsExecuting = false;
        });
    }
}
