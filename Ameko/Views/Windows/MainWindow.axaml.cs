// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using Ameko.Messages;
using Ameko.Services;
using Ameko.Utilities;
using Ameko.ViewModels;
using Ameko.ViewModels.Dialogs;
using Ameko.ViewModels.Windows;
using Ameko.Views.Dialogs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using DynamicData;
using Holo.Configuration;
using Holo.Configuration.Keybinds;
using Holo.Media.Providers;
using Holo.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Ameko.Views.Windows;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private static readonly string[] ScriptExtensions = [".ass", ".ssa", ".srt", ".txt"];
    private static readonly string[] VideoExtensions =
    [
        ".mkv",
        ".mp4",
        ".m4v",
        ".mov",
        ".webm",
        ".avi",
        ".wmv",
        "ts",
        "vob",
    ];
    private static readonly string[] AudioExtensions =
    [
        ".mkv",
        ".mp4",
        ".mp3",
        ".flac",
        ".ogg",
        ".opus",
    ];
    private const string ProjectExtension = ".aproj";

    private readonly ILogger _logger;
    private readonly IConfiguration _config;
    private readonly ISourceProvider _sourceProvider;
    private readonly SearchDialog _searchDialog;
    private readonly CommandPaletteDialog _cmdPalette;
    private readonly KnpWindow _knpWindow;
    private bool _isSearching;
    private bool _isCmdPaletteOpen;
    private bool _isKnpWindowOpen;
    private bool _canClose;

    private WindowState? _lastWindowState;

    /// <summary>
    /// Show an async dialog window
    /// </summary>
    /// <param name="interaction">Interaction</param>
    /// <typeparam name="TDialog">Dialog type</typeparam>
    /// <typeparam name="TViewModel">ViewModel type</typeparam>
    private async Task DoShowDialogAsync<TDialog, TViewModel>(
        IInteractionContext<TViewModel, Unit> interaction
    )
        where TDialog : Window, new()
        where TViewModel : ViewModelBase
    {
        var dialog = new TDialog { DataContext = interaction.Input };
        await dialog.ShowDialog(this);
        interaction.SetOutput(Unit.Default);
    }

    /// <summary>
    /// Show a window
    /// </summary>
    /// <param name="interaction">Interaction</param>
    /// <typeparam name="TWindow">Dialog type</typeparam>
    /// <typeparam name="TViewModel">ViewModel type</typeparam>
    private static void DoShowWindow<TWindow, TViewModel>(
        IInteractionContext<TViewModel, Unit> interaction
    )
        where TWindow : Window, new()
        where TViewModel : ViewModelBase
    {
        var window = new TWindow { DataContext = interaction.Input };
        window.Show();
        interaction.SetOutput(Unit.Default);
    }

    private async Task DoShowOpenSubtitleDialogAsync(IInteractionContext<Unit, Uri[]?> interaction)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = I18N.Other.FileDialog_OpenSubtitle_Title,
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Ass)
                    {
                        Patterns = ["*.ass", "*.ssa"],
                    },
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Srt)
                    {
                        Patterns = ["*.srt"],
                    },
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Text)
                    {
                        Patterns = ["*.txt"],
                    },
                ],
            }
        );
        if (files.Count > 0)
        {
            interaction.SetOutput(files.Select(f => f.Path).ToArray());
            return;
        }
        interaction.SetOutput([]);
    }

    private async Task DoShowSaveSubtitleAsDialogAsync(
        IInteractionContext<string, Uri?> interaction
    )
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = I18N.Other.FileDialog_SaveSubtitle_Title,
                FileTypeChoices =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Ass)
                    {
                        Patterns = ["*.ass"],
                    },
                ],
                SuggestedFileName = interaction.Input,
            }
        );

        if (file is not null)
        {
            var path = file.Path;
            if (!Path.HasExtension(path.LocalPath))
                path = new Uri(Path.ChangeExtension(path.LocalPath, ".ass"));

            interaction.SetOutput(path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoShowExportSubtitleDialogAsync(
        IInteractionContext<string, Uri?> interaction
    )
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = I18N.Other.FileDialog_ExportSubtitle_Title,
                FileTypeChoices =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Text)
                    {
                        Patterns = ["*.txt"],
                    },
                ],
                SuggestedFileName = interaction.Input,
            }
        );

        if (file is not null)
        {
            var path = file.Path;
            if (!Path.HasExtension(path.LocalPath))
                path = new Uri(Path.ChangeExtension(path.LocalPath, ".txt"));

            interaction.SetOutput(path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoShowOpenProjectDialogAsync(IInteractionContext<Unit, Uri?> interaction)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = I18N.Other.FileDialog_OpenProject_Title,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Project)
                    {
                        Patterns = ["*.aproj"],
                    },
                ],
            }
        );
        if (files.Count > 0)
        {
            interaction.SetOutput(files[0].Path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoShowOpenFolderAsProjectDialogAsync(
        IInteractionContext<Unit, Uri?> interaction
    )
    {
        var dirs = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = I18N.Other.FileDialog_OpenFolderAsProject_Title,
                AllowMultiple = false,
            }
        );
        if (dirs.Count > 0)
        {
            interaction.SetOutput(dirs[0].Path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoShowSaveProjectAsDialogAsync(IInteractionContext<string, Uri?> interaction)
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = I18N.Other.FileDialog_SaveProject_Title,
                FileTypeChoices =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Project)
                    {
                        Patterns = ["*.aproj"],
                    },
                ],
                SuggestedFileName = interaction.Input,
            }
        );

        if (file is not null)
        {
            var path = file.Path;
            if (!Path.HasExtension(path.LocalPath))
                path = new Uri(Path.ChangeExtension(path.LocalPath, ".aproj"));

            interaction.SetOutput(path);
            return;
        }
        interaction.SetOutput(null);
    }

    private void DoShowSearchDialog(IInteractionContext<SearchDialogViewModel, Unit> interaction)
    {
        _searchDialog.DataContext ??= interaction.Input;

        if (_isSearching)
        {
            _searchDialog.Activate();
        }
        else
        {
            _isSearching = true;
            _searchDialog.Show();
        }
        interaction.SetOutput(Unit.Default);
    }

    private void DoShowCommandPalette(
        IInteractionContext<CommandPaletteDialogViewModel, Unit> interaction
    )
    {
        _cmdPalette.DataContext ??= interaction.Input;

        if (_isCmdPaletteOpen)
        {
            _cmdPalette.Activate();
        }
        else
        {
            _isCmdPaletteOpen = true;
            _cmdPalette.Show();
        }
        interaction.SetOutput(Unit.Default);
    }

    private void DoShowKnpWindow(IInteractionContext<KnpWindowViewModel, Unit> interaction)
    {
        _knpWindow.DataContext ??= interaction.Input;

        if (_isKnpWindowOpen)
        {
            _knpWindow.Activate();
        }
        else
        {
            _isKnpWindowOpen = true;
            _knpWindow.Show();
        }
        interaction.SetOutput(Unit.Default);
    }

    private async Task DoShowAttachReferenceFileDialogAsync(
        IInteractionContext<Unit, Uri?> interaction
    )
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = I18N.Other.FileDialog_AttachReferenceFile_Title,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Ass)
                    {
                        Patterns = ["*.ass", "*.ssa"],
                    },
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Srt)
                    {
                        Patterns = ["*.srt"],
                    },
                ],
            }
        );
        if (files.Count > 0)
        {
            interaction.SetOutput(files[0].Path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoShowOpenVideoDialogAsync(IInteractionContext<Unit, Uri?> interaction)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = I18N.Other.FileDialog_OpenVideo_Title,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Video)
                    {
                        Patterns = VideoExtensions.Select(e => $"*{e}").ToArray(),
                    },
                ],
            }
        );
        if (files.Count > 0)
        {
            interaction.SetOutput(files[0].Path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoShowOpenAudioDialogAsync(IInteractionContext<Unit, Uri?> interaction)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = I18N.Other.FileDialog_OpenAudio_Title,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Audio)
                    {
                        Patterns = AudioExtensions.Select(e => $"*{e}").ToArray(),
                    },
                ],
            }
        );
        if (files.Count > 0)
        {
            interaction.SetOutput(files[0].Path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoShowOpenKeyframesDialogAsync(IInteractionContext<Unit, Uri?> interaction)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = I18N.Other.FileDialog_OpenKeyframes_Title,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Kf)
                    {
                        Patterns = ["*.txt", "*.log"],
                    },
                ],
            }
        );
        if (files.Count > 0)
        {
            interaction.SetOutput(files[0].Path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoShowJumpDialogAsync(
        IInteractionContext<JumpDialogViewModel, JumpDialogClosedMessage?> interaction
    )
    {
        _logger.LogDebug("Displaying Jump dialog");
        var dialog = new JumpDialog { DataContext = interaction.Input };
        var result = await dialog.ShowDialog<JumpDialogClosedMessage?>(this);
        interaction.SetOutput(result);
    }

    private async Task DoShowInstallDictionaryDialogAsync(
        IInteractionContext<InstallDictionaryDialogViewModel, Unit> interaction
    )
    {
        _logger.LogDebug("Displaying install dictionary dialog");
        var dialog = new InstallDictionaryDialog { DataContext = interaction.Input };
        await dialog.ShowDialog<EmptyMessage>(this);
        interaction.SetOutput(Unit.Default);
    }

    private async Task DoShowSelectFolderDialogAsync(
        IInteractionContext<SelectFolderDialogViewModel, SelectFolderMessage?> interaction
    )
    {
        _logger.LogDebug("Displaying Select Folder dialog");
        var dialog = new SelectFolderDialog { DataContext = interaction.Input };
        var result = await dialog.ShowDialog<SelectFolderMessage?>(this);
        interaction.SetOutput(result);
    }

    private async Task DoOpenIssueTrackerAsync(IInteractionContext<Unit, Unit> interaction)
    {
        const string issuesUrl = "https://github.com/9vult/Ameko/issues";
        _logger.LogDebug("Opening the issue tracker in the default browser");
        try
        {
            await Launcher.LaunchUriAsync(new Uri(issuesUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch default browser");
        }
        interaction.SetOutput(Unit.Default);
    }

    private void DoToggleFullscreen(IInteractionContext<Unit, Unit> interaction)
    {
        interaction.SetOutput(Unit.Default);
        if (WindowState is WindowState.FullScreen && _lastWindowState is not null)
        {
            WindowState = _lastWindowState.Value;
        }
        else if (WindowState is not WindowState.FullScreen)
        {
            _lastWindowState = WindowState;
            WindowState = WindowState.FullScreen;
        }
    }

    public MainWindow(
        ILogger<MainWindow> logger,
        IConfiguration config,
        ISourceProvider sourceProvider
    )
    {
        _logger = logger;
        _config = config;
        _sourceProvider = sourceProvider;
        _logger.LogInformation("Initializing Main Window...");
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, DoDragAndDrop);

        // Set up search dialog
        _searchDialog = new SearchDialog();
        _searchDialog.Closing += (sender, args) =>
        {
            if (sender is not SearchDialog searchDialog)
                return;
            args.Cancel = true;
            searchDialog.Hide();
            _isSearching = false;
        };

        // Set up command palette
        _cmdPalette = new CommandPaletteDialog();
        _cmdPalette.Closing += (sender, args) =>
        {
            if (sender is not CommandPaletteDialog cmdP)
                return;
            args.Cancel = true;
            cmdP.Hide();
            _isCmdPaletteOpen = false;
        };

        // Set up KNP window
        _knpWindow = new KnpWindow();
        _knpWindow.Closing += (sender, args) =>
        {
            if (sender is not KnpWindow knpWindow)
                return;
            args.Cancel = true;
            knpWindow.Hide();
            _isKnpWindowOpen = false;
        };

        Closing += async (sender, args) => await OnWindowClosing(sender, args);
        Closed += OnWindowClosed;

        // Hook into macOS activations
        if (Application.Current?.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
        {
            activatableLifetime.Activated += (_, a) =>
            {
                if (a is not FileActivatedEventArgs { Kind: ActivationKind.File } args)
                    return;

                foreach (var file in args.Files)
                {
                    var path = file.Path.LocalPath;
                    switch (Path.GetExtension(path).ToLower())
                    {
                        case ".ass":
                        case ".srt":
                        case ".ssa":
                            ViewModel?.OpenSubtitleNoGuiCommand.Execute(file.Path);
                            break;
                        case ".aproj":
                            ViewModel?.OpenProjectNoGuiCommand.Execute(file.Path);
                            break;
                    }
                }
            };
        }

        this.WhenActivated(disposables =>
        {
            if (ViewModel is not null)
            {
                // csharpier-ignore-start
                // File
                ViewModel.OpenSubtitle.RegisterHandler(DoShowOpenSubtitleDialogAsync);
                ViewModel.SaveSubtitleAs.RegisterHandler(DoShowSaveSubtitleAsDialogAsync);
                ViewModel.ExportSubtitle.RegisterHandler(DoShowExportSubtitleDialogAsync);
                ViewModel.OpenProject.RegisterHandler(DoShowOpenProjectDialogAsync);
                ViewModel.OpenFolderAsProject.RegisterHandler(DoShowOpenFolderAsProjectDialogAsync);
                ViewModel.SaveProjectAs.RegisterHandler(DoShowSaveProjectAsDialogAsync);
                // Edit
                ViewModel.ShowSearchDialog.RegisterHandler(DoShowSearchDialog);
                ViewModel.ShowSpellcheckDialog.RegisterHandler(DoShowDialogAsync<SpellcheckDialog, SpellcheckDialogViewModel>);
                // Subtitle
                ViewModel.ShowStylesManager.RegisterHandler(DoShowWindow<StylesManagerWindow, StylesManagerWindowViewModel>);
                ViewModel.AttachReferenceFile.RegisterHandler(DoShowAttachReferenceFileDialogAsync);
                ViewModel.ShowScriptInfoDialog.RegisterHandler(DoShowDialogAsync<ScriptInfoDialog, ScriptInfoDialogViewModel>);
                ViewModel.ShowProfilerDialog.RegisterHandler(DoShowDialogAsync<ProfilerDialog, ProfilerDialogViewModel>);
                ViewModel.ShowSortDialog.RegisterHandler(DoShowDialogAsync<SortDialog, SortDialogViewModel>);
                ViewModel.ShowSelectDialog.RegisterHandler(DoShowDialogAsync<SelectDialog, SelectDialogViewModel>);
                // Project
                ViewModel.ShowKnpWindow.RegisterHandler(DoShowKnpWindow);
                ViewModel.ShowProjectConfigDialog.RegisterHandler(DoShowDialogAsync<ProjectConfigDialog, ProjectConfigDialogViewModel>);
                // Timing
                ViewModel.ShowShiftTimesDialog.RegisterHandler(DoShowDialogAsync<ShiftTimesDialog, ShiftTimesDialogViewModel>);
                // Video
                ViewModel.OpenVideo.RegisterHandler(DoShowOpenVideoDialogAsync);
                ViewModel.OpenKeyframes.RegisterHandler(DoShowOpenKeyframesDialogAsync);
                ViewModel.ShowJumpDialog.RegisterHandler(DoShowJumpDialogAsync);
                // Audio
                ViewModel.OpenAudio.RegisterHandler(DoShowOpenAudioDialogAsync);
                // Scripts
                ViewModel.ShowPackageManager.RegisterHandler(DoShowWindow<PkgManWindow, PkgManWindowViewModel>);
                ViewModel.ShowPlaygroundWindow.RegisterHandler(DoShowWindow<PlaygroundWindow, PlaygroundWindowViewModel>);
                // Help
                ViewModel.ShowHelpWindow.RegisterHandler(DoShowWindow<HelpWindow, HelpWindowViewModel>);
                ViewModel.ShowLogWindow.RegisterHandler(DoShowWindow<LogWindow, LogWindowViewModel>);
                ViewModel.ShowAboutWindow.RegisterHandler(DoShowDialogAsync<AboutWindow, AboutWindowViewModel>);
                ViewModel.ShowConfigDialog.RegisterHandler(DoShowDialogAsync<ConfigDialog, ConfigDialogViewModel>);
                ViewModel.ShowKeybindsDialog.RegisterHandler(DoShowDialogAsync<KeybindsDialog, KeybindsDialogViewModel>);
                ViewModel.OpenIssueTracker.RegisterHandler(DoOpenIssueTrackerAsync);
                // Other
                ViewModel.ShowCommandPaletteDialog.RegisterHandler(DoShowCommandPalette);
                ViewModel.ToggleFullscreen.RegisterHandler(DoToggleFullscreen);
                ViewModel.ShowInstallDictionaryDialog.RegisterHandler(DoShowInstallDictionaryDialogAsync);
                ViewModel.ShowSelectFolderDialog.RegisterHandler(DoShowSelectFolderDialogAsync);
                // csharpier-ignore-end

                // Generate layouts menu and apply current layout
                ApplyLayout(ViewModel, ViewModel.LayoutProvider.Current);
                GenerateLayoutsMenu();
                ViewModel.LayoutProvider.LayoutChanged += (_, args) =>
                {
                    ApplyLayout(ViewModel, args.Layout);
                    GenerateLayoutsMenu();
                };

                // Generate scripts menu
                GenerateScriptsMenu();
                ViewModel.ScriptService.Reloaded += (_, _) =>
                {
                    GenerateScriptsMenu();
                };

                // Generate recents menus
                GenerateRecentsMenus();
                ViewModel.Persistence.PropertyChanged += (_, args) =>
                {
                    var flag =
                        args.PropertyName
                        is nameof(ViewModel.Persistence.RecentDocuments)
                            or nameof(ViewModel.Persistence.RecentProjects);
                    if (flag)
                    {
                        GenerateRecentsMenus();
                    }
                };

                AttachKeybinds(ViewModel);
                // Update keybinds when WorkingSpace changes
                MessageBus
                    .Current.Listen<WorkingSpaceChangedMessage>()
                    .Subscribe(_ => AttachKeybinds(ViewModel));
                // Update keybinds when keybinds change
                ViewModel.KeybindService.KeybindRegistrar.KeybindsChanged += (_, _) =>
                    AttachKeybinds(ViewModel);

                // Subscript to project changes and reset persistent windows
                ViewModel?.ProjectProvider.PropertyChanged += (_, _) =>
                {
                    if (_isSearching)
                    {
                        _isSearching = false;
                        _searchDialog.Hide();
                    }
                    _searchDialog.DataContext = null;

                    if (_isCmdPaletteOpen)
                    {
                        _isCmdPaletteOpen = false;
                        _cmdPalette.Hide();
                    }
                    _cmdPalette.DataContext = null;

                    if (_isKnpWindowOpen)
                    {
                        _isKnpWindowOpen = false;
                        _knpWindow.Hide();
                    }
                    _knpWindow.DataContext = null;
                };
            }

            ViewModel?.CheckSpellcheckDictionaryCommand.Execute(null);
            ViewModel?.Configuration.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IConfiguration.SpellcheckCulture))
                    ViewModel?.CheckSpellcheckDictionaryCommand.Execute(null);
            };

            Disposable.Create(() => { }).DisposeWith(disposables);
        });

        _logger.LogInformation("Done!");
    }

    private void AttachKeybinds(MainWindowViewModel? vm)
    {
        if (vm is null)
            return;

        var contextId = vm.ProjectProvider.Current.WorkingSpace?.Id ?? -1;
        vm.KeybindService.AttachKeybinds(KeybindContext.Global, this, contextId);
        vm.KeybindService.AttachScriptKeybinds(
            vm.ExecuteScriptCommand,
            KeybindContext.Global,
            this
        );

        _logger.LogInformation("Attached global keybinds for context {Context}", contextId);
    }

    private void ApplyLayout(MainWindowViewModel? vm, Layout? layout)
    {
        if (vm is null || layout is null)
            return;

        if (layout.Window.IsProjectExplorerOnLeft)
        {
            var columnDefinitions = new ColumnDefinitions("115, 2, *");
            columnDefinitions[0].MinWidth = 100;
            columnDefinitions[2].MinWidth = 500;
            MainWindowGrid.ColumnDefinitions = columnDefinitions;

            ProjectExplorer.SetValue(Grid.ColumnProperty, 0);
            TabControlHost.SetValue(Grid.ColumnProperty, 2);
        }
        else
        {
            var columnDefinitions = new ColumnDefinitions("*, 2, 115");
            columnDefinitions[0].MinWidth = 500;
            columnDefinitions[2].MinWidth = 100;
            MainWindowGrid.ColumnDefinitions = columnDefinitions;

            ProjectExplorer.SetValue(Grid.ColumnProperty, 2);
            TabControlHost.SetValue(Grid.ColumnProperty, 0);
        }
    }

    private void GenerateRecentsMenus()
    {
        if (ViewModel is null || ViewModel.DisplayInWindowMenu)
            return;

        _logger.LogDebug("Regenerating recents native menus...");
        var subsMenuItems = RecentsMenuService.GenerateNativeMenuItemSource(
            ViewModel.Persistence.RecentDocuments,
            ViewModel.OpenSubtitleNoGuiCommand
        );
        var prjMenuItems = RecentsMenuService.GenerateNativeMenuItemSource(
            ViewModel.Persistence.RecentProjects,
            ViewModel.OpenProjectNoGuiCommand
        );
        var subsClearItem = RecentsMenuService.GenerateClearNativeMenuItem(
            ViewModel.ClearRecentSubtitlesCommand
        );
        var prjClearItem = RecentsMenuService.GenerateClearNativeMenuItem(
            ViewModel.ClearRecentProjectsCommand
        );

        var subsMenu = NativeMenu
            .GetMenu(this)
            ?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(m => m.Header == I18N.Resources.Menu_File)
            ?.Menu?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(m => m.Header == I18N.Resources.Menu_RecentSubtitles)
            ?.Menu;
        var prjMenu = NativeMenu
            .GetMenu(this)
            ?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(m => m.Header == I18N.Resources.Menu_File)
            ?.Menu?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(m => m.Header == I18N.Resources.Menu_RecentProjects)
            ?.Menu;

        if (subsMenu is null || prjMenu is null)
            return;

        subsMenu.Items.Clear();
        subsMenu.Items.AddRange(subsMenuItems);
        if (subsMenuItems.Count > 0)
            subsMenu.Items.Add(new NativeMenuItemSeparator());
        subsMenu.Items.Add(subsClearItem);

        prjMenu.Items.Clear();
        prjMenu.Items.AddRange(prjMenuItems);
        if (prjMenuItems.Count > 0)
            prjMenu.Items.Add(new NativeMenuItemSeparator());
        prjMenu.Items.Add(prjClearItem);
        _logger.LogDebug("Done!");
    }

    private void GenerateLayoutsMenu()
    {
        if (ViewModel is null || ViewModel.DisplayInWindowMenu)
            return;

        _logger.LogDebug("Regenerating layouts native menu...");
        var menuItems = LayoutMenuService.GenerateNativeMenuItemSource(
            ViewModel.LayoutProvider.Layouts,
            ViewModel.SelectLayoutCommand
        );
        var reloadItem = LayoutMenuService.GenerateReloadNativeMenuItem(
            ViewModel.RefreshLayoutsCommand
        );

        var menu = NativeMenu
            .GetMenu(this)
            ?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(m => m.Header == I18N.Resources.Menu_Layouts)
            ?.Menu;

        if (menu is null)
            return;

        menu.Items.Clear();
        menu.Items.AddRange(menuItems);
        if (menuItems.Count > 0)
            menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(reloadItem);
        _logger.LogDebug("Done!");
    }

    private void GenerateScriptsMenu()
    {
        if (ViewModel is null || ViewModel.DisplayInWindowMenu)
            return;

        _logger.LogDebug("Regenerating scripts native menu...");
        var menuItems = ScriptMenuService.GenerateNativeMenuItemSource(
            ViewModel.ScriptService.Scripts,
            ViewModel.Configuration.ScriptMenuOverrides,
            ViewModel.ExecuteScriptCommand
        );
        var reloadItem = ScriptMenuService.GenerateReloadNativeMenuItem(
            ViewModel.ReloadScriptsCommand
        );
        var pkgManItem = ScriptMenuService.GeneratePkgManNativeMenuItem(
            ViewModel.ShowPackageManagerCommand
        );
        var playgroundItem = ScriptMenuService.GeneratePlaygroundNativeMenuItem(
            ViewModel.ShowPlaygroundWindowCommand
        );

        var menu = NativeMenu
            .GetMenu(this)
            ?.Items.OfType<NativeMenuItem>()
            .FirstOrDefault(m => m.Header == I18N.Resources.Menu_Scripts)
            ?.Menu;

        if (menu is null)
            return;

        menu.Items.Clear();
        menu.Items.AddRange(menuItems);
        if (menuItems.Count > 0)
            menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(playgroundItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(reloadItem);
        menu.Items.Add(pkgManItem);
        _logger.LogDebug("Done...");
    }

    private void DoDragAndDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer is not { } transfer || ViewModel is null)
            return;

        var files = transfer.TryGetFiles() ?? [];

        // Order the incoming files in project → script → media order.
        // This should ensure that dropping in a script and video at the same time loads in the correct order
        var prioritizedFiles = files.OrderBy(file =>
        {
            var ext = Path.GetExtension(file.Path.LocalPath);
            if (ext == ProjectExtension)
                return 0;
            if (ScriptExtensions.Contains(ext))
                return 1;
            if (VideoExtensions.Contains(ext))
                return 2;
            return 3;
        });

        foreach (var file in prioritizedFiles)
        {
            var ext = Path.GetExtension(file.Path.LocalPath);

            if (ScriptExtensions.Contains(ext))
            {
                ViewModel.OpenSubtitleNoGuiCommand.Execute(file.Path);
                continue;
            }

            if (VideoExtensions.Contains(ext))
            {
                ViewModel.OpenVideoNoGuiCommand.Execute(file.Path);
                continue;
            }

            if (ext == ProjectExtension)
            {
                ViewModel.OpenProjectNoGuiCommand.Execute(file.Path);
            }
        }
    }

    private async Task OnWindowClosing(object? _, WindowClosingEventArgs e)
    {
        if (ViewModel is null || _canClose)
        {
            // Good to close
            return;
        }

        e.Cancel = true;

        foreach (var wsp in ViewModel.ProjectProvider.Current.LoadedWorkspaces.ToArray())
        {
            await ViewModel.IoService.SafeCloseWorkspace(wsp, ViewModel.SaveSubtitleAs, false);
        }

        if (ViewModel.ProjectProvider.Current.LoadedWorkspaces.Count != 0)
        {
            _logger.LogInformation(
                "Quit aborted - {LoadedWorkspacesCount} workspaces remain open",
                ViewModel.ProjectProvider.Current.LoadedWorkspaces.Count
            );
            return;
        }

        var projectClosed = await ViewModel.IoService.SafeCloseProject(
            ViewModel.ProjectProvider.Current,
            ViewModel.SaveProjectAs
        );
        if (!projectClosed)
        {
            _logger.LogInformation("Quit aborted - User canceled project closure");
            return;
        }

        _canClose = true;
        Close();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        // Save window state
        ViewModel?.Persistence.WindowState = WindowState.ToPersistentWindowState();
        if (WindowState is WindowState.Normal)
        {
            ViewModel?.Persistence.WindowWidth = Width;
            ViewModel?.Persistence.WindowHeight = Height;
        }

        // Save configuration, etc.
        ViewModel?.Configuration.Save();
        ViewModel?.Persistence.Save();

        // Clean provider cache
        _sourceProvider.CleanCache(_config.IndexCacheExpiration);

        // Goodbye!
        _logger.LogInformation("See you next time...");

        if (
            Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop
        )
            desktop.Shutdown();
        else
            Environment.Exit(0);
    }
}
