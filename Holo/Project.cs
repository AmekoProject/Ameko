// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.Json;
using AssCS;
using AssCS.IO;
using AssCS.Utilities;
using Holo.Configuration;
using Holo.Configuration.Migration;
using Holo.Models;
using Holo.Providers;
using Microsoft.Extensions.Logging;
using NaturalSort.Extension;

namespace Holo;

/// <summary>
/// A project consisting of multiple <see cref="Workspace"/>s,
/// <see cref="Style"/>s, and options common to the files within.
/// </summary>
/// <remarks>
/// <para>
/// A project primarily consists of multiple, potentially open,
/// <see cref="Document"/>s. Not all documents in the project need
/// be opened at the same time.
/// </para><para>
/// Additionally, all open files exist within a project. If the user
/// has not opened a project, the files implicitly are opened in the
/// default project.
/// </para><para>
/// Projects can be saved to disk with the <c>*.aproj</c> file extension.
/// </para>
/// </remarks>
public class Project : BindableBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true,
    };

    private readonly RangeObservableCollection<ProjectItem> _referencedItems;
    private readonly RangeObservableCollection<Workspace> _loadedWorkspaces;

    private readonly IWorkspaceFactory _workspaceFactory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    private Uri? _savePath;

    private int _docId = 1;
    private Workspace? _workingSpace;

    private uint? _cps;
    private bool? _cpsIncludesWhitespace;
    private bool? _cpsIncludesPunctuation;
    private bool? _useSoftLinebreaks;
    private int? _defaultLayer;
    private string? _spellcheckCulture;
    private readonly ObservableCollection<string> _customWords;
    private readonly RangeObservableCollection<Color> _colors;

    /// <summary>
    /// Denotes if the selection is currently changing
    /// </summary>
    /// <remarks>GUIs can use this property to determine if a change should be reported</remarks>
    public bool IsSelectionChanging { get; private set; } = true;

    /// <summary>
    /// All items referenced by the project
    /// </summary>
    public System.Collections.ObjectModel.ReadOnlyObservableCollection<ProjectItem> ReferencedItems { get; }

    /// <summary>
    /// Currently-open workspaces
    /// </summary>
    public System.Collections.ObjectModel.ReadOnlyObservableCollection<Workspace> LoadedWorkspaces { get; }

    /// <summary>
    /// The project's style manager
    /// </summary>
    public StyleManager StyleManager { get; }

    /// <summary>
    /// The path the <see cref="Document"/> is saved to,
    /// or <see langword="null"/> if the document has not been saved.
    /// </summary>
    [MemberNotNullWhen(true, nameof(IsFile))]
    public Uri? SavePath
    {
        get => _savePath;
        set => SetProperty(ref _savePath, value);
    }

    /// <summary>
    /// <see langword="true"/> if the <see cref="Project"/> has been saved and is up to date
    /// </summary>
    public bool IsSaved
    {
        get;
        internal set => SetProperty(ref field, value);
    }

    /// <summary>
    /// <see langword="true"/> if the <see cref="Project"/> has a save path
    /// </summary>
    public bool IsFile => _savePath is not null;

    /// <summary>
    /// Next available ID for documents/workspaces
    /// </summary>
    private int NextId => _docId++;

    /// <summary>
    /// Whether there is currently a loaded <see cref="Workspace"/>
    /// </summary>
    [MemberNotNullWhen(true, nameof(WorkingSpace))]
    public bool IsWorkspaceLoaded => WorkingSpace is not null;

    /// <summary>
    /// The currently-loaded workspace
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> if no workspace is loaded
    /// </remarks>
    public Workspace? WorkingSpace
    {
        get => _workingSpace;
        set
        {
            BeginSelectionChange();
            _workingSpace?.SelectionManager.BeginSelectionChange();
            _workingSpace?.MediaController.Stop();
            value?.SelectionManager.BeginSelectionChange();

            SetProperty(ref _workingSpace, value);
        }
    }

    /// <summary>
    /// Project-scoped characters-per-second threshold
    /// </summary>
    public uint? Cps
    {
        get => _cps;
        set
        {
            SetProperty(ref _cps, value);
            IsSaved = false;
        }
    }

    /// <summary>
    /// If whitespace should be included in <see cref="Event.Cps"/> calculation
    /// </summary>
    public bool? CpsIncludesWhitespace
    {
        get => _cpsIncludesWhitespace;
        set
        {
            SetProperty(ref _cpsIncludesWhitespace, value);
            IsSaved = false;
        }
    }

    /// <summary>
    /// If punctuation should be included in <see cref="Event.Cps"/> calculation
    /// </summary>
    public bool? CpsIncludesPunctuation
    {
        get => _cpsIncludesPunctuation;
        set
        {
            SetProperty(ref _cpsIncludesPunctuation, value);
            IsSaved = false;
        }
    }

    /// <summary>
    /// Workspace-scoped soft linebreaks preference
    /// </summary>
    public bool? UseSoftLinebreaks
    {
        get => _useSoftLinebreaks;
        set
        {
            SetProperty(ref _useSoftLinebreaks, value);
            IsSaved = false;
        }
    }

    /// <summary>
    /// Project-scoped default layer preference
    /// </summary>
    public int? DefaultLayer
    {
        get => _defaultLayer;
        set
        {
            SetProperty(ref _defaultLayer, value);
            IsSaved = false;
        }
    }

    /// <summary>
    /// Project-scoped spellcheck preference
    /// </summary>
    public string? SpellcheckCulture
    {
        get => _spellcheckCulture;
        set
        {
            SetProperty(ref _spellcheckCulture, value);
            IsSaved = false;
        }
    }

    /// <summary>
    /// Custom spellcheck words for the project
    /// </summary>
    public AssCS.Utilities.ReadOnlyObservableCollection<string> CustomWords => new(_customWords);

    /// <summary>
    /// Custom colors for the project
    /// </summary>
    public AssCS.Utilities.ReadOnlyObservableCollection<Color> Colors => new(_colors);

    /// <summary>
    /// Project-level timing configuration
    /// </summary>
    public TimingConfiguration Timing { get; } = new();

    /// <summary>
    /// Script configuration
    /// </summary>
    internal Dictionary<string, Dictionary<string, JsonElement>> ScriptConfiguration { get; } = [];

    /// <summary>
    /// Project title/name
    /// </summary>
    public string Title =>
        IsFile ? Path.GetFileNameWithoutExtension(SavePath!.LocalPath) : "Default Project";

    /// <summary>
    /// Get a loaded <see cref="Workspace"/> by ID
    /// </summary>
    /// <param name="id">ID of the desired workspace</param>
    /// <returns>The workspace with the given ID, or <see langword="null"/> if one does not exist</returns>
    public Workspace? GetWorkspace(int id)
    {
        return _loadedWorkspaces.FirstOrDefault(f => f.Id == id);
    }

    /// <summary>
    /// Add a new, empty <see cref="Workspace"/> to the project, and open it
    /// </summary>
    /// <param name="parentId">Optional ID for adding to a directory</param>
    /// <returns>The created workspace</returns>
    public Workspace AddWorkspace(int parentId = -1)
    {
        _logger.LogTrace("Creating a default workspace");
        var wsp = _workspaceFactory.Create(new Document(true), NextId);
        return AddWorkspace(wsp, parentId);
    }

    /// <summary>
    /// Add a new <see cref="Workspace"/> to the project using an existing <see cref="Document"/>, and open it
    /// </summary>
    /// <param name="document">Document to add</param>
    /// <param name="savePath">Save path of the document</param>
    /// <param name="parentId">Optional ID for adding to a directory</param>
    /// <returns>The created workspace</returns>
    public Workspace AddWorkspace(Document document, Uri savePath, int parentId = -1)
    {
        _logger.LogTrace("Creating a workspace from document at {Uri}", savePath);
        var wsp = _workspaceFactory.Create(document, NextId, savePath);
        return AddWorkspace(wsp, parentId);
    }

    /// <summary>
    /// Add a new <see cref="Workspace"/> to the project using an existing <see cref="Document"/>, and open it
    /// </summary>
    /// <param name="document">Document to add</param>
    /// <param name="parentId">Optional ID for adding to a directory</param>
    /// <returns>The created workspace</returns>
    public Workspace AddWorkspace(Document document, int parentId = -1)
    {
        _logger.LogTrace("Creating a workspace from document with no save path");
        var wsp = _workspaceFactory.Create(document, NextId);
        return AddWorkspace(wsp, parentId);
    }

    /// <summary>
    /// Add an existing <see cref="Workspace"/> to the project, and open it
    /// </summary>
    /// <param name="wsp">The workspace to add</param>
    /// <param name="parentId">Optional ID for adding to a directory</param>
    /// <returns>The workspace</returns>
    public Workspace AddWorkspace(Workspace wsp, int parentId = -1)
    {
        _logger.LogTrace(
            "Adding workspace {WspDisplayTitle}, parent: {ParentId}",
            wsp.DisplayTitle,
            parentId
        );
        var docItem = new DocumentItem
        {
            Id = wsp.Id,
            Workspace = wsp,
            Uri = wsp.SavePath,
        };

        if (parentId < 0)
        {
            _referencedItems.Add(docItem);
        }
        else
        {
            var parent = FindItemById(parentId);
            if (parent is not null)
                parent.Children.Add(docItem);
            else // Parent not found
                _referencedItems.Add(docItem);
        }

        BeginSelectionChange();
        _loadedWorkspaces.Add(wsp);
        WorkingSpace = wsp;
        if (IsFile)
            IsSaved = false;
        return wsp;
    }

    /// <summary>
    /// Add a virtual directory to the project
    /// </summary>
    /// <param name="name">Name of the directory</param>
    /// <param name="parentId">Optional ID for adding to a directory</param>
    /// <returns>The directory item</returns>
    public ProjectItem AddDirectory(string name, int parentId = -1)
    {
        _logger.LogTrace("Adding a new directory, parent: {ParentId}", parentId);
        var dirItem = new DirectoryItem { Id = NextId, Name = name };

        if (parentId < 0)
        {
            _referencedItems.Add(dirItem);
        }
        else
        {
            var parent = FindItemById(parentId);
            if (parent is not null)
                parent.Children.Add(dirItem);
            else // Parent not found
                _referencedItems.Add(dirItem);
        }

        IsSaved = false;
        return dirItem;
    }

    /// <summary>
    /// Remove a directory from the <see cref="ReferencedItems"/>
    /// </summary>
    /// <param name="id">ID of the directory</param>
    /// <returns><see langword="true"/> is successfully removed</returns>
    /// <remarks>Removing a directory will remove all subdirectories and documents!</remarks>
    public bool RemoveDirectory(int id)
    {
        _logger.LogTrace("Removing directory with id: {Id}", id);
        var dir = FindItemById(id);
        var result = dir?.Type == ProjectItemType.Directory && RemoveItemById(id);
        if (result && IsFile)
            IsSaved = false;
        return result;
    }

    /// <summary>
    /// Remove a <see cref="Workspace"/> from the project
    /// </summary>
    /// <param name="id">ID of the workspace to remove</param>
    /// <returns><see langword="true"/> if the workspace was removed</returns>
    /// <remarks>
    /// If the workspace being removed is the <see cref="WorkingSpace"/>, then
    /// the <see cref="WorkingSpace"/> will be set to any other currently open
    /// <see cref="Workspace"/>. If there are no other open workspaces, a new
    /// workspace will be created and selected.
    /// </remarks>
    public bool RemoveWorkspace(int id)
    {
        _logger.LogTrace("Removing workspace {Id}", id);
        if (WorkingSpace?.Id == id)
        {
            WorkingSpace =
                _loadedWorkspaces.Count > 1
                    ? _loadedWorkspaces.First(w => w.Id != id)
                    : AddWorkspace();
        }

        BeginSelectionChange();
        _loadedWorkspaces.RemoveAll(w => w.Id == id);
        var result = RemoveItemById(id);
        if (result && IsFile)
            IsSaved = false;
        return result;
    }

    /// <summary>
    /// Move a <see cref="ProjectItem"/> to a new parent
    /// </summary>
    /// <param name="id">ID of the item to move</param>
    /// <param name="parentId">ID of the new parent</param>
    /// <returns><see langword="true"/> on success</returns>
    public bool MoveToParent(int id, int parentId)
    {
        _logger.LogTrace("Moving {Id} to new parent {ParentId}", id, parentId);

        var item = FindItemById(id);
        var currentParent = FindParentItemById(id);

        if (item is null || parentId == id || (parentId < 0 && currentParent is null))
            return false;

        if (parentId < 0 && currentParent is not null)
        {
            _referencedItems.Add(item);
            currentParent.Children.RemoveAll(c => c.Id == id);
            return true;
        }

        var newParent = FindItemById(parentId);
        if (newParent?.Type != ProjectItemType.Directory)
            return false;

        newParent.Children.Add(item);
        (currentParent?.Children ?? _referencedItems).RemoveAll(c => c.Id == id);
        IsSaved = false;
        return true;
    }

    /// <summary>
    /// Open a referenced document
    /// </summary>
    /// <param name="id">ID of the document to open</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns>ID of the document or -1 on failure</returns>
    /// <remarks>
    /// A new <see cref="Workspace"/> containing the document and any supporting
    /// files will be created and set as the <see cref="WorkingSpace"/>
    /// </remarks>
    public Workspace? OpenDocument(int id)
    {
        _logger.LogTrace("Opening referenced document {Id}", id);
        try
        {
            var item = FindItemById(id);
            if (item is null || item.Type != ProjectItemType.Document)
            {
                _logger.LogError("A referenced document with id {Id} was not found", id);
                return null;
            }

            if (item.Type != ProjectItemType.Document)
            {
                _logger.LogError("Failed to parse document reference with id {Id}", id);
                return null;
            }

            if (!item.IsSavedToFileSystem)
            {
                _logger.LogError("Document item with id {Id} was not saved to file system", id);
                return null;
            }

            var ext = Path.GetExtension(item.Uri!.LocalPath);
            var document = ext switch
            {
                ".ass" or ".ssa" => new AssParser().Parse(_fileSystem, item.Uri!),
                ".srt" => new SrtParser().Parse(_fileSystem, item.Uri!),
                ".txt" => new TxtParser().Parse(_fileSystem, item.Uri!),
                _ => throw new ArgumentOutOfRangeException(nameof(id), "Invalid document type"),
            };

            BeginSelectionChange();
            item.Workspace = _workspaceFactory.Create(document, item.Id, item.Uri);

            if (ext is not (".ass" or ".ssa"))
            {
                // Non-ass sourced documents need to be re-saved as an ass file
                item.Workspace.SavePath = null;
                item.Workspace.IsSaved = false;
            }

            _loadedWorkspaces.Add(item.Workspace);
            WorkingSpace = item.Workspace;
            return item.Workspace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open document");
            return null;
        }
    }

    /// <summary>
    /// Add a custom word to the project
    /// </summary>
    /// <param name="word">Word to add</param>
    public void AddCustomWord(string word)
    {
        _customWords.Add(word);
        IsSaved = false;
        RaisePropertyChanged(nameof(CustomWords));
    }

    /// <summary>
    /// Remove a custom word from the project
    /// </summary>
    /// <param name="word">Word to remove</param>
    public void RemoveCustomWord(string word)
    {
        _customWords.Remove(word);
        IsSaved = false;
        RaisePropertyChanged(nameof(CustomWords));
    }

    /// <summary>
    /// Add a color to the project
    /// </summary>
    /// <param name="color">Color to add</param>
    public void AddColor(Color color)
    {
        _colors.Add(color);
        IsSaved = false;
        RaisePropertyChanged(nameof(Colors));
    }

    /// <summary>
    /// Remove a color from the project
    /// </summary>
    /// <param name="color">Color to remove</param>
    public void RemoveColor(Color color)
    {
        _colors.Remove(color);
        IsSaved = false;
        RaisePropertyChanged(nameof(Colors));
    }

    /// <summary>
    /// Close an open document
    /// </summary>
    /// <param name="id">ID of the document to close</param>
    /// <param name="replaceIfLast">Open a new document if this is the last one open</param>
    /// <returns><see langword="true"/> if the document was closed</returns>
    /// <remarks>
    ///<para>
    /// It is assumed that the caller has saved the file prior
    /// </para><para>
    /// If the document closed is in the <see cref="WorkingSpace"/>, then
    /// the <see cref="WorkingSpace"/> will be set to any other currently open
    /// <see cref="Workspace"/>. If there are no other open workspaces, a new
    /// workspace will be created and selected, so long as <paramref name="replaceIfLast"/>
    /// is <see langword="true"/>.
    /// </para>
    /// </remarks>
    public bool CloseDocument(int id, bool replaceIfLast = true)
    {
        _logger.LogTrace("Closing referenced document {Id}", id);

        // If the document is not saved anywhere, remove from the referenced list
        var isFileDoc = _loadedWorkspaces.FirstOrDefault(w => w.Id == id)?.SavePath is not null;
        if (!isFileDoc)
        {
            var referenced = FindItemById(id);
            if (referenced is not null)
                _referencedItems.Remove(referenced);
        }

        BeginSelectionChange();
        if (WorkingSpace?.Id != id)
            return _loadedWorkspaces.RemoveAll(w => w.Id == id) != 0;
        if (_loadedWorkspaces.Count > 1)
            WorkingSpace = _loadedWorkspaces.First(w => w.Id != id);
        else if (replaceIfLast)
            WorkingSpace = AddWorkspace();

        return _loadedWorkspaces.RemoveAll(w => w.Id == id) != 0;
    }

    /// <summary>
    /// Write the project to file
    /// </summary>
    /// <returns><see langword="true"/> if saving was successful</returns>
    /// <remarks>
    /// <see cref="SavePath"/> must be set prior to calling this method.
    /// </remarks>
    public bool Save()
    {
        if (SavePath is null)
            throw new InvalidOperationException("SavePath is null");

        var path = SavePath.LocalPath;
        _logger.LogInformation("Saving project {S} to {Path}", Title, path);

        var dir = _fileSystem.Path.GetDirectoryName(path) ?? string.Empty;

        try
        {
            if (!_fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

            using var fs = _fileSystem.FileStream.New(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            using var writer = new StreamWriter(fs);

            var model = new ProjectModel
            {
                Version = ProjectModelBase.CurrentApiVersion,
                ReferencedDocuments = ConvertToModels(_referencedItems, dir),
                Styles = StyleManager.Styles.Select(s => s.AsAss(AssVersion.V400P)).ToArray(), // TODO: Detect style version
                Colors = Colors.Select(c => c.AsStyleColor()).ToArray(),
                Cps = _cps,
                CpsIncludesWhitespace = _cpsIncludesWhitespace,
                CpsIncludesPunctuation = _cpsIncludesPunctuation,
                UseSoftLinebreaks = _useSoftLinebreaks,
                DefaultLayer = _defaultLayer,
                SpellcheckCulture = _spellcheckCulture,
                CustomWords = _customWords.ToArray(),
                Timing = new TimingModel
                {
                    LeadIn = Timing.LeadIn,
                    LeadOut = Timing.LeadOut,
                    SnapStartEarlierThreshold = Timing.SnapStartEarlierThreshold,
                    SnapStartLaterThreshold = Timing.SnapStartLaterThreshold,
                    SnapEndEarlierThreshold = Timing.SnapEndEarlierThreshold,
                    SnapEndLaterThreshold = Timing.SnapEndLaterThreshold,
                },
                ScriptConfiguration = ScriptConfiguration,
            };

            var content = JsonSerializer.Serialize(model, JsonOptions);
            writer.Write(content);
            IsSaved = true;
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogError(ex, "Failed to save project");
            return false;
        }
    }

    /// <summary>
    /// Find a referenced <see cref="ProjectItem"/> by ID
    /// </summary>
    /// <param name="id">ID of the item to find</param>
    /// <returns>The item, or <see langword="null"/> if not found</returns>
    /// <remarks>Uses breadth-first search</remarks>
    public ProjectItem? FindItemById(int id)
    {
        var queue = new Queue<ProjectItem>(_referencedItems);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Id == id)
                return current;

            if (current.Type != ProjectItemType.Directory)
                continue;

            foreach (var child in current.Children)
                queue.Enqueue(child);
        }

        return null;
    }

    /// <summary>
    /// Find the parent of a referenced <see cref="ProjectItem"/> by ID
    /// </summary>
    /// <param name="id">ID of the item to find the parent of</param>
    /// <returns>The item's parent, or <see langword="null"/> if root level or not found</returns>
    /// <remarks>Uses breadth-first search</remarks>
    public ProjectItem? FindParentItemById(int id)
    {
        var queue = new Queue<(ProjectItem item, ProjectItem? parent)>();
        foreach (var item in _referencedItems)
            queue.Enqueue((item, null));

        while (queue.Count > 0)
        {
            var (current, parent) = queue.Dequeue();

            if (current.Id == id)
                return parent;

            if (current.Type != ProjectItemType.Directory)
                continue;

            foreach (var child in current.Children)
                queue.Enqueue((child, current));
        }
        return null;
    }

    /// <summary>
    /// Get all <see cref="ProjectItem"/>s with the given <paramref name="type"/>
    /// </summary>
    /// <param name="type">Type of item to find</param>
    /// <returns>All items matching the type</returns>
    public List<ProjectItem> GetAllOfType(ProjectItemType type)
    {
        var result = new List<ProjectItem>();
        var queue = new Queue<ProjectItem>(_referencedItems);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Type == type)
                result.Add(current);

            if (current.Type != ProjectItemType.Directory)
                continue;

            foreach (var child in current.Children)
                queue.Enqueue(child);
        }

        return result;
    }

    /// <summary>
    /// Copy a <see cref="DocumentItem"/>
    /// </summary>
    /// <param name="source">Source item</param>
    /// <returns>Item with same content but different ID</returns>
    public DocumentItem Copy(DocumentItem source)
    {
        var newItem = new DocumentItem
        {
            Id = NextId,
            Uri = source.Uri,
            Name = source.Name,
            Workspace = source.Workspace,
        };
        _referencedItems.Add(newItem);
        IsSaved = false;
        return newItem;
    }

    /// <summary>
    /// Set the name and URI of an item in the project
    /// </summary>
    /// <param name="item">Item to modify</param>
    /// <param name="name">Name to set</param>
    /// <param name="uri">URI to set</param>
    public void SetNameAndUri(ProjectItem item, string name, Uri? uri)
    {
        item.Name = name;
        item.Uri = uri;
        IsSaved = false;
    }

    /// <summary>
    /// Get the relative path of a <see cref="DocumentItem"/> to the project
    /// </summary>
    /// <param name="item">Item to get the path of</param>
    /// <returns>The relative path between the <paramref name="item"/> and the project</returns>
    /// <remarks>Returns an empty string if a relative path is unavailable</remarks>
    public string GetRelativePath(ProjectItem item)
    {
        if (SavePath is null || item.Uri is null)
            return string.Empty;
        return PathExtensions.GetRelativePath(SavePath.LocalPath, item.Uri.LocalPath);
    }

    /// <summary>
    /// Sets <see cref="IsSelectionChanging"/> to <see langword="true"/>
    /// </summary>
    private void BeginSelectionChange()
    {
        IsSelectionChanging = true;
    }

    /// <summary>
    /// Sets <see cref="IsSelectionChanging"/> to <see langword="false"/>
    /// </summary>
    /// <remarks>
    /// Not called from within the <see cref="Project"/>.
    /// GUIs can use this method to signal that changes can be reported
    /// </remarks>
    public void EndSelectionChange()
    {
        IsSelectionChanging = false;
    }

    /// <summary>
    /// Remove a referenced <see cref="ProjectItem"/> by ID
    /// </summary>
    /// <param name="id">ID of the item to remove</param>
    /// <returns><see langword="true"/> if successfully removed</returns>
    /// <remarks>Uses breadth-first search</remarks>
    private bool RemoveItemById(int id)
    {
        var queue = new Queue<(ObservableCollection<ProjectItem> Parent, ProjectItem Item)>();

        // Seed the queue
        foreach (var item in _referencedItems)
        {
            queue.Enqueue((_referencedItems, item));
        }

        while (queue.Count > 0)
        {
            var (parent, current) = queue.Dequeue();

            if (current.Id == id)
                return parent.Remove(current);

            if (current.Type != ProjectItemType.Directory)
                continue;

            foreach (var item in current.Children)
                queue.Enqueue((current.Children, item));
        }

        return false;
    }

    /// <summary>
    /// Convert from <see cref="ProjectItem"/> to <see cref="ProjectItemModel"/>
    /// </summary>
    /// <param name="items">List of project items</param>
    /// <param name="dir">Directory for making relative paths</param>
    /// <returns>Array of <see cref="ProjectItemModel"/>s</returns>
    private static ProjectItemModel[] ConvertToModels(IList<ProjectItem> items, string dir)
    {
        return items
            .Where(item =>
                item is not { Type: ProjectItemType.Document, IsSavedToFileSystem: false }
            )
            .Select(item => new ProjectItemModel
            {
                Name = item.Name,
                Type = item.Type,
                RelativePath =
                    item.Type == ProjectItemType.Document
                        ? PathExtensions.GetRelativePath(dir, item.Uri!.LocalPath)
                        : null,
                Children =
                    item.Type == ProjectItemType.Directory
                        ? ConvertToModels(item.Children, dir).ToArray()
                        : [],
            })
            .ToArray();
    }

    /// <summary>
    /// Convert from <see cref="ProjectItemModel"/> to <see cref="ProjectItem"/>
    /// </summary>
    /// <param name="models">Array of models</param>
    /// <param name="dir">Directory for resolving relative paths</param>
    /// <param name="nextId">Initial ID</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns>List of <see cref="ProjectItem"/>s</returns>
    private static List<ProjectItem> ConvertFromModels(
        ProjectItemModel[] models,
        string dir,
        ref int nextId
    )
    {
        List<ProjectItem> result = [];

        foreach (var model in models)
        {
            result.Add(
                model.Type switch
                {
                    ProjectItemType.Document => new DocumentItem
                    {
                        Id = nextId++,
                        Name = model.Name,
                        Uri = model.RelativePath is not null
                            ? new Uri(Path.GetFullPath(Path.Combine(dir, model.RelativePath)))
                            : null,
                    },
                    ProjectItemType.Directory => new DirectoryItem
                    {
                        Id = nextId++,
                        Name = model.Name,
                        Children = new RangeObservableCollection<ProjectItem>(
                            ConvertFromModels(model.Children, dir, ref nextId)
                        ),
                    },
                    _ => throw new InvalidOperationException(nameof(model)),
                }
            );
        }

        return result;
    }

    /// <summary>
    /// Initialize a project
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    /// <param name="workspaceFactory">Factory for creating <see cref="Workspace"/>s</param>
    /// <param name="isEmpty">If the project should be created
    /// without a default <see cref="Workspace"/></param>
    internal Project(
        IFileSystem fileSystem,
        ILogger<Project> logger,
        IWorkspaceFactory workspaceFactory,
        bool isEmpty = false
    )
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _workspaceFactory = workspaceFactory;

        _referencedItems = [];
        _loadedWorkspaces = [];
        _customWords = [];
        _colors = [];
        StyleManager = new StyleManager();

        ReferencedItems =
            new System.Collections.ObjectModel.ReadOnlyObservableCollection<ProjectItem>(
                _referencedItems
            );
        LoadedWorkspaces =
            new System.Collections.ObjectModel.ReadOnlyObservableCollection<Workspace>(
                _loadedWorkspaces
            );

        IsSaved = true;
        if (isEmpty)
            return;

        var id = NextId;
        var defaultWorkspace = _workspaceFactory.Create(new Document(true), id);
        var defaultLink = new DocumentItem { Id = id, Workspace = defaultWorkspace };

        _referencedItems.Add(defaultLink);
        _loadedWorkspaces.Add(defaultWorkspace);
        _workingSpace = defaultWorkspace;
    }

    /// <summary>
    /// Initialize a Project
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    /// <param name="workspaceFactory">Factory for creating <see cref="Workspace"/>s</param>
    /// <param name="uri">Path the project is saved to</param>
    /// <exception cref="FileNotFoundException">If the project file was not found</exception>
    /// <exception cref="InvalidDataException">If the project file is malformed</exception>
    internal Project(
        IFileSystem fileSystem,
        ILogger<Project> logger,
        IWorkspaceFactory workspaceFactory,
        Uri uri
    )
        : this(fileSystem, logger, workspaceFactory, isEmpty: true)
    {
        var path = uri.LocalPath;
        // We are loading a project file
        if (fileSystem.File.Exists(path))
        {
            logger.LogInformation("Parsing project {Path}", path);
            _savePath = uri;
            try
            {
                var dir = fileSystem.Path.GetDirectoryName(path) ?? string.Empty;

                if (!fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                    fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

                if (!fileSystem.File.Exists(path))
                    throw new FileNotFoundException($"Project {path} was not found");

                using var fs = fileSystem.FileStream.New(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );

                using var reader = new StreamReader(fs);
                var content = reader.ReadToEnd();
                var model = ProjectMigrator.MigrateToCurrent(content);

                if (model is null)
                    throw new InvalidDataException("Project model migration failed");

                // De-relative the file paths in the project
                _referencedItems.AddRange(
                    ConvertFromModels(model.ReferencedDocuments, dir, ref _docId)
                );
                _colors.AddRange(model.Colors.Select(Color.FromAss));
                _cps = model.Cps;
                _cpsIncludesWhitespace = model.CpsIncludesWhitespace;
                _cpsIncludesPunctuation = model.CpsIncludesPunctuation;
                _defaultLayer = model.DefaultLayer;
                _useSoftLinebreaks = model.UseSoftLinebreaks;
                _spellcheckCulture = model.SpellcheckCulture;
                _customWords = new ObservableCollection<string>(model.CustomWords);

                Timing = new TimingConfiguration
                {
                    LeadIn = model.Timing.LeadIn,
                    LeadOut = model.Timing.LeadOut,
                    SnapStartEarlierThreshold = model.Timing.SnapStartEarlierThreshold,
                    SnapStartLaterThreshold = model.Timing.SnapStartLaterThreshold,
                    SnapEndEarlierThreshold = model.Timing.SnapEndEarlierThreshold,
                    SnapEndLaterThreshold = model.Timing.SnapEndLaterThreshold,
                };

                ScriptConfiguration = model.ScriptConfiguration;

                model
                    .Styles.Select(s => Style.FromAss(StyleManager.NextId, s, AssVersion.V400P)) // TODO: Detect style version
                    .Where(s => s is not null)
                    .ToList()
                    .ForEach(StyleManager.Add!);

                IsSaved = true;
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                logger.LogError(ex, "Failed to parse project");
            }
        }
        // We are loading a directory as a project
        else if (fileSystem.Directory.Exists(path))
        {
            logger.LogInformation("Generating project from directory {Path}...", path);
            var fileCount = 0;
            var dirCount = 0;
            var stack = new Stack<(string, ObservableCollection<ProjectItem>)>();
            stack.Push((path, _referencedItems));

            while (stack.Count > 0)
            {
                var (currentPath, currentCollection) = stack.Pop();

                // Subdirectories
                var sortedDirectories = fileSystem
                    .Directory.EnumerateDirectories(currentPath)
                    .OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase.WithNaturalSort());
                foreach (var subDirectory in sortedDirectories)
                {
                    var dirName = fileSystem.Path.GetFileName(subDirectory);

                    // Skip .directories
                    if (dirName.StartsWith('.'))
                    {
                        logger.LogTrace("Skipping .directory {SubDirectory}", subDirectory);
                        continue;
                    }
                    // Skip directories that don't have subdirectories or ass files
                    if (
                        fileSystem.Directory.GetDirectories(subDirectory).Length == 0
                        && fileSystem.Directory.GetFiles(subDirectory, "*.ass").Length == 0
                        && fileSystem.Directory.GetFiles(subDirectory, "*.srt").Length == 0
                    )
                    {
                        logger.LogTrace(
                            "Skipping {SubDirectory} as it doesn't contain relevant files",
                            subDirectory
                        );
                        continue;
                    }

                    var dirItem = new DirectoryItem { Id = NextId, Name = dirName };
                    currentCollection.Add(dirItem);

                    stack.Push((subDirectory, dirItem.Children));
                    dirCount++;
                }

                // Files
                var sortedFiles = fileSystem
                    .Directory.EnumerateFiles(currentPath, "*.ass")
                    .Concat(fileSystem.Directory.EnumerateFiles(currentPath, "*.srt"))
                    .OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase.WithNaturalSort());
                foreach (var file in sortedFiles)
                {
                    var docItem = new DocumentItem { Id = NextId, Uri = new Uri(file) };
                    currentCollection.Add(docItem);
                    fileCount++;
                }
            }
            logger.LogInformation(
                "Done! Project contains {DirCount} directories and {FileCount} files",
                dirCount,
                fileCount
            );
        }

        if (_referencedItems.Count == 0)
            AddWorkspace();
    }
}
