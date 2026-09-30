// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Ameko.DataModels;
using Ameko.Utilities;
using AssCS;
using Holo;
using Holo.Providers;
using ReactiveUI;
using ReactiveUI.Primitives.Signals;

namespace Ameko.ViewModels.Dialogs;

public class SearchDialogViewModel : ViewModelBase
{
    private readonly IProjectProvider _projectProvider;

    public string Query { get; set; } = string.Empty;
    public SearchFilter Filter { get; set; } = SearchFilter.Text;
    public bool MatchCase { get; set; }
    public bool UseRegex
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    public ICommand FindNextCommand { get; }

    private string? _previousQuery;
    private SearchFilter? _previousFilter;
    private bool? _previousCase;
    private bool? _previousRegex;

    private List<Event> _results = [];
    private int _resultIndex;
    private Workspace? _lastWorkspace;

    public SearchDialogViewModel(IProjectProvider projectProvider, ITabFactory tabFactory)
    {
        _projectProvider = projectProvider;

        FindNextCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var wsp = _projectProvider.Current.WorkingSpace;
            if (wsp is null)
                return;

            // Check if this is a new query or a continuation of the previous one
            if (
                wsp != _lastWorkspace
                || Query != _previousQuery
                || Filter != _previousFilter
                || UseRegex != _previousRegex
                || MatchCase != _previousCase
            )
            {
                GenerateResults();
                _previousQuery = Query;
                _previousFilter = Filter;
                _previousRegex = UseRegex;
                _previousCase = MatchCase;
                _lastWorkspace = wsp;
            }

            // Loop back if needed
            if (_results.Count == 0)
                return;

            if (_resultIndex >= _results.Count)
                _resultIndex = 0;

            if (!tabFactory.TryGetViewModel(wsp, out var vm))
                return;
            await vm.ScrollToAndSelectEvent.Handle(_results[_resultIndex++]);
        });
    }

    private void GenerateResults()
    {
        var currentCultureCase = MatchCase
            ? StringComparison.CurrentCulture
            : StringComparison.CurrentCultureIgnoreCase;
        var invariantCase = MatchCase
            ? StringComparison.InvariantCulture
            : StringComparison.InvariantCultureIgnoreCase;

        _resultIndex = 0;
        if (UseRegex)
        {
            _results =
                _projectProvider
                    .Current.WorkingSpace?.Document.EventManager.Events.Where(e =>
                        Filter switch
                        {
                            SearchFilter.Text => Regex.IsMatch(e.Text, Query),
                            SearchFilter.StrippedText => Regex.IsMatch(e.GetStrippedText(), Query),
                            SearchFilter.Style => Regex.IsMatch(e.Style, Query),
                            SearchFilter.Actor => Regex.IsMatch(e.Actor, Query),
                            SearchFilter.Effect => Regex.IsMatch(e.Effect, Query),
                            _ => false,
                        }
                    )
                    .ToList()
                ?? [];
        }
        else
        {
            _results =
                _projectProvider
                    .Current.WorkingSpace?.Document.EventManager.Events.Where(e =>
                        Filter switch
                        {
                            SearchFilter.Text => e.Text.Contains(Query, currentCultureCase),
                            SearchFilter.StrippedText => e.GetStrippedText()
                                .Contains(Query, currentCultureCase),
                            SearchFilter.Style => e.Style.Contains(Query, invariantCase),
                            SearchFilter.Actor => e.Actor.Contains(Query, invariantCase),
                            SearchFilter.Effect => e.Effect.Contains(Query, invariantCase),
                            _ => false,
                        }
                    )
                    .ToList()
                ?? [];
        }
    }
}
