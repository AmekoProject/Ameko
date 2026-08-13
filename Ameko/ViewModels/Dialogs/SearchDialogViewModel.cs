// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
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
    public ICommand FindNextCommand { get; }

    private string? _previousQuery;
    private SearchFilter? _previousFilter;

    private List<Event> _results = [];
    private int _resultIndex = 0;
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
            if (wsp != _lastWorkspace || Query != _previousQuery || Filter != _previousFilter)
            {
                GenerateResults();
                _previousQuery = Query;
                _previousFilter = Filter;
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
        _resultIndex = 0;
        _results =
            _projectProvider
                .Current.WorkingSpace?.Document.EventManager.Events.Where(e =>
                    Filter switch
                    {
                        SearchFilter.Text => e.Text.Contains(
                            Query,
                            StringComparison.CurrentCultureIgnoreCase
                        ),
                        SearchFilter.StrippedText => e.GetStrippedText()
                            .Contains(Query, StringComparison.CurrentCultureIgnoreCase),
                        SearchFilter.Style => e.Style.Contains(
                            Query,
                            StringComparison.InvariantCultureIgnoreCase
                        ),
                        SearchFilter.Actor => e.Actor.Contains(
                            Query,
                            StringComparison.InvariantCultureIgnoreCase
                        ),
                        SearchFilter.Effect => e.Effect.Contains(
                            Query,
                            StringComparison.InvariantCultureIgnoreCase
                        ),
                        _ => false,
                    }
                )
                .ToList()
            ?? [];
    }
}
