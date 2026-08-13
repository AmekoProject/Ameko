// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Linq;
using Ameko.ViewModels.Controls;
using AssCS;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;

namespace Ameko.Views.Controls;

public partial class TabItemEventsArea : ReactiveUserControl<TabItemViewModel>
{
    private void DoScrollToAndSelectEvent(IInteractionContext<Event, RxVoid> interaction)
    {
        EventsGrid.ScrollIntoView(interaction.Input, null);
        EventsGrid.SelectedItem = interaction.Input;
        interaction.SetOutput(RxVoid.Default);
    }

    private void DoSelectEvents(IInteractionContext<IList<Event>, RxVoid> interaction)
    {
        interaction.SetOutput(RxVoid.Default);
        if (interaction.Input.Count < 2)
            return;

        EventsGrid.SelectedItems.Clear();

        // No AddRange :pensive:
        foreach (var @event in interaction.Input)
            EventsGrid.SelectedItems.Add(@event);
    }

    public TabItemEventsArea()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is not { } vm)
                return;

            vm.ScrollToAndSelectEvent.RegisterHandler(DoScrollToAndSelectEvent)
                .DisposeWith(disposables);
            vm.SelectEvents.RegisterHandler(DoSelectEvents).DisposeWith(disposables);

            EventsGrid.AddHandler(
                DataGrid.SelectionChangedEvent,
                DataGrid_OnSelectionChanged,
                RoutingStrategies.Bubble
            );

            EventsGrid.AddHandler(
                DataGrid.DoubleTappedEvent,
                DataGrid_OnDoubleTapped,
                RoutingStrategies.Bubble
            );
        });
    }

    private void DataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var active = (Event)EventsGrid.SelectedItem;
        if (active is null)
            return;
        var selection = EventsGrid.SelectedItems.Cast<Event>().ToList();
        // Side effect: set SelectionChanging to true to prevent Commit() calls
        ViewModel?.Workspace.SelectionManager.Select(active, selection);
        ViewModel?.Workspace.MediaController.AutoSeekTo(active);

        EventsGrid.ScrollIntoView(active, null);

        // Mark the selection change complete (on the UI thread)
        // to re-allow Commit() calls
        Dispatcher.UIThread.Post(
            () =>
            {
                ViewModel?.Workspace.SelectionManager.EndSelectionChange();
                ViewModel?.ProjectProvider.Current.EndSelectionChange();
            },
            DispatcherPriority.Background
        );
    }

    private void DataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (!ViewModel?.Workspace.MediaController.IsVideoLoaded == true)
            return;

        var active = (Event)EventsGrid.SelectedItem;
        if (active is null)
            return;

        ViewModel?.Workspace.MediaController.SeekTo(active);
    }
}
