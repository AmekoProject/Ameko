// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Holo.Models;
using Holo.Providers;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace Ameko.ViewModels.Windows;

public class LogWindowViewModel : ViewModelBase
{
    private readonly ILogProvider _logProvider;

    public ICommand CopySelectionCommand { get; }
    public Interaction<string, RxVoid> CopySelection { get; }

    public object? SelectedLog
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public List<LogEntry> SelectedLogs { get; set; } = [];

    public ReadOnlyObservableCollection<LogEntry> LogEntries => _logProvider.LogEntries;

    public LogWindowViewModel(ILogProvider logProvider)
    {
        _logProvider = logProvider;
        _logProvider.LogEntries.CollectionChanged += (_, args) =>
        {
            if (args.NewItems is not null && args.NewItems.Count > 0)
                SelectedLog = args.NewItems[^1];
        };

        CopySelection = new Interaction<string, RxVoid>();
        CopySelectionCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedLogs.Count == 0)
                return;
            var logs = SelectedLogs.OrderBy(l => l.Timestamp).Select(l => l.ToString());
            await CopySelection.Handle(string.Join(Environment.NewLine, logs));
        });
    }
}
