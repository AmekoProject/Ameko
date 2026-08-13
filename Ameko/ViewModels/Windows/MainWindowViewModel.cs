// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ameko.Messages;
using Ameko.Services;
using Ameko.Utilities;
using Ameko.ViewModels.Controls;
using Ameko.ViewModels.Dialogs;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Holo;
using Holo.Configuration;
using Holo.Configuration.Keybinds;
using Holo.Media.Providers;
using Holo.Providers;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IViewModelFactory _vmFactory;
    private readonly IMessageService _messageService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ISpellcheckService _spellcheckService;
    private readonly IDictionaryService _dictionaryService;
    private readonly ITabFactory _tabFactory;
    private readonly ILogger _logger;

    public IConfiguration Configuration { get; }
    public IPersistence Persistence { get; }
    public IScriptService ScriptService { get; }
    public IKeybindService KeybindService { get; }
    public ILayoutProvider LayoutProvider { get; }

    public bool DisplayInWindowMenu { get; } = !OperatingSystem.IsMacOS();

    #region Interactions
    // File
    public Interaction<RxVoid, Uri[]?> OpenSubtitle { get; }
    public Interaction<string, Uri?> SaveSubtitleAs { get; }
    public Interaction<string, Uri?> ExportSubtitle { get; }
    public Interaction<RxVoid, Uri?> OpenProject { get; }
    public Interaction<RxVoid, Uri?> OpenFolderAsProject { get; }
    public Interaction<string, Uri?> SaveProjectAs { get; }

    // Edit
    public Interaction<SearchDialogViewModel, RxVoid> ShowSearchDialog { get; }
    public Interaction<SpellcheckDialogViewModel, RxVoid> ShowSpellcheckDialog { get; }

    // Subtitle
    public Interaction<StylesManagerWindowViewModel, RxVoid> ShowStylesManager { get; }
    public Interaction<RxVoid, Uri?> AttachReferenceFile { get; }
    public Interaction<ScriptInfoDialogViewModel, RxVoid> ShowScriptInfoDialog { get; }
    public Interaction<ProfilerDialogViewModel, RxVoid> ShowProfilerDialog { get; }
    public Interaction<SortDialogViewModel, RxVoid> ShowSortDialog { get; }
    public Interaction<SelectDialogViewModel, RxVoid> ShowSelectDialog { get; }

    // Project
    public Interaction<KnpWindowViewModel, RxVoid> ShowKnpWindow { get; }
    public Interaction<ProjectConfigDialogViewModel, RxVoid> ShowProjectConfigDialog { get; }

    // Timing
    public Interaction<ShiftTimesDialogViewModel, RxVoid> ShowShiftTimesDialog { get; }

    // Video
    public Interaction<RxVoid, Uri?> OpenVideo { get; }
    public Interaction<RxVoid, Uri?> OpenKeyframes { get; }
    public Interaction<JumpDialogViewModel, JumpDialogClosedMessage?> ShowJumpDialog { get; }

    // Audio
    public Interaction<RxVoid, Uri?> OpenAudio { get; }

    // Scripts
    public Interaction<PkgManWindowViewModel, RxVoid> ShowPackageManager { get; }
    public Interaction<PlaygroundWindowViewModel, RxVoid> ShowPlaygroundWindow { get; }

    // Help
    public Interaction<HelpWindowViewModel, RxVoid> ShowHelpWindow { get; }
    public Interaction<LogWindowViewModel, RxVoid> ShowLogWindow { get; }
    public Interaction<AboutWindowViewModel, RxVoid> ShowAboutWindow { get; }
    public Interaction<ConfigDialogViewModel, RxVoid> ShowConfigDialog { get; }
    public Interaction<KeybindsDialogViewModel, RxVoid> ShowKeybindsDialog { get; }
    public Interaction<RxVoid, RxVoid> OpenIssueTracker { get; }

    // Other
    public Interaction<CommandPaletteDialogViewModel, RxVoid> ShowCommandPaletteDialog { get; }
    public Interaction<RxVoid, RxVoid> ToggleFullscreen { get; }
    public Interaction<
        InstallDictionaryDialogViewModel,
        RxVoid
    > ShowInstallDictionaryDialog { get; }
    public Interaction<
        SelectFolderDialogViewModel,
        SelectFolderMessage?
    > ShowSelectFolderDialog { get; }
    #endregion

    #region Commands
    // File
    [Command("ameko.document.new", "Ctrl+N", KeybindContext.Global)]
    public ICommand NewCommand { get; }

    [Command("ameko.document.open", "Ctrl+O", KeybindContext.Global)]
    public ICommand OpenSubtitleCommand { get; }
    public ICommand OpenSubtitleNoGuiCommand { get; }
    public ICommand OpenSubtitlesNoGuiCommand { get; }

    [Command("ameko.document.save", "Ctrl+S", KeybindContext.Global)]
    public ICommand SaveSubtitleCommand { get; }

    [Command("ameko.document.saveAs", "Ctrl+Shift+S", KeybindContext.Global)]
    public ICommand SaveSubtitleAsCommand { get; }

    [Command("ameko.document.export", KeybindContext.Global)]
    public ICommand ExportSubtitleCommand { get; }
    public ICommand ClearRecentSubtitlesCommand { get; }

    [Command("ameko.project.open", KeybindContext.Global)]
    public ICommand OpenProjectCommand { get; }
    public ICommand OpenProjectNoGuiCommand { get; }

    [Command("ameko.project.openFolder", KeybindContext.Global)]
    public ICommand OpenFolderAsProjectCommand { get; }

    [Command("ameko.project.save", KeybindContext.Global)]
    public ICommand SaveProjectCommand { get; }
    public ICommand ClearRecentProjectsCommand { get; }

    [Command("ameko.workspace.close", "Ctrl+W", KeybindContext.Global)]
    public ICommand CloseTabCommand { get; }

    [Command("ameko.project.close", KeybindContext.Global)]
    public ICommand CloseProjectCommand { get; }

    [Command("ameko.application.quit", "Ctrl+Q", KeybindContext.Global)]
    public ICommand QuitCommand { get; }

    // Edit
    [Command("ameko.document.undo", "Ctrl+Z", KeybindContext.Global)]
    public ICommand UndoCommand { get; }

    [Command("ameko.document.redo", "Ctrl+Y", KeybindContext.Global)]
    public ICommand RedoCommand { get; }

    [Command("ameko.document.search", "Ctrl+F", KeybindContext.Global)]
    public ICommand ShowSearchDialogCommand { get; }

    [Command("ameko.document.spellcheck", "F7", KeybindContext.Global)]
    public ICommand ShowSpellcheckDialogCommand { get; }

    // Subtitle
    [Command("ameko.stylesManager.show", KeybindContext.Global)]
    public ICommand ShowStylesManagerCommand { get; }

    [Command("ameko.reference.attach", KeybindContext.Global)]
    public ICommand AttachReferenceFileCommand { get; }

    [Command("ameko.reference.detach", KeybindContext.Global)]
    public ICommand DetachReferenceFileCommand { get; }

    [Command("ameko.document.properties", KeybindContext.Global)]
    public ICommand ShowScriptInfoDialogCommand { get; }

    [Command("ameko.document.profile", KeybindContext.Global)]
    public ICommand ShowProfileSubtitlesCommand { get; }

    [Command("ameko.document.sort", KeybindContext.Global)]
    public ICommand ShowSortDialogCommand { get; }

    [Command("ameko.document.select", KeybindContext.Global)]
    public ICommand ShowSelectDialogCommand { get; }

    // Project
    [Command("ameko.project.knp.show", KeybindContext.Global)]
    public ICommand ShowKnpWindowCommand { get; }

    [Command("ameko.project.config.show", KeybindContext.Global)]
    public ICommand ShowProjectConfigDialogCommand { get; }

    // Timing
    [Command("ameko.document.shiftTimes", "Ctrl+I", KeybindContext.Global)]
    public ICommand ShowShiftTimesDialogCommand { get; }

    [Command("ameko.event.snap.startKf", KeybindContext.Global)]
    public ICommand SnapStartToKeyframeCommand { get; }

    [Command("ameko.event.snap.endKf", KeybindContext.Global)]
    public ICommand SnapEndToKeyframeCommand { get; }

    [Command("ameko.event.snap.startFrame", "Ctrl+D3", KeybindContext.Global)]
    public ICommand SnapStartToCurrentFrameCommand { get; }

    [Command("ameko.event.snap.endFrame", "Ctrl+D4", KeybindContext.Global)]
    public ICommand SnapEndToCurrentFrameCommand { get; }

    // Video
    [Command("ameko.video.open", KeybindContext.Global)]
    public ICommand OpenVideoCommand { get; }
    public ICommand OpenVideoNoGuiCommand { get; }

    [Command("ameko.video.close", KeybindContext.Global)]
    public ICommand CloseVideoCommand { get; }

    [Command("ameko.video.openKeyframes", KeybindContext.Global)]
    public ICommand OpenKeyframesCommand { get; }

    [Command("ameko.video.jump", "Ctrl+G", KeybindContext.Global)]
    public ICommand ShowJumpDialogCommand { get; }

    [Command("ameko.audio.open", KeybindContext.Global)]
    public ICommand OpenAudioCommand { get; }

    [Command("ameko.audio.close", KeybindContext.Global)]
    public ICommand CloseAudioCommand { get; }

    [Command("ameko.audio.changeTracks", KeybindContext.Global)]
    public ICommand ChangeTracksCommand { get; }

    // Scripts
    // Command execution doesn't get a keybind. So sad :(
    public ICommand ExecuteScriptCommand { get; }

    [Command("ameko.scripts.reload", KeybindContext.Global)]
    public ICommand ReloadScriptsCommand { get; }

    [Command("ameko.pkgMan.show", KeybindContext.Global)]
    public ICommand ShowPackageManagerCommand { get; }

    [Command("ameko.playground.show", KeybindContext.Global)]
    public ICommand ShowPlaygroundWindowCommand { get; }

    // Layouts
    public ICommand SelectLayoutCommand { get; }
    public ICommand RefreshLayoutsCommand { get; }

    // Help
    [Command("ameko.help.show", "F1", KeybindContext.Global)]
    public ICommand ShowHelpWindowCommand { get; }

    [Command("ameko.logs.show", "Ctrl+L", KeybindContext.Global)]
    public ICommand ShowLogWindowCommand { get; }

    [Command("ameko.about.show", "Shift+F1", KeybindContext.Global)]
    public ICommand ShowAboutWindowCommand { get; }

    [Command("ameko.config.show", "Ctrl+OemComma", KeybindContext.Global)]
    public ICommand ShowConfigDialogCommand { get; }

    [Command("ameko.keybinds.show", KeybindContext.Global)]
    public ICommand ShowKeybindsDialogCommand { get; }

    [Command("ameko.issues.open", KeybindContext.Global)]
    public ICommand OpenIssueTrackerCommand { get; }

    // Other
    [Command("ameko.commandPalette.show", "Ctrl+P", KeybindContext.Global)]
    public ICommand ShowCommandPaletteCommand { get; }

    [Command("ameko.window.fullscreen", "F11", KeybindContext.Global)]
    public ICommand ToggleFullscreenCommand { get; }
    public ICommand RemoveDocumentFromProjectCommand { get; }
    public ICommand RemoveDirectoryFromProjectCommand { get; }
    public ICommand OpenDocumentCommand { get; }
    public ICommand RenameDocumentCommand { get; }
    public ICommand RenameDirectoryCommand { get; }
    public ICommand CreateFolderCommand { get; }
    public ICommand MoveToFolderCommand { get; }
    public ICommand AttachReferenceFileNoGuiCommand { get; }
    public ICommand CheckSpellcheckDictionaryCommand { get; }
    #endregion

    public IProjectProvider ProjectProvider { get; }
    public IIoService IoService { get; }
    public GitToolboxViewModel GitToolboxViewModel { get; }

    public ObservableCollection<TemplatedControl> ScriptMenuItems { get; }
    public ObservableCollection<TemplatedControl> LayoutMenuItems { get; }
    public ObservableCollection<TemplatedControl> RecentDocumentMenuItems { get; }
    public ObservableCollection<TemplatedControl> RecentProjectMenuItems { get; }

    public string CurrentMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = I18N.Resources.Message_Welcome;

    /// <summary>
    /// WindowSection title
    /// </summary>
    public string WindowTitle { get; } =
        !Program.IsInSafeMode
            ? $"Ameko {VersionService.FullLabel}"
            : $"Ameko {VersionService.FullLabel} [Safe Mode]";

    /// <summary>
    /// Set the <see cref="Project.WorkingSpace"/> to the selected workspace, opening it if needed
    /// </summary>
    /// <param name="workspaceId">ID to open</param>
    public async Task TryLoadReferenced(int workspaceId)
    {
        var wsp = ProjectProvider.Current.LoadedWorkspaces.FirstOrDefault(w => w.Id == workspaceId);
        if (wsp is not null)
        {
            ProjectProvider.Current.WorkingSpace = wsp;
            return;
        }

        wsp = ProjectProvider.Current.OpenDocument(workspaceId);
        if (wsp is null)
            return;

        var lastPercent = -1;
        ISourceProvider.ProgressCallback? callback = null;
        if (_tabFactory.TryGetViewModel(wsp, out var tabVm))
        {
            callback = (current, total) =>
            {
                var progress = (double)current / total;
                var percent = (int)(100.0d * progress);

                if (percent == lastPercent)
                    return;

                lastPercent = percent;
                Dispatcher.UIThread.Post(() => tabVm.IndexingProgress = progress);
            };
        }

        try
        {
            Dispatcher.UIThread.Post(() => tabVm?.IsIndexing = true);
            await IoService.ProcessProjectGarbageAsync(wsp, ProjectProvider.Current, callback);
        }
        finally
        {
            Dispatcher.UIThread.Post(() => tabVm?.IsIndexing = false);
        }
    }

    private void GenerateScriptsMenu()
    {
        _logger.LogDebug("Regenerating scripts menu...");
        var menuItems = ScriptMenuService.GenerateMenuItemSource(
            ScriptService.Scripts,
            Configuration.ScriptMenuOverrides,
            ExecuteScriptCommand
        );
        ScriptMenuItems.Clear();
        ScriptMenuItems.AddRange(menuItems);
        if (menuItems.Count > 0)
            ScriptMenuItems.Add(new Separator());
        ScriptMenuItems.Add(
            ScriptMenuService.GeneratePlaygroundMenuItem(ShowPlaygroundWindowCommand)
        );
        ScriptMenuItems.Add(new Separator());
        ScriptMenuItems.Add(ScriptMenuService.GenerateReloadMenuItem(ReloadScriptsCommand));
        ScriptMenuItems.Add(ScriptMenuService.GeneratePkgManMenuItem(ShowPackageManagerCommand));
        _logger.LogDebug("Done!");
    }

    private void GenerateLayoutsMenu()
    {
        _logger.LogDebug("Regenerating layouts menu...");
        var menuItems = LayoutMenuService.GenerateMenuItemSource(
            LayoutProvider.Layouts,
            SelectLayoutCommand
        );
        LayoutMenuItems.Clear();
        LayoutMenuItems.AddRange(menuItems);
        if (menuItems.Count > 0)
            LayoutMenuItems.Add(new Separator());
        LayoutMenuItems.Add(LayoutMenuService.GenerateReloadMenuItem(RefreshLayoutsCommand));
        _logger.LogDebug("Done!");
    }

    private void GenerateRecentsMenus()
    {
        _logger.LogDebug("Regenerating recents menus...");
        var subsMenuItems = RecentsMenuService.GenerateMenuItemSource(
            Persistence.RecentDocuments,
            OpenSubtitleNoGuiCommand
        );
        var prjMenuItems = RecentsMenuService.GenerateMenuItemSource(
            Persistence.RecentProjects,
            OpenProjectNoGuiCommand
        );

        RecentDocumentMenuItems.Clear();
        RecentProjectMenuItems.Clear();
        RecentDocumentMenuItems.AddRange(subsMenuItems);
        RecentProjectMenuItems.AddRange(prjMenuItems);
        if (RecentDocumentMenuItems.Count > 0)
            RecentDocumentMenuItems.Add(new Separator());
        if (RecentProjectMenuItems.Count > 0)
            RecentProjectMenuItems.Add(new Separator());
        RecentDocumentMenuItems.Add(
            RecentsMenuService.GenerateClearMenuItem(ClearRecentSubtitlesCommand)
        );
        RecentProjectMenuItems.Add(
            RecentsMenuService.GenerateClearMenuItem(ClearRecentProjectsCommand)
        );
        _logger.LogDebug("Done!");
    }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IConfiguration configuration,
        IPersistence persistence,
        IIoService ioService,
        ILayoutProvider layoutProvider,
        IProjectProvider projectProvider,
        IScriptService scriptService,
        IKeybindService keybindService,
        IDictionaryService dictionaryService,
        IMessageBoxService messageBoxService,
        IMessageService messageService,
        ISpellcheckService spellCheckService,
        ITabFactory tabFactory,
        IViewModelFactory vmFactory,
        ICommandService commandService
    )
    {
        _logger = logger;

        Configuration = configuration;
        Persistence = persistence;
        IoService = ioService;
        LayoutProvider = layoutProvider;
        ProjectProvider = projectProvider;
        ScriptService = scriptService;
        KeybindService = keybindService;

        _dictionaryService = dictionaryService;
        _messageBoxService = messageBoxService;
        _messageService = messageService;
        _spellcheckService = spellCheckService;
        _tabFactory = tabFactory;
        _vmFactory = vmFactory;

        GitToolboxViewModel = _vmFactory.Create<GitToolboxViewModel>();

        #region Interactions
        // File
        OpenSubtitle = new Interaction<RxVoid, Uri[]?>();
        SaveSubtitleAs = new Interaction<string, Uri?>();
        ExportSubtitle = new Interaction<string, Uri?>();
        OpenProject = new Interaction<RxVoid, Uri?>();
        OpenFolderAsProject = new Interaction<RxVoid, Uri?>();
        SaveProjectAs = new Interaction<string, Uri?>();
        // Edit
        ShowSearchDialog = new Interaction<SearchDialogViewModel, RxVoid>();
        ShowSpellcheckDialog = new Interaction<SpellcheckDialogViewModel, RxVoid>();
        // Subtitle
        ShowStylesManager = new Interaction<StylesManagerWindowViewModel, RxVoid>();
        AttachReferenceFile = new Interaction<RxVoid, Uri?>();
        ShowScriptInfoDialog = new Interaction<ScriptInfoDialogViewModel, RxVoid>();
        ShowProfilerDialog = new Interaction<ProfilerDialogViewModel, RxVoid>();
        ShowSortDialog = new Interaction<SortDialogViewModel, RxVoid>();
        ShowSelectDialog = new Interaction<SelectDialogViewModel, RxVoid>();
        // Project
        ShowKnpWindow = new Interaction<KnpWindowViewModel, RxVoid>();
        ShowProjectConfigDialog = new Interaction<ProjectConfigDialogViewModel, RxVoid>();
        // Video
        OpenVideo = new Interaction<RxVoid, Uri?>();
        OpenKeyframes = new Interaction<RxVoid, Uri?>();
        ShowJumpDialog = new Interaction<JumpDialogViewModel, JumpDialogClosedMessage?>();
        // Audio
        OpenAudio = new Interaction<RxVoid, Uri?>();
        // Timing
        ShowShiftTimesDialog = new Interaction<ShiftTimesDialogViewModel, RxVoid>();
        // Scripts
        ShowPackageManager = new Interaction<PkgManWindowViewModel, RxVoid>();
        ShowPlaygroundWindow = new Interaction<PlaygroundWindowViewModel, RxVoid>();
        // Help
        ShowHelpWindow = new Interaction<HelpWindowViewModel, RxVoid>();
        ShowLogWindow = new Interaction<LogWindowViewModel, RxVoid>();
        ShowAboutWindow = new Interaction<AboutWindowViewModel, RxVoid>();
        ShowConfigDialog = new Interaction<ConfigDialogViewModel, RxVoid>();
        ShowKeybindsDialog = new Interaction<KeybindsDialogViewModel, RxVoid>();
        OpenIssueTracker = new Interaction<RxVoid, RxVoid>();
        // Other
        ShowCommandPaletteDialog = new Interaction<CommandPaletteDialogViewModel, RxVoid>();
        ToggleFullscreen = new Interaction<RxVoid, RxVoid>();
        ShowInstallDictionaryDialog = new Interaction<InstallDictionaryDialogViewModel, RxVoid>();
        ShowSelectFolderDialog =
            new Interaction<SelectFolderDialogViewModel, SelectFolderMessage?>();
        #endregion

        #region Commands
        // File
        NewCommand = CreateNewCommand();
        OpenSubtitleCommand = CreateOpenSubtitleCommand();
        OpenSubtitleNoGuiCommand = CreateOpenSubtitleNoGuiCommand();
        OpenSubtitlesNoGuiCommand = CreateOpenSubtitlesNoGuiCommand();
        SaveSubtitleCommand = CreateSaveSubtitleCommand();
        SaveSubtitleAsCommand = CreateSaveSubtitleAsCommand();
        ExportSubtitleCommand = CreateExportSubtitleCommand();
        ClearRecentSubtitlesCommand = CreateClearRecentSubtitlesCommand();
        OpenProjectCommand = CreateOpenProjectCommand();
        OpenProjectNoGuiCommand = CreateOpenProjectNoGuiCommand();
        OpenFolderAsProjectCommand = CreateOpenFolderAsProjectCommand();
        SaveProjectCommand = CreateSaveProjectCommand();
        CloseTabCommand = CreateCloseTabCommand();
        CloseProjectCommand = CreateCloseProjectCommand();
        ClearRecentProjectsCommand = CreateClearRecentProjectsCommand();
        QuitCommand = CreateQuitCommand();
        // Edit
        UndoCommand = CreateUndoCommand();
        RedoCommand = CreateRedoCommand();
        ShowSearchDialogCommand = CreateShowSearchDialogCommand();
        ShowSpellcheckDialogCommand = CreateShowSpellcheckDialogCommand();
        // Subtitle
        ShowStylesManagerCommand = CreateShowStylesManagerCommand();
        AttachReferenceFileCommand = CreateAttachReferenceFileCommand();
        DetachReferenceFileCommand = CreateDetachReferenceFileCommand();
        ShowScriptInfoDialogCommand = CreateShowScriptInfoDialogCommand();
        ShowProfileSubtitlesCommand = CreateShowProfileSubtitlesDialogCommand();
        ShowSortDialogCommand = CreateShowSortDialogCommand();
        ShowSelectDialogCommand = CreateShowSelectDialogCommand();
        // Project
        ShowKnpWindowCommand = CreateShowKnpWindowCommand();
        ShowProjectConfigDialogCommand = CreateShowProjectConfigDialogCommand();
        // Timing
        ShowShiftTimesDialogCommand = CreateShowShiftTimesDialogCommand();
        SnapStartToKeyframeCommand = CreateSnapStartToKeyframeCommand();
        SnapEndToKeyframeCommand = CreateSnapEndToKeyframeCommand();
        SnapStartToCurrentFrameCommand = CreateSnapStartToCurrentFrameCommand();
        SnapEndToCurrentFrameCommand = CreateSnapEndToCurrentFrameCommand();

        // Video
        OpenVideoCommand = CreateOpenVideoCommand();
        OpenVideoNoGuiCommand = CreateOpenVideoNoGuiCommand();
        CloseVideoCommand = CreateCloseVideoCommand();
        OpenKeyframesCommand = CreateOpenKeyframesCommand();
        ShowJumpDialogCommand = CreateShowJumpDialogCommand();
        // Audio
        OpenAudioCommand = CreateOpenAudioCommand();
        CloseAudioCommand = CreateCloseAudioCommand();
        ChangeTracksCommand = CreateChangeTracksCommand();
        // Scripts
        ExecuteScriptCommand = CreateExecuteScriptCommand();
        ReloadScriptsCommand = CreateReloadScriptsCommand();
        ShowPackageManagerCommand = CreateShowPackageManagerCommand();
        ShowPlaygroundWindowCommand = CreateShowPlaygroundCommand();
        // Layouts
        SelectLayoutCommand = CreateSelectLayoutCommand();
        RefreshLayoutsCommand = CreateRefreshLayoutsCommand();
        // Help
        ShowHelpWindowCommand = CreateShowHelpWindowCommand();
        ShowLogWindowCommand = CreateShowLogWindowCommand();
        ShowAboutWindowCommand = CreateShowAboutWindowCommand();
        ShowConfigDialogCommand = CreateShowConfigDialogCommand();
        ShowKeybindsDialogCommand = CreateShowKeybindsDialogCommand();
        OpenIssueTrackerCommand = CreateOpenIssueTrackerCommand();
        // Other
        ShowCommandPaletteCommand = CreateShowCommandPaletteCommand();
        ToggleFullscreenCommand = CreateToggleFullscreenCommand();
        RemoveDocumentFromProjectCommand = CreateRemoveDocumentFromProjectCommand();
        RemoveDirectoryFromProjectCommand = CreateRemoveFolderFromProjectCommand();
        OpenDocumentCommand = CreateOpenDocumentCommand();
        AttachReferenceFileNoGuiCommand = CreateAttachReferenceFileNoGuiCommand();
        RenameDocumentCommand = CreateRenameDocumentCommand();
        RenameDirectoryCommand = CreateRenameFolderCommand();
        CreateFolderCommand = CreateCreateFolderCommand();
        MoveToFolderCommand = CreateMoveToFolderCommand();
        CheckSpellcheckDictionaryCommand = CreateCheckSpellcheckDictionaryCommand();
        #endregion

        ScriptMenuItems = [];
        ScriptService.Reloaded += (_, _) => GenerateScriptsMenu();

        LayoutMenuItems = [];
        LayoutProvider.LayoutChanged += (_, _) => GenerateLayoutsMenu();
        GenerateLayoutsMenu();

        RecentDocumentMenuItems = [];
        RecentProjectMenuItems = [];
        Persistence.PropertyChanged += (_, args) =>
        {
            var flag =
                args.PropertyName
                is nameof(Persistence.RecentDocuments)
                    or nameof(Persistence.RecentProjects);
            if (flag)
            {
                GenerateRecentsMenus();
            }
        };
        GenerateRecentsMenus();

        // Register commands ASAP so they can be used for keybinds
        commandService.RegisterCommands(-1, this);

        _messageService.MessageReady += (_, msg) => CurrentMessage = msg.Content;
        _messageService.QueueDrained += (_, _) => CurrentMessage = string.Empty;
    }
}
