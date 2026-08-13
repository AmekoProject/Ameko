// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Ameko.DataModels;
using Ameko.Messages;
using AssCS.Utilities;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using Holo.Configuration.Keybinds;
using Holo.Models;
using Holo.Providers;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class CommandPaletteDialogViewModel : ViewModelBase
{
    private readonly IProjectProvider _projectProvider;
    private readonly ICommandRegistrar _commandRegistrar;
    private readonly IKeybindRegistrar _keybindRegistrar;
    private readonly ICommand _executeScriptCommand;

    public string Query { get; set; } = string.Empty;
    public ReactiveCommand<RxVoid, EmptyMessage> GoCommand { get; }

    private readonly RangeObservableCollection<CommandPaletteResult> _results = [];

    public AssCS.Utilities.ReadOnlyObservableCollection<CommandPaletteResult> Results { get; }

    private List<CommandCommandPaletteResult> _commandSuggestions = [];

    public CommandPaletteResult? SelectedResult
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int? SelectedIndex
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public CommandPaletteDialogViewModel(
        IProjectProvider projectProvider,
        ICommandRegistrar commandRegistrar,
        IKeybindRegistrar keybindRegistrar,
        ICommand openDocumentCommand,
        ICommand executeScriptCommand
    )
    {
        _projectProvider = projectProvider;
        _commandRegistrar = commandRegistrar;
        _keybindRegistrar = keybindRegistrar;
        _executeScriptCommand = executeScriptCommand;
        Results = new AssCS.Utilities.ReadOnlyObservableCollection<CommandPaletteResult>(_results);

        GoCommand = ReactiveCommand.Create(() =>
        {
            var wsp = _projectProvider.Current.WorkingSpace;
            if (SelectedResult is null || wsp is null)
                return new EmptyMessage();

            switch (SelectedResult)
            {
                case DocumentCommandPaletteResult dr:
                    openDocumentCommand.Execute(dr.Id);
                    break;
                case CommandCommandPaletteResult cr:
                    cr.Command.Execute(cr.Key.StartsWith("ameko") ? null : cr.Key);
                    break;
            }

            return new EmptyMessage();
        });
    }

    internal void GenerateSuggestions()
    {
        _results.Clear();
        if (string.IsNullOrWhiteSpace(Query))
            return;

        // Search for command
        if (Query[0] is '>')
        {
            var qCmd = new CommandCommandPaletteResult(Query[1..], null!);

            _results.AddRange(
                FuzzySharp
                    .Process.ExtractTop(
                        qCmd,
                        _commandSuggestions,
                        c => c.Key.ReplaceMany([".", "+"], " "),
                        ScorerCache.Get<DefaultRatioScorer>()
                    )
                    .Select(c => c.Value)
            );
        }
        else // Search for file
        {
            var prj = _projectProvider.Current;
            var files = prj.GetAllOfType(ProjectItemType.Document);

            var qFile = new DocumentItem { Name = Query };
            if (prj.SavePath is not null)
            {
                Path.Combine(Path.GetDirectoryName(prj.SavePath.LocalPath) ?? "/", Query);
                qFile.Uri = new Uri(prj.SavePath.LocalPath);
            }

            // Search by name
            var results = FuzzySharp.Process.ExtractTop(
                qFile,
                files,
                f => f.Title,
                ScorerCache.Get<DefaultRatioScorer>()
            );

            _results.AddRange(
                results.Select(r => new DocumentCommandPaletteResult(
                    Key: qFile.Uri is not null ? prj.GetRelativePath(r.Value) : string.Empty,
                    Name: r.Value.Title,
                    Id: r.Value.Id
                ))
            );
        }

        SelectedResult = _results.FirstOrDefault();
    }

    internal void GenerateCommandSuggestions()
    {
        var contextId = _projectProvider.Current.WorkingSpace?.Id ?? -1;
        _commandSuggestions = _commandRegistrar
            .GetMetadataForContext(contextId)
            .Where(c => c.PaletteEnabled)
            .Select(c => new CommandCommandPaletteResult(c.QualifiedName, c.Command))
            .Concat(
                _keybindRegistrar
                    .GetKeybinds()
                    .Where(kb => !kb.IsBuiltin)
                    .Select(kb => new CommandCommandPaletteResult(
                        kb.QualifiedName,
                        _executeScriptCommand
                    ))
            )
            .ToList();
    }
}
