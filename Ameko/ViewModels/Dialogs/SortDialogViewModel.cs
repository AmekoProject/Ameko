// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Ameko.DataModels;
using Ameko.Messages;
using AssCS;
using AssCS.History;
using Holo;
using Holo.Providers;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class SortDialogViewModel : ViewModelBase
{
    public SearchFilter SortBy { get; set; } = SearchFilter.Start;
    public SearchFilter ThenBy { get; set; } = SearchFilter.None;
    public bool SortByDescending { get; set; }
    public bool ThenByDescending { get; set; }

    public ReactiveCommand<RxVoid, EmptyMessage> SortCommand { get; }

    public SortDialogViewModel(IProjectProvider projectProvider)
    {
        var projectProvider1 = projectProvider;

        SortCommand = ReactiveCommand.Create(() =>
        {
            var wsp = projectProvider1.Current.WorkingSpace;
            if (wsp is null)
                return new EmptyMessage();

            var results = GenerateResults(wsp);
            if (results.Count == 0)
                return new EmptyMessage();

            wsp.Document.EventManager.Clear();
            wsp.Document.EventManager.AddLast(results);
            wsp.SelectionManager.Select(results[0]);
            wsp.Commit(results[0], ChangeType.ComplexEvent);

            return new EmptyMessage();
        });
    }

    private List<Event> GenerateResults(Workspace workspace)
    {
        var events = workspace.Document.EventManager.Events;
        if (events.Count == 0)
            return [];
        var sorted = !SortByDescending
            ? SortBy switch
            {
                SearchFilter.Text => events.OrderBy(e => e.Text),
                SearchFilter.StrippedText => events.OrderBy(e => e.GetStrippedText()),
                SearchFilter.Style => events.OrderBy(e => e.Style),
                SearchFilter.Actor => events.OrderBy(e => e.Actor),
                SearchFilter.Effect => events.OrderBy(e => e.Effect),
                SearchFilter.Comment => events.OrderBy(e => e.IsComment),
                SearchFilter.Start => events.OrderBy(e => e.Start),
                SearchFilter.End => events.OrderBy(e => e.End),
                _ => throw new ArgumentOutOfRangeException(),
            }
            : SortBy switch
            {
                SearchFilter.Text => events.OrderByDescending(e => e.Text),
                SearchFilter.StrippedText => events.OrderByDescending(e => e.GetStrippedText()),
                SearchFilter.Style => events.OrderByDescending(e => e.Style),
                SearchFilter.Actor => events.OrderByDescending(e => e.Actor),
                SearchFilter.Effect => events.OrderByDescending(e => e.Effect),
                SearchFilter.Comment => events.OrderByDescending(e => e.IsComment),
                SearchFilter.Start => events.OrderByDescending(e => e.Start),
                SearchFilter.End => events.OrderByDescending(e => e.End),
                _ => throw new ArgumentOutOfRangeException(),
            };

        if (ThenBy == SearchFilter.None)
            return sorted.ToList();

        sorted = !ThenByDescending
            ? ThenBy switch
            {
                SearchFilter.Text => sorted.ThenBy(e => e.Text),
                SearchFilter.StrippedText => sorted.ThenBy(e => e.GetStrippedText()),
                SearchFilter.Style => sorted.ThenBy(e => e.Style),
                SearchFilter.Actor => sorted.ThenBy(e => e.Actor),
                SearchFilter.Effect => sorted.ThenBy(e => e.Effect),
                SearchFilter.Comment => sorted.ThenBy(e => e.IsComment),
                SearchFilter.Start => sorted.ThenBy(e => e.Start),
                SearchFilter.End => sorted.ThenBy(e => e.End),
                _ => throw new ArgumentOutOfRangeException(),
            }
            : ThenBy switch
            {
                SearchFilter.Text => sorted.ThenByDescending(e => e.Text),
                SearchFilter.StrippedText => sorted.ThenByDescending(e => e.GetStrippedText()),
                SearchFilter.Style => sorted.ThenByDescending(e => e.Style),
                SearchFilter.Actor => sorted.ThenByDescending(e => e.Actor),
                SearchFilter.Effect => sorted.ThenByDescending(e => e.Effect),
                SearchFilter.Comment => sorted.ThenByDescending(e => e.IsComment),
                SearchFilter.Start => sorted.ThenByDescending(e => e.Start),
                SearchFilter.End => sorted.ThenByDescending(e => e.End),
                _ => throw new ArgumentOutOfRangeException(),
            };

        return sorted.ToList();
    }
}
