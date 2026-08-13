// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ameko.Utilities;
using Ameko.ViewModels.Controls;
using AssCS;
using Holo;
using Holo.Models;
using Holo.Providers;
using ReactiveUI;
using ReactiveUI.Primitives.Signals;
using ChangeType = AssCS.History.ChangeType;

namespace Ameko.ViewModels.Dialogs;

public class SpellcheckDialogViewModel : ViewModelBase
{
    private IEnumerator<SpellcheckSuggestion> _spellcheckSuggestions;

    private readonly Workspace? _workspace;
    private readonly TabItemViewModel? _tabVm;

    private string _misspelledWord = string.Empty;
    private string _misspelledWordCopy = string.Empty;
    private readonly ObservableCollection<string> _suggestions;

    public string DictionaryInUse { get; }

    public string MisspelledWord
    {
        get => _misspelledWord;
        set
        {
            this.RaiseAndSetIfChanged(ref _misspelledWord, value);
            this.RaisePropertyChanged(nameof(CanChange));
        }
    }

    public ReadOnlyObservableCollection<string> Suggestions { get; }

    public string? SelectedSuggestion
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanChange));
        }
    }

    public int EventId { get; private set; }

    public bool HaveSuggestions
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool CanChange =>
        MisspelledWord != _misspelledWordCopy || SelectedSuggestion is not null;

    public ICommand ChangeCommand { get; }
    public ICommand FindNextCommand { get; }
    public ICommand AddToProjectCommand { get; }
    public ICommand AddToGlobalsCommand { get; }
    public ICommand AddToBothCommand { get; }

    private Action _resetSuggestions;

    /// <summary>
    /// Initialize the Spellcheck Dialog with suggestions for the entire active <see cref="Document"/>
    /// </summary>
    /// <param name="projectProvider">Project provider</param>
    /// <param name="spellcheckService">Spellcheck service</param>
    /// <param name="tabFactory">Tab factory</param>
    public SpellcheckDialogViewModel(
        IProjectProvider projectProvider,
        ISpellcheckService spellcheckService,
        ITabFactory tabFactory
    )
        : this(
            projectProvider,
            spellcheckService,
            tabFactory,
            spellcheckService.CheckSpelling().GetEnumerator()
        )
    {
        _resetSuggestions = () =>
        {
            _spellcheckSuggestions = spellcheckService.CheckSpelling().GetEnumerator();
        };
    }

    /// <summary>
    /// Initialize the Spellcheck Dialog with suggestions for the entire specified <paramref name="event"/>
    /// </summary>
    /// <param name="projectProvider">Project provider</param>
    /// <param name="spellcheckService">Spellcheck service</param>
    /// <param name="tabFactory">Tab factory</param>
    /// <param name="event"></param>
    public SpellcheckDialogViewModel(
        IProjectProvider projectProvider,
        ISpellcheckService spellcheckService,
        ITabFactory tabFactory,
        Event @event
    )
        : this(
            projectProvider,
            spellcheckService,
            tabFactory,
            spellcheckService.CheckSpelling(@event).GetEnumerator()
        )
    {
        _resetSuggestions = () =>
        {
            _spellcheckSuggestions = spellcheckService.CheckSpelling(@event).GetEnumerator();
        };
    }

    private SpellcheckDialogViewModel(
        IProjectProvider projectProvider,
        ISpellcheckService spellcheckService,
        ITabFactory tabFactory,
        IEnumerator<SpellcheckSuggestion> initialSuggestions
    )
    {
        // Configure the initial suggestions
        _spellcheckSuggestions = initialSuggestions;

        // If this private constructor is being called for some reason
        _resetSuggestions ??= () =>
        {
            _spellcheckSuggestions = spellcheckService.CheckSpelling().GetEnumerator();
        };

        DictionaryInUse = string.Format(
            I18N.Spellcheck.Spellcheck_Label_Dictionary,
            spellcheckService.CurrentLanguage.Name
        );

        _suggestions = [];
        Suggestions = new ReadOnlyObservableCollection<string>(_suggestions);

        _workspace = projectProvider.Current.WorkingSpace;
        if (_workspace is not null)
            tabFactory.TryGetViewModel(_workspace, out _tabVm);

        ChangeCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (!CanChange || _workspace is null)
                return;

            var replacement =
                MisspelledWord != _misspelledWordCopy ? MisspelledWord : SelectedSuggestion!;

            var @event = _workspace.Document.EventManager.Get(EventId);
            @event.Text = @event.Text.Replace(_misspelledWordCopy, replacement);
            _workspace.Commit(@event, ChangeType.ModifyEventText);

            await LoadNextSuggestion();
        });

        FindNextCommand = ReactiveCommand.CreateFromTask(async () => await LoadNextSuggestion());

        AddToProjectCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            spellcheckService.AddWordToProject(_misspelledWord);
            // Restart in case it shows up again
            _resetSuggestions();
            await LoadNextSuggestion();
        });

        AddToGlobalsCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            spellcheckService.AddWordToGlobals(_misspelledWord);
            // Restart in case it shows up again
            _resetSuggestions();
            await LoadNextSuggestion();
        });

        AddToBothCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            spellcheckService.AddWordToProject(_misspelledWord);
            spellcheckService.AddWordToGlobals(_misspelledWord);
            // Restart in case it shows up again
            _resetSuggestions();
            await LoadNextSuggestion();
        });

        _ = LoadNextSuggestion();
    }

    private async Task LoadNextSuggestion()
    {
        if (!_spellcheckSuggestions.MoveNext())
        {
            HaveSuggestions = false;
            return;
        }
        var suggestion = _spellcheckSuggestions.Current;

        MisspelledWord = suggestion.Word;
        _misspelledWordCopy = suggestion.Word;

        _suggestions.Clear();
        _suggestions.AddRange(suggestion.Suggestions);

        SelectedSuggestion = _suggestions.FirstOrDefault();

        EventId = suggestion.EventId;

        // Scroll and select
        var @event = _workspace?.Document.EventManager.Get(EventId);
        if (@event is not null && _tabVm is not null)
            await _tabVm.ScrollToAndSelectEvent.Handle(@event);

        this.RaisePropertyChanged(nameof(CanChange));
    }
}
