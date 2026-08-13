// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Ameko.DataModels;
using Ameko.Messages;
using Ameko.Utilities;
using AssCS;
using Holo.Providers;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class SelectDialogViewModel : ViewModelBase
{
    private readonly IProjectProvider _projectProvider;

    public string Query { get; set; } = string.Empty;
    public SearchFilter Filter { get; set; } = SearchFilter.Text;
    public ReactiveCommand<RxVoid, EmptyMessage> SelectCommand { get; }

    public SelectDialogViewModel(IProjectProvider projectProvider, ITabFactory tabFactory)
    {
        _projectProvider = projectProvider;

        SelectCommand = ReactiveCommand.Create(() =>
        {
            var wsp = _projectProvider.Current.WorkingSpace;
            if (wsp is null || string.IsNullOrEmpty(Query))
                return new EmptyMessage();

            var results = GenerateResults();
            if (results.Count == 0)
                return new EmptyMessage();

            wsp.SelectionManager.Select(results[0], results);
            return new EmptyMessage();
        });
    }

    private List<Event> GenerateResults()
    {
        return _projectProvider
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
