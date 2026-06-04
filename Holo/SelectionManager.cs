// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using AssCS;

namespace Holo;

/// <summary>
/// Provides methods for managing event selections in a <see cref="Workspace"/>
/// </summary>
public class SelectionManager : BindableBase
{
    private Event _activeEvent;
    private readonly RangeObservableCollection<Event> _selectedEventCollection;

    /// <summary>
    /// Denotes if the selection is currently changing
    /// </summary>
    /// <remarks>GUIs can use this property to determine if a change should be reported</remarks>
    public bool IsSelectionChanging { get; private set; } = true;

    /// <summary>
    /// Sets <see cref="IsSelectionChanging"/> to <see langword="true"/>
    /// </summary>
    public void BeginSelectionChange()
    {
        IsSelectionChanging = true;
    }

    /// <summary>
    /// Sets <see cref="IsSelectionChanging"/> to <see langword="false"/>
    /// </summary>
    /// <remarks>
    /// Not called from within the <see cref="SelectionManager"/>.
    /// GUIs can use this method to signal that changes can be reported
    /// </remarks>
    public void EndSelectionChange()
    {
        IsSelectionChanging = false;
    }

    /// <summary>
    /// The currently-selected event.
    /// Setting this value sets <see cref="IsSelectionChanging"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// If there are multiple selected events, then <see cref="ActiveEvent"/>
    /// will be the "primary" selected event. <see cref="SelectedEventCollection"/>
    /// will contain the entire selection.
    /// </remarks>
    public Event ActiveEvent
    {
        get => _activeEvent;
        private set
        {
            BeginSelectionChange();
            SetProperty(ref _activeEvent, value);
        }
    }

    /// <summary>
    /// Collection of currently-selected events
    /// </summary>
    public ReadOnlyObservableCollection<Event> SelectedEventCollection { get; }

    /// <summary>
    /// Set the active and currently-selected events
    /// </summary>
    /// <param name="active">Event to set as active</param>
    public void Select(Event active)
    {
        Select(active, [active]);
    }

    /// <summary>
    /// Set the active and currently-selected events.
    /// Calling this method sets <see cref="IsSelectionChanging"/> to <see langword="true"/>.
    /// </summary>
    /// <param name="active">Event to set as active</param>
    /// <param name="selection">Collection of all selected events</param>
    public void Select(Event active, IList<Event> selection)
    {
        if (IsSelectionChanging)
            return;

        if (active == ActiveEvent && selection.Count == SelectedEventCollection.Count)
            return;

        BeginSelectionChange();

        _activeEvent = active;
        _selectedEventCollection.ReplaceRange(selection);
        RaisePropertyChanged(nameof(ActiveEvent));
        RaisePropertyChanged(nameof(SelectedEventCollection));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Forcefully set the active and currently-selected events. (Bypass no-op check)
    /// Calling this method sets <see cref="IsSelectionChanging"/> to <see langword="true"/>.
    /// </summary>
    /// <param name="active">Event to set as active</param>
    /// <param name="selection">Collection of all selected events</param>
    public void ForceSelect(Event active, IList<Event> selection)
    {
        BeginSelectionChange();
        _activeEvent = active;
        _selectedEventCollection.ReplaceRange(selection);
        RaisePropertyChanged(nameof(ActiveEvent));
        RaisePropertyChanged(nameof(SelectedEventCollection));
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Initialize the selection manager
    /// </summary>
    /// <param name="initialSelection">Event to select initially</param>
    public SelectionManager(Event initialSelection)
    {
        _activeEvent = initialSelection;
        _selectedEventCollection = [initialSelection];
        SelectedEventCollection = new ReadOnlyObservableCollection<Event>(_selectedEventCollection);
    }

    /// <summary>
    /// Event raised when the current selection changes
    /// </summary>
    public event EventHandler<EventArgs>? SelectionChanged;
}
