// SPDX-License-Identifier: MPL-2.0

using AssCS;
using AssCS.History;
using Holo.Configuration;
using Microsoft.Extensions.Logging;

namespace Holo;

/// <summary>
/// A group of related files for editing, part of a <see cref="Project"/>
/// </summary>
/// <remarks>
/// A workspace is the related content normally displayed in a single
/// tab in the editor, which includes a <see cref="Document"/>, and optionally
/// supporting files, like video or audio.
/// </remarks>
public class Workspace : BindableBase
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private FileSystemWatcher? _fileSystemWatcher;
    private DateTimeOffset? _lastWriteTime;
    private DateTimeOffset? _lastExternalModificationAlertTime;

    private Uri? _savePath;

    /// <summary>
    /// The ass document in the workspace
    /// </summary>
    public Document Document { get; }

    /// <summary>
    /// The ID of the workspace
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Manages selections within the workspace
    /// </summary>
    public SelectionManager SelectionManager { get; }

    /// <summary>
    /// Manages audio/video
    /// </summary>
    public MediaController MediaController { get; }

    /// <summary>
    /// Manages the closed captions attached to the workspace
    /// </summary>
    public ReferenceFileManager ReferenceFileManager { get; }

    /// <summary>
    /// The path the <see cref="Document"/> is saved to,
    /// or <see langword="null"/> if the document has not been saved.
    /// </summary>
    public Uri? SavePath
    {
        get => _savePath;
        set
        {
            SetProperty(ref _savePath, value);
            RaisePropertyChanged(nameof(DisplayTitle));

            if (_fileSystemWatcher is null && value is not null)
            {
                _fileSystemWatcher = new FileSystemWatcher(
                    Path.GetDirectoryName(value.LocalPath) ?? "/",
                    Path.GetFileName(value.LocalPath)
                );
                _fileSystemWatcher.NotifyFilter =
                    NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
                _fileSystemWatcher.EnableRaisingEvents = true;
                _fileSystemWatcher.Changed += FileSystemWatcherOnChanged;
                _fileSystemWatcher.Renamed += FileSystemWatcherOnChanged;
            }
        }
    }

    /// <summary>
    /// <see langword="true"/> if the <see cref="Document"/> has been saved and is up to date
    /// </summary>
    public bool IsSaved
    {
        get;
        set
        {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(DisplayTitle));

            IsAutosaved = value; // Reset the autosave flag if a manual save or change was made

            if (value)
                _lastWriteTime = DateTimeOffset.Now;
        }
    }

    /// <summary>
    /// <see langword="true"/> if the <see cref="Document"/> has been autosaved
    /// </summary>
    public bool IsAutosaved { get; set; }

    /// <summary>
    /// Fallback title to use when not saved to a file
    /// </summary>
    public string FallbackTitle { get; set; }

    /// <summary>
    /// Title of this workspace
    /// </summary>
    public string Title =>
        SavePath is not null ? Path.GetFileNameWithoutExtension(SavePath.LocalPath) : FallbackTitle;

    /// <summary>
    /// Title to display in the GUI
    /// </summary>
    /// <remarks>Title is prefixed with <c>*</c> if there are unsaved changes</remarks>
    public string DisplayTitle =>
        SavePath is not null
            ? $"{(!IsSaved ? '*' : string.Empty)}{Path.GetFileNameWithoutExtension(SavePath.LocalPath)}"
            : $"{(!IsSaved ? '*' : string.Empty)}{FallbackTitle}";

    /// <summary>
    /// Whether the Actors column in the events grid should be displayed
    /// </summary>
    public bool DisplayActorsColumn => Document.EventManager.Actors.Count > 0;

    /// <summary>
    /// Whether the Effects column in the events grid should be displayed
    /// </summary>
    public bool DisplayEffectsColumn => Document.EventManager.Effects.Count > 0;

    /// <summary>
    /// Commit a change
    /// </summary>
    /// <param name="active">Active event (<see cref="SelectionManager.ActiveEvent"/>)</param>
    /// <param name="changeType">Type of change</param>
    /// <param name="amend">Force amend</param>
    public void Commit(Event active, ChangeType changeType, bool amend = false)
    {
        Commit(active, [active], changeType, amend);
    }

    /// <summary>
    /// Commit a change
    /// </summary>
    /// <param name="selection">Entire selection (<see cref="SelectionManager.SelectedEventCollection"/>)</param>
    /// <param name="changeType">Type of change</param>
    /// <param name="amend">Force amend</param>
    public void Commit(IList<Event> selection, ChangeType changeType, bool amend = false)
    {
        Commit(null, selection, changeType, amend);
    }

    /// <summary>
    /// Commit a change
    /// </summary>
    /// <param name="active">Active event (<see cref="SelectionManager.ActiveEvent"/>)</param>
    /// <param name="selection">Entire selection (<see cref="SelectionManager.SelectedEventCollection"/>)</param>
    /// <param name="changeType">Type of change</param>
    /// <param name="amend">Force amend</param>
    /// <remarks>
    /// If supplied, changes will be propagated from <paramref name="active"/> to the rest
    /// of the <paramref name="selection"/> based on the user's <see cref="Configuration"/>.
    /// </remarks>
    public void Commit(
        Event? active,
        IList<Event> selection,
        ChangeType changeType,
        bool amend = false
    )
    {
        // See: SubsEditBox::SetSelectedRows
        // https://github.com/arch1t3cht/Aegisub/blob/b2a0b098215d7028ba26f1bf728731fc585f2b99/src/subs_edit_box.cpp#L476

        amend = ShouldAmend();
        _logger.LogTrace(
            "Commiting {Count} events under change {Type} (amend={Amend})",
            selection.Count,
            changeType,
            amend
        );

        // Propagation, if applicable
        if (
            active is not null
            && selection.Count > 1
            && _configuration.PropagateFields is not PropagateFields.None
        )
        {
            var progenitor = Document.HistoryManager.CanUndo
                ? Document.HistoryManager.PeekHistory().Events.GetValueOrDefault(active.Id) ?? null
                : null;
            progenitor ??= new Event(-1); // Default state
            PropagateChanges(selection, progenitor, active, _configuration.PropagateFields);
        }

        var selectionIds = !amend ? selection.Select(e => e.Id).ToList() : null;

        if (changeType is ChangeType.ModifyEventText or ChangeType.ModifyEventMeta)
        {
            foreach (var @event in selection)
            {
                Document.HistoryManager.Commit(changeType, @event, amend, selectionIds);
                amend = true;
            }
        }
        else
        {
            Document.HistoryManager.Commit(changeType, selectionIds);
        }

        IsSaved = false;
        return;

        bool ShouldAmend()
        {
            if (!Document.HistoryManager.CanUndo)
                return false;
            if (amend)
                return true;
            if (changeType is not (ChangeType.ModifyEventText or ChangeType.ModifyEventMeta))
                return false;
            if (Document.HistoryManager.LastCommitTime.AddSeconds(15) < DateTimeOffset.Now)
                return false;

            var peek = Document.HistoryManager.PeekHistory();

            if (peek.Type != changeType)
                return false;
            if (peek.Selection?.Count != selection.Count)
                return false;

            return peek.Selection.Count <= 0 || peek.Selection[0] == selection[0].Id;
        }
    }

    /// <summary>
    /// Perform an Undo history operation, if legal
    /// </summary>
    public void Undo()
    {
        if (!Document.HistoryManager.CanUndo)
            return;
        SelectionManager.BeginSelectionChange();
        _logger.LogTrace("Undoing");

        if (!Document.HistoryManager.Undo(out var _, out var undone))
        {
            _logger.LogError("Undo failed");
            return;
        }

        var currentSelection = SelectionManager.SelectedEventCollection.Select(e => e.Id).ToList();
        var shouldChangeSelection = !(undone.Selection ?? []).SequenceEqual(currentSelection); // cross-reference undone commit's selection
        var selectionIds = shouldChangeSelection ? undone.Selection ?? [] : currentSelection;

        // Change selection to the committed state
        var selection = selectionIds
            .Where(id => Document.EventManager.TryGet(id, out _)) // use applied commit's events
            .Select(id => Document.EventManager.Get(id))
            .ToList();

        if (selection.Count > 0)
            SelectionManager.ForceSelect(selection[0], selection);
    }

    /// <summary>
    /// Perform a Redo history operation, if legal
    /// </summary>
    public void Redo()
    {
        if (!Document.HistoryManager.CanRedo)
            return;
        SelectionManager.BeginSelectionChange();
        _logger.LogTrace("Redoing");

        if (!Document.HistoryManager.Redo(out var commit))
        {
            _logger.LogError("Undo failed");
            return;
        }

        var currentSelection = SelectionManager.SelectedEventCollection.Select(e => e.Id).ToList();
        var shouldChangeSelection = !(commit.Selection ?? []).SequenceEqual(currentSelection);
        var selectionIds = shouldChangeSelection ? commit.Selection ?? [] : currentSelection;

        // Change selection to the committed state
        var selection = selectionIds
            .Where(id => Document.EventManager.TryGet(id, out _)) // use applied commit's events
            .Select(id => Document.EventManager.Get(id))
            .ToList();

        if (selection.Count > 0)
            SelectionManager.ForceSelect(selection[0], selection);
    }

    /// <summary>
    /// Propagate changes made to the active event to the other selected events
    /// </summary>
    /// <param name="selection">Selected events</param>
    /// <param name="progenitor">Initial state of the active event</param>
    /// <param name="child">Current state of the active event</param>
    /// <param name="fields">Which fields should be propagated</param>
    private static void PropagateChanges(
        IEnumerable<Event> selection,
        Event progenitor,
        Event child,
        PropagateFields fields
    )
    {
        if (fields == PropagateFields.None)
            return;

        foreach (var e in selection)
        {
            if (progenitor.IsComment != child.IsComment)
                e.IsComment = child.IsComment;
            if (progenitor.Layer != child.Layer)
                e.Layer = child.Layer;
            if (progenitor.Start != child.Start)
                e.Start = child.Start;
            if (progenitor.End != child.End)
                e.End = child.End;
            if (progenitor.Style != child.Style)
                e.Style = child.Style;
            if (progenitor.Actor != child.Actor)
                e.Actor = child.Actor;
            if (progenitor.Effect != child.Effect)
                e.Effect = child.Effect;
            if (progenitor.Margins.Left != child.Margins.Left)
                e.Margins.Left = child.Margins.Left;
            if (progenitor.Margins.Right != child.Margins.Right)
                e.Margins.Right = child.Margins.Right;
            if (progenitor.Margins.Vertical != child.Margins.Vertical)
                e.Margins.Vertical = child.Margins.Vertical;

            if (fields == PropagateFields.NonText)
                continue;

            if (progenitor.Text != child.Text)
                e.Text = child.Text;
        }
    }

    /// <summary>
    /// Listen for updates from the <see cref="_fileSystemWatcher"/>
    /// </summary>
    /// <param name="sender">The FileSystemWatcher</param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void FileSystemWatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        if (
            _lastWriteTime is not null
            && DateTimeOffset.Now - _lastWriteTime <= TimeSpan.FromSeconds(5)
        )
            return;
        if (
            _lastExternalModificationAlertTime is not null
            && DateTimeOffset.Now - _lastExternalModificationAlertTime <= TimeSpan.FromSeconds(15)
        )
            return;
        // Also alert if last write time is null
        _lastExternalModificationAlertTime = DateTimeOffset.Now;
        FileModifiedExternally?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Initialize a Workspace
    /// </summary>
    /// <param name="document">Document in the workspace</param>
    /// <param name="id">Unique ID</param>
    /// <param name="savePath"><paramref name="document"/>'s save path, if applicable</param>
    /// <param name="logger">Logger</param>
    /// <param name="configuration">Configuration</param>
    /// <param name="mediaController">MediaController to use</param>
    internal Workspace(
        Document document,
        int id,
        Uri? savePath,
        ILogger<Workspace> logger,
        IConfiguration configuration,
        MediaController mediaController
    )
    {
        Document = document;
        MediaController = mediaController;
        Id = id;
        FallbackTitle = $"New {Id}";
        IsSaved = true;

        _savePath = savePath;
        _logger = logger;
        _configuration = configuration;

        if (_savePath is not null)
        {
            _fileSystemWatcher = new FileSystemWatcher(
                Path.GetDirectoryName(_savePath.LocalPath) ?? "/",
                Path.GetFileName(_savePath.LocalPath)
            );
            _fileSystemWatcher.NotifyFilter =
                NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
            _fileSystemWatcher.Changed += FileSystemWatcherOnChanged;
            _fileSystemWatcher.Renamed += FileSystemWatcherOnChanged;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        SelectionManager = new SelectionManager(Document.EventManager.Head);
        ReferenceFileManager = new ReferenceFileManager(SelectionManager);

        SelectionManager.SelectionChanged += (_, _) =>
        {
            RaisePropertyChanged(nameof(DisplayActorsColumn));
            RaisePropertyChanged(nameof(DisplayEffectsColumn));
        };

        // TODO: Should this be here or elsewhere?
        document.HistoryManager.ChangeMade += (_, _) => MediaController.SetSubtitles(document);
    }

    /// <summary>
    /// Event raised if the file represented by this workspace is modified outside the current program
    /// </summary>
    public event EventHandler<EventArgs>? FileModifiedExternally;
}
