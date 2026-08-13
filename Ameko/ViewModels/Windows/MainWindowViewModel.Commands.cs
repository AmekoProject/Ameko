// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using Ameko.DataModels;
using Ameko.Messages;
using Ameko.ViewModels.Dialogs;
using Ameko.Views.Dialogs;
using Ameko.Views.Windows;
using AssCS;
using AssCS.History;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Holo.Media;
using Holo.Media.Providers;
using Holo.Models;
using Material.Icons;
using Microsoft.Extensions.Logging;
using NaturalSort.Extension;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace Ameko.ViewModels.Windows;

public partial class MainWindowViewModel
{
    /// <summary>
    /// Create a new file
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateNewCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var wsp = ProjectProvider.Current.AddWorkspace();
            ProjectProvider.Current.WorkingSpace = wsp;
        });
    }

    /// <summary>
    /// Display the Open Subtitle dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateOpenSubtitleCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var workspaces = await IoService.OpenSubtitleFilesAsync(
                OpenSubtitle,
                ProjectProvider.Current
            );

            foreach (var wsp in workspaces)
            {
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
                    await IoService.ProcessProjectGarbageAsync(
                        wsp,
                        ProjectProvider.Current,
                        callback
                    );
                }
                finally
                {
                    Dispatcher.UIThread.Post(() => tabVm?.IsIndexing = false);
                }
            }
        });
    }

    /// <summary>
    /// Open a subtitle without using a dialog
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<Uri, RxVoid> CreateOpenSubtitleNoGuiCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (Uri uri) =>
            {
                if (await IoService.OpenSubtitleFileAsync(uri, ProjectProvider.Current) is { } wsp)
                {
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
                        await IoService.ProcessProjectGarbageAsync(
                            wsp,
                            ProjectProvider.Current,
                            callback
                        );
                    }
                    finally
                    {
                        Dispatcher.UIThread.Post(() => tabVm?.IsIndexing = false);
                    }
                }
            }
        );
    }

    /// <summary>
    /// Open subtitles without using a dialog
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<Uri[], RxVoid> CreateOpenSubtitlesNoGuiCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (Uri[] uris) =>
            {
                var workspaces = await IoService.OpenSubtitleFilesAsync(
                    uris,
                    ProjectProvider.Current
                );
                foreach (var wsp in workspaces)
                {
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
                        await IoService.ProcessProjectGarbageAsync(
                            wsp,
                            ProjectProvider.Current,
                            callback
                        );
                    }
                    finally
                    {
                        Dispatcher.UIThread.Post(() => tabVm?.IsIndexing = false);
                    }
                }
            }
        );
    }

    /// <summary>
    /// Display either the Save Subtitle or Save Subtitle As dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateSaveSubtitleCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (ProjectProvider.Current.IsWorkspaceLoaded)
            {
                _ = await IoService.SaveSubtitle(
                    SaveSubtitleAs,
                    ProjectProvider.Current.WorkingSpace
                );
            }
        });
    }

    /// <summary>
    /// Display the Save Subtitle As dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateSaveSubtitleAsCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (ProjectProvider.Current.IsWorkspaceLoaded)
            {
                _ = await IoService.SaveSubtitleAs(
                    SaveSubtitleAs,
                    ProjectProvider.Current.WorkingSpace
                );
            }
        });
    }

    /// <summary>
    /// Display the Save As dialog for exporting
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateExportSubtitleCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (ProjectProvider.Current.IsWorkspaceLoaded)
            {
                _ = await IoService.ExportSubtitle(
                    ExportSubtitle,
                    ProjectProvider.Current.WorkingSpace
                );
            }
        });
    }

    /// <summary>
    /// Clear the recent subtitles list
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<RxVoid, RxVoid> CreateClearRecentSubtitlesCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            Persistence.ClearRecentDocuments();
        });
    }

    /// <summary>
    /// Display the Open Project File dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateOpenProjectCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            _logger.LogDebug("Preparing to open project file");
            var uri = await OpenProject.Handle(RxVoid.Default);

            if (uri is null)
            {
                _logger.LogInformation("Opening project file aborted");
                return;
            }

            if (!await IoService.OpenProjectFile(uri, SaveSubtitleAs))
                return;

            MessageBus.Current.SendMessage(new WorkingSpaceChangedMessage());
            var culture = ProjectProvider.Current.SpellcheckCulture;
            if (culture is not null && !_spellcheckService.IsDictionaryInstalled(culture))
            {
                _logger.LogInformation(
                    "Prompting user to download dictionary for {Culture}",
                    culture
                );
                var lang = SpellcheckLanguage.AvailableLanguages.First(l => l.Locale == culture);
                var vm = _vmFactory.Create<InstallDictionaryDialogViewModel>(
                    _dictionaryService,
                    lang,
                    true
                );
                await ShowInstallDictionaryDialog.Handle(vm);
                _spellcheckService.RebuildDictionary();
            }
            else
            {
                _spellcheckService.RebuildDictionary();
            }
        });
    }

    /// <summary>
    /// Open a project without using a dialog
    /// </summary>
    private ReactiveCommand<Uri, RxVoid> CreateOpenProjectNoGuiCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (Uri uri) =>
            {
                _logger.LogDebug("Preparing to open project file (no-gui)");

                if (!await IoService.OpenProjectFile(uri, SaveSubtitleAs))
                    return;

                MessageBus.Current.SendMessage(new WorkingSpaceChangedMessage());
                var culture = ProjectProvider.Current.SpellcheckCulture;
                if (culture is not null && !_spellcheckService.IsDictionaryInstalled(culture))
                {
                    _logger.LogInformation(
                        "Prompting user to download dictionary for {Culture}",
                        culture
                    );
                    var lang = SpellcheckLanguage.AvailableLanguages.First(l =>
                        l.Locale == culture
                    );
                    var vm = _vmFactory.Create<InstallDictionaryDialogViewModel>(
                        _dictionaryService,
                        lang,
                        true
                    );
                    await ShowInstallDictionaryDialog.Handle(vm);
                    _spellcheckService.RebuildDictionary();
                }
                else
                {
                    _spellcheckService.RebuildDictionary();
                }
            }
        );
    }

    /// <summary>
    /// Display the Open Folder as Project File dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateOpenFolderAsProjectCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            _logger.LogDebug("Preparing to open a directory as a project");
            var uri = await OpenFolderAsProject.Handle(RxVoid.Default);

            if (uri is null)
            {
                _logger.LogInformation("Opening project directory aborted");
                return;
            }

            _ = await IoService.OpenProjectDirectory(uri, SaveSubtitleAs);
            MessageBus.Current.SendMessage(new WorkingSpaceChangedMessage());
        });
    }

    /// <summary>
    /// Display the Save Project File dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateSaveProjectCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            _ = await IoService.SaveProject(SaveProjectAs, ProjectProvider.Current);
        });
    }

    /// <summary>
    /// Close the currently active tab
    /// </summary>
    private ReactiveCommand<int?, RxVoid> CreateCloseTabCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (int? id) =>
            {
                if (ProjectProvider.Current.IsWorkspaceLoaded)
                {
                    var wsp = id.HasValue
                        ? ProjectProvider.Current.GetWorkspace(id.Value)
                        : ProjectProvider.Current.WorkingSpace;
                    if (wsp is null)
                        return;

                    // If the user opened a project file, don't open a new workspace
                    var isLoadedProject = ProjectProvider.Current.SavePath is not null;

                    _logger.LogDebug("Closing tab {WspTitle}", wsp.Title);
                    await IoService.SafeCloseWorkspace(wsp, SaveSubtitleAs, !isLoadedProject);

                    // If a project is open, send message in case working wsp is now null
                    if (isLoadedProject)
                        MessageBus.Current.SendMessage(new WorkingSpaceChangedMessage());
                }
            }
        );
    }

    /// <summary>
    /// Close the currently active tab
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateCloseProjectCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            _logger.LogDebug("Preparing to close project");
            var prj = ProjectProvider.Current;

            foreach (var wsp in prj.LoadedWorkspaces.ToArray())
            {
                await IoService.SafeCloseWorkspace(wsp, SaveSubtitleAs, false);
            }

            if (prj.LoadedWorkspaces.Count > 0)
            {
                _logger.LogInformation(
                    "Closing project aborted - {LoadedWorkspacesCount} workspaces remain open",
                    prj.LoadedWorkspaces.Count
                );
                return;
            }

            var canClose = await IoService.SafeCloseProject(prj, SaveProjectAs);
            if (!canClose)
            {
                _logger.LogInformation("Closing project aborted - User canceled operation");
                return;
            }

            ProjectProvider.Current = ProjectProvider.Create();
            _logger.LogDebug("Successfully closed project and opened a new one");
            _spellcheckService.RebuildDictionary();
        });
    }

    /// <summary>
    /// Clear the recent projects list
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<RxVoid, RxVoid> CreateClearRecentProjectsCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            Persistence.ClearRecentProjects();
        });
    }

    /// <summary>
    /// Quit the application
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateQuitCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            _logger.LogDebug("Preparing to quit");

            foreach (var wsp in ProjectProvider.Current.LoadedWorkspaces.ToArray())
            {
                await IoService.SafeCloseWorkspace(wsp, SaveSubtitleAs, false);
            }

            if (ProjectProvider.Current.LoadedWorkspaces.Count != 0)
            {
                _logger.LogInformation(
                    "Quit aborted - {LoadedWorkspacesCount} workspaces remain open",
                    ProjectProvider.Current.LoadedWorkspaces.Count
                );
                return;
            }

            var projectClosed = await IoService.SafeCloseProject(
                ProjectProvider.Current,
                SaveProjectAs
            );
            if (!projectClosed)
            {
                _logger.LogInformation("Quit aborted - User canceled project closure");
                return;
            }

            if (
                Application.Current?.ApplicationLifetime
                is not IClassicDesktopStyleApplicationLifetime desktop
            )
                return;

            desktop.Shutdown();
        });
    }

    /// <summary>
    /// Undo
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateUndoCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            ProjectProvider.Current.WorkingSpace?.Undo();
            Dispatcher.UIThread.Post(
                () =>
                {
                    ProjectProvider.Current.WorkingSpace?.SelectionManager.EndSelectionChange();
                    ProjectProvider.Current.EndSelectionChange();
                },
                DispatcherPriority.Background
            );
        });
    }

    /// <summary>
    /// Redo
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateRedoCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            ProjectProvider.Current.WorkingSpace?.Redo();
            Dispatcher.UIThread.Post(
                () =>
                {
                    ProjectProvider.Current.WorkingSpace?.SelectionManager.EndSelectionChange();
                    ProjectProvider.Current.EndSelectionChange();
                },
                DispatcherPriority.Background
            );
        });
    }

    /// <summary>
    /// Show search dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowSearchDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<SearchDialogViewModel>();
            await ShowSearchDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Show spellcheck dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowSpellcheckDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<SpellcheckDialogViewModel>();
            await ShowSpellcheckDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Display the <see cref="StylesManagerWindow"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowStylesManagerCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (ProjectProvider.Current.IsWorkspaceLoaded)
            {
                var vm = _vmFactory.Create<StylesManagerWindowViewModel>(
                    ProjectProvider.Current,
                    ProjectProvider.Current.WorkingSpace.Document
                );
                await ShowStylesManager.Handle(vm);
            }
        });
    }

    /// <summary>
    /// Display the "attach reference file" dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateAttachReferenceFileCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null)
                return;

            await IoService.AttachReferenceFile(AttachReferenceFile, wsp);
        });
    }

    /// <summary>
    /// Detach the reference file
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateDetachReferenceFileCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            _logger.LogDebug("Detaching reference file");
            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null)
                return;
            wsp.ReferenceFileManager.Reference = null;
        });
    }

    /// <summary>
    /// Show Script Info dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowScriptInfoDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null)
                return;
            var vm = _vmFactory.Create<ScriptInfoDialogViewModel>(wsp.Document);
            await ShowScriptInfoDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Show Profile Subtitles dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowProfileSubtitlesDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null || !wsp.MediaController.IsVideoLoaded)
                return;
            var vm = _vmFactory.Create<ProfilerDialogViewModel>(wsp);
            await ShowProfilerDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Show sort lines dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowSortDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<SortDialogViewModel>();
            await ShowSortDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Show select lines dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowSelectDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<SelectDialogViewModel>();
            await ShowSelectDialog.Handle(vm);
        });
    }

    /// <summary>
    ///Attach a reference file without an open file dialog
    /// </summary>
    private ReactiveCommand<Uri, RxVoid> CreateAttachReferenceFileNoGuiCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (Uri uri) =>
            {
                var wsp = ProjectProvider.Current.WorkingSpace;
                if (wsp is null)
                    return;
                await IoService.AttachReferenceFile(uri, wsp);
            }
        );
    }

    /// <summary>
    /// Display the <see cref="KnpWindow"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowKnpWindowCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<KnpWindowViewModel>();
            await ShowKnpWindow.Handle(vm);
            _spellcheckService.RebuildDictionary(); // Just in case :)
        });
    }

    /// <summary>
    /// Display the <see cref="ProjectConfigDialog"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowProjectConfigDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<ProjectConfigDialogViewModel>();
            await ShowProjectConfigDialog.Handle(vm);
            _spellcheckService.RebuildDictionary(); // Just in case :)
        });
    }

    /// <summary>
    /// Display the <see cref="ShiftTimesDialog"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowShiftTimesDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (ProjectProvider.Current.IsWorkspaceLoaded)
            {
                var vm = _vmFactory.Create<ShiftTimesDialogViewModel>(
                    ProjectProvider.Current.WorkingSpace
                );
                await ShowShiftTimesDialog.Handle(vm);
            }
        });
    }

    /// <summary>
    /// Snap start of active event to nearest keyframe
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<RxVoid, RxVoid> CreateSnapStartToKeyframeCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var prj = ProjectProvider.Current;
            var wsp = prj.WorkingSpace;
            if (wsp is null)
                return;

            var mc = wsp.MediaController;
            if (!mc.IsVideoLoaded)
                return;
            var @event = wsp.SelectionManager.ActiveEvent;
            var startFrame = mc.VideoInfo.FrameFromTime(@event.Start);
            var nearestKf = FindNearestKeyframeTo(startFrame, mc.VideoInfo);
            var kfTime = mc.VideoInfo.TimeFromFrame(nearestKf);

            var delta = @event.Start.TotalMilliseconds - kfTime.TotalMilliseconds;
            if (delta > 0) // Earlier
            {
                var threshold = prj.Timing.SnapStartEarlierThreshold;
                if (threshold == 0)
                    threshold = Configuration.Timing.SnapStartEarlierThreshold;
                if (delta > threshold)
                    return;
            }
            else // Later
            {
                var threshold = prj.Timing.SnapStartLaterThreshold;
                if (threshold == 0)
                    threshold = Configuration.Timing.SnapStartLaterThreshold;
                if (Math.Abs(delta) > threshold)
                    return;
            }

            @event.Start = kfTime;
            wsp.Commit(@event, ChangeType.ModifyEventMeta);
        });
    }

    /// <summary>
    /// Snap end of active event to nearest keyframe
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<RxVoid, RxVoid> CreateSnapEndToKeyframeCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var prj = ProjectProvider.Current;
            var wsp = prj.WorkingSpace;
            if (wsp is null)
                return;

            var mc = wsp.MediaController;
            if (!mc.IsVideoLoaded)
                return;
            var @event = wsp.SelectionManager.ActiveEvent;
            var endFrame = mc.VideoInfo.FrameFromTime(@event.End);
            var nearestKf = FindNearestKeyframeTo(endFrame, mc.VideoInfo);
            var kfTime = mc.VideoInfo.TimeFromFrame(nearestKf);

            var delta = @event.End.TotalMilliseconds - kfTime.TotalMilliseconds;
            if (delta > 0) // Earlier
            {
                var threshold = prj.Timing.SnapEndEarlierThreshold;
                if (threshold == 0)
                    threshold = Configuration.Timing.SnapEndEarlierThreshold;
                if (delta > threshold)
                    return;
            }
            else // Later
            {
                var threshold = prj.Timing.SnapEndLaterThreshold;
                if (threshold == 0)
                    threshold = Configuration.Timing.SnapEndLaterThreshold;
                if (Math.Abs(delta) > threshold)
                    return;
            }

            @event.End = kfTime;
            wsp.Commit(@event, ChangeType.ModifyEventMeta);
        });
    }

    /// <summary>
    /// Snap start of active event to current frame
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<RxVoid, RxVoid> CreateSnapStartToCurrentFrameCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var prj = ProjectProvider.Current;
            var wsp = prj.WorkingSpace;
            if (wsp is null)
                return;

            var mc = wsp.MediaController;
            if (!mc.IsVideoLoaded)
                return;
            var @event = wsp.SelectionManager.ActiveEvent;

            @event.Start = mc.VideoInfo.TimeFromMidpoint(mc.CurrentFrame - 1, mc.CurrentFrame);
            wsp.Commit(@event, ChangeType.ModifyEventMeta);
        });
    }

    /// <summary>
    /// Snap end of active event to current frame
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<RxVoid, RxVoid> CreateSnapEndToCurrentFrameCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var prj = ProjectProvider.Current;
            var wsp = prj.WorkingSpace;
            if (wsp is null)
                return;

            var mc = wsp.MediaController;
            if (!mc.IsVideoLoaded)
                return;
            var @event = wsp.SelectionManager.ActiveEvent;

            @event.End = mc.VideoInfo.TimeFromMidpoint(mc.CurrentFrame, mc.CurrentFrame + 1);
            wsp.Commit(@event, ChangeType.ModifyEventMeta);
        });
    }

    /// <summary>
    /// Display the Open Video dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateOpenVideoCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null)
            {
                ProjectProvider.Current.WorkingSpace = wsp = ProjectProvider.Current.AddWorkspace();
            }

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
                await IoService.OpenVideoFileAsync(OpenVideo, wsp, callback);
            }
            finally
            {
                Dispatcher.UIThread.Post(() => tabVm?.IsIndexing = false);
            }
        });
    }

    /// <summary>
    /// Open a video without using a dialog
    /// </summary>
    private ReactiveCommand<Uri, RxVoid> CreateOpenVideoNoGuiCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (Uri uri) =>
            {
                var wsp = ProjectProvider.Current.WorkingSpace;
                if (wsp is null)
                {
                    ProjectProvider.Current.WorkingSpace = wsp =
                        ProjectProvider.Current.AddWorkspace();
                }

                await IoService.OpenVideoFileAsync(uri, wsp);
            }
        );
    }

    /// <summary>
    /// Close the open video
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateCloseVideoCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            wsp?.MediaController.CloseVideo();
        });
    }

    /// <summary>
    /// Display the Open Keyframes dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateOpenKeyframesCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null)
                return;

            await IoService.OpenKeyframesAsync(OpenKeyframes, wsp);
        });
    }

    /// <summary>
    /// Display the Jump dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowJumpDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            _logger.LogDebug("Opening Jump dialog");

            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null)
                return;

            var videoLoaded = wsp.MediaController.IsVideoLoaded;

            var vm = _vmFactory.Create<JumpDialogViewModel>(videoLoaded);
            var result = await ShowJumpDialog.Handle(vm);

            if (result is null)
                return;

            if (result.Frame != 0)
            {
                wsp.MediaController.SeekTo(result.Frame);
                return;
            }

            if (result.Time != Time.FromSeconds(0))
            {
                wsp.MediaController.SeekTo(result.Time);
                return;
            }

            var @event = wsp.Document.EventManager.Events.ElementAtOrDefault(result.Line - 1);
            if (@event is null)
                return;

            wsp.MediaController.SeekTo(@event.Start);
            // Publish a scroll message (?)
        });
    }

    /// <summary>
    /// Display the Open Audio dialog
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateOpenAudioCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null)
            {
                ProjectProvider.Current.WorkingSpace = wsp = ProjectProvider.Current.AddWorkspace();
            }

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
                await IoService.OpenAudioFileAsync(OpenAudio, wsp, callback);
            }
            finally
            {
                Dispatcher.UIThread.Post(() => tabVm?.IsIndexing = false);
            }
        });
    }

    /// <summary>
    /// Close the open audio
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateCloseAudioCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            wsp?.MediaController.CloseAudio();
        });
    }

    /// <summary>
    /// Change audio tracks
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateChangeTracksCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var wsp = ProjectProvider.Current.WorkingSpace;
            if (wsp is null)
            {
                ProjectProvider.Current.WorkingSpace = wsp = ProjectProvider.Current.AddWorkspace();
            }

            var path = wsp.MediaController.AudioInfo?.Path;
            if (path is null)
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
                await IoService.OpenAudioFileAsync(
                    new Uri(path, UriKind.Absolute),
                    wsp,
                    callback,
                    allowAutoload: false
                );
            }
            finally
            {
                Dispatcher.UIThread.Post(() => tabVm?.IsIndexing = false);
            }
        });
    }

    /// <summary>
    /// Execute a Script
    /// </summary>
    private ReactiveCommand<string, RxVoid> CreateExecuteScriptCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (string qualifiedName) =>
            {
                await ScriptService.ExecuteScriptAsync(qualifiedName);
            }
        );
    }

    /// <summary>
    /// Reload scripts
    /// </summary>
    private ReactiveCommand<bool, RxVoid> CreateReloadScriptsCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (bool isManual) =>
            {
                await ScriptService.Reload(isManual);
            }
        );
    }

    /// <summary>
    /// Display the <see cref="PkgManWindow"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowPackageManagerCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<PkgManWindowViewModel>();
            await ShowPackageManager.Handle(vm);
        });
    }

    /// <summary>
    /// Display the <see cref="PlaygroundWindow"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowPlaygroundCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<PlaygroundWindowViewModel>();
            await ShowPlaygroundWindow.Handle(vm);
        });
    }

    private ReactiveCommand<string, RxVoid> CreateSelectLayoutCommand()
    {
        return ReactiveCommand.Create(
            (string name) =>
            {
                _logger.LogDebug("Switching to layout {Name}", name);
                var layout = LayoutProvider.Layouts.FirstOrDefault(l => l.Name == name);
                if (layout is null)
                    return;
                LayoutProvider.Current = layout;
            }
        );
    }

    private ReactiveCommand<RxVoid, RxVoid> CreateRefreshLayoutsCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            LayoutProvider.Reload();
        });
    }

    /// <summary>
    /// Display the <see cref="HelpWindow"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowHelpWindowCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<HelpWindowViewModel>();
            await ShowHelpWindow.Handle(vm);
        });
    }

    /// <summary>
    /// Display the <see cref="LogWindow"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowLogWindowCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<LogWindowViewModel>();
            await ShowLogWindow.Handle(vm);
        });
    }

    /// <summary>
    /// Display the <see cref="AboutWindow"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowAboutWindowCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<AboutWindowViewModel>();
            await ShowAboutWindow.Handle(vm);
        });
    }

    /// <summary>
    /// Display the <see cref="KeybindsDialog"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowKeybindsDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<KeybindsDialogViewModel>();
            await ShowKeybindsDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Display the <see cref="ConfigDialog"/>
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowConfigDialogCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<ConfigDialogViewModel>();
            await ShowConfigDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Open the issue tracker in the user's default browser
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateOpenIssueTrackerCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            await OpenIssueTracker.Handle(RxVoid.Default);
        });
    }

    /// <summary>
    /// Show command palette
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShowCommandPaletteCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var vm = _vmFactory.Create<CommandPaletteDialogViewModel>(
                OpenDocumentCommand,
                ExecuteScriptCommand
            );
            await ShowCommandPaletteDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Toggle fullscreen
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateToggleFullscreenCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            await ToggleFullscreen.Handle(RxVoid.Default);
        });
    }

    /// <summary>
    /// Remove workspace from the project
    /// </summary>
    private ReactiveCommand<int, RxVoid> CreateRemoveDocumentFromProjectCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (int id) =>
            {
                _logger.LogDebug(
                    "Displaying message to confirm removal of document {Id} from project",
                    id
                );

                var boxResult = await _messageBoxService.ShowAsync(
                    I18N.Other.MsgBox_RemoveDocument_Title,
                    $"{I18N.Other.MsgBox_RemoveDocument_Body}\n\n{I18N.Other.MsgBox_RemoveDocument_Disclaimer}",
                    MsgBoxButtonSet.YesNo,
                    MsgBoxButton.Yes,
                    MaterialIconKind.QuestionMark
                );

                if (boxResult == MsgBoxButton.Yes)
                {
                    ProjectProvider.Current.RemoveWorkspace(id);
                }
            }
        );
    }

    /// <summary>
    /// Remove folder from the project
    /// </summary>
    private ReactiveCommand<int, RxVoid> CreateRemoveFolderFromProjectCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (int id) =>
            {
                _logger.LogDebug(
                    "Displaying message to confirm removal of directory {Id} from project",
                    id
                );

                var boxResult = await _messageBoxService.ShowAsync(
                    I18N.Other.MsgBox_RemoveDirectory_Title,
                    $"{I18N.Other.MsgBox_RemoveDirectory_Body}\n\n{I18N.Other.MsgBox_RemoveDirectory_Disclaimer}",
                    MsgBoxButtonSet.YesNo,
                    MsgBoxButton.Yes,
                    MaterialIconKind.QuestionMark
                );

                if (boxResult == MsgBoxButton.Yes)
                {
                    ProjectProvider.Current.RemoveDirectory(id);
                }
            }
        );
    }

    /// <summary>
    /// Rename project folder
    /// </summary>
    private ReactiveCommand<int, RxVoid> CreateRenameFolderCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (int id) =>
            {
                if (ProjectProvider.Current.FindItemById(id) is not DirectoryItem dirItem)
                    return;

                _logger.LogDebug(
                    "Displaying input box for rename of folder {Id} ({DirItemTitle})",
                    id,
                    dirItem.Title
                );

                var result = await _messageBoxService.ShowInputAsync(
                    I18N.Other.MsgBox_NameDirectory_Title,
                    I18N.Other.MsgBox_NameDirectory_Body,
                    dirItem.Title,
                    MsgBoxButtonSet.OkCancel,
                    MsgBoxButton.Ok,
                    MaterialIconKind.Rename
                );

                if (result is null)
                    return;

                var (boxResult, userInput) = result.Value;

                if (boxResult == MsgBoxButton.Ok && !string.IsNullOrWhiteSpace(userInput))
                {
                    dirItem.Name = userInput;
                }
            }
        );
    }

    /// <summary>
    /// Rename project document
    /// </summary>
    private ReactiveCommand<int, RxVoid> CreateRenameDocumentCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (int id) =>
            {
                if (ProjectProvider.Current.FindItemById(id) is not DocumentItem docItem)
                    return;

                _logger.LogDebug(
                    "Displaying input box for rename of document {Id} ({DocItemTitle})",
                    id,
                    docItem.Title
                );

                var result = await _messageBoxService.ShowInputAsync(
                    I18N.Other.MsgBox_NameDocument_Title,
                    I18N.Other.MsgBox_NameDocument_Body,
                    docItem.Title,
                    MsgBoxButtonSet.OkCancel,
                    MsgBoxButton.Ok,
                    MaterialIconKind.Rename
                );

                if (result is null)
                    return;

                var (boxResult, userInput) = result.Value;

                if (boxResult == MsgBoxButton.Ok && !string.IsNullOrWhiteSpace(userInput))
                {
                    docItem.Name = userInput;
                }
            }
        );
    }

    /// <summary>
    /// Add a folder to an existing project folder
    /// </summary>
    private ReactiveCommand<int, RxVoid> CreateCreateFolderCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (int parentId) =>
            {
                if (
                    parentId != -1
                    && ProjectProvider.Current.FindItemById(parentId) is not DirectoryItem
                )
                    return;

                _logger.LogDebug(
                    "Displaying input box for creation of folder in parent {Id}",
                    parentId
                );

                var result = await _messageBoxService.ShowInputAsync(
                    I18N.Other.MsgBox_NameDirectory_Title,
                    I18N.Other.MsgBox_NameDirectory_Body,
                    string.Empty,
                    MsgBoxButtonSet.OkCancel,
                    MsgBoxButton.Ok,
                    MaterialIconKind.Rename
                );

                if (result is null)
                    return;

                var (boxResult, userInput) = result.Value;

                if (boxResult != MsgBoxButton.Ok || string.IsNullOrWhiteSpace(userInput))
                    return;

                ProjectProvider.Current.AddDirectory(userInput, parentId);
            }
        );
    }

    /// <summary>
    /// Move an item to a folder
    /// </summary>
    private ReactiveCommand<int, RxVoid> CreateMoveToFolderCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (int id) =>
            {
                var prj = ProjectProvider.Current;
                if (prj.FindItemById(id) is null)
                    return;

                _logger.LogDebug("Displaying input box to move item {Id} to a folder", id);

                var folders = prj.GetAllOfType(ProjectItemType.Directory)
                    .Where(d => d.Id != id)
                    .Select(d => new FolderInformation { Id = d.Id, Name = d.Title })
                    .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase.WithNaturalSort())
                    .ToList();
                folders.Insert(
                    0,
                    new FolderInformation { Id = -1, Name = I18N.Other.SelectFolderDialog_Root }
                );

                var result = await ShowSelectFolderDialog.Handle(
                    _vmFactory.Create<SelectFolderDialogViewModel>(folders)
                );

                if (result is null)
                    return;

                prj.MoveToParent(id, result.Id);
            }
        );
    }

    /// <summary>
    /// Open project document
    /// </summary>
    private ReactiveCommand<int, RxVoid> CreateOpenDocumentCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (int id) =>
            {
                if (ProjectProvider.Current.FindItemById(id) is not DocumentItem docItem)
                    return;

                await TryLoadReferenced(docItem.Id);
            }
        );
    }

    /// <summary>
    /// Check if the configuration-specified spellcheck dictionary is installed
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateCheckSpellcheckDictionaryCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var culture = Configuration.SpellcheckCulture;
            if (!_spellcheckService.IsDictionaryInstalled(culture)) // Not installed
            {
                _logger.LogInformation(
                    "Prompting user to download dictionary for {Culture}",
                    culture
                );
                var lang = SpellcheckLanguage.AvailableLanguages.First(l => l.Locale == culture);
                var vm = _vmFactory.Create<InstallDictionaryDialogViewModel>(
                    _dictionaryService,
                    lang,
                    false
                );
                await ShowInstallDictionaryDialog.Handle(vm);
                _spellcheckService.RebuildDictionary();
            }
            else
                _spellcheckService.RebuildDictionary(); // Installed
        });
    }

    /// <summary>
    /// Finds the nearest keyframe to a frame
    /// </summary>
    /// <param name="frame">Starting frame</param>
    /// <param name="video">Video information</param>
    /// <returns>Nearest keyframe</returns>
    private static int FindNearestKeyframeTo(int frame, VideoInfo video)
    {
        var keyframes = video.Keyframes;
        var idx = keyframes.BinarySearch(frame);
        if (idx >= 0)
            return idx;

        idx = ~idx;
        if (idx <= 0)
            return keyframes[0];
        if (idx > keyframes.Length)
            return keyframes[^1];

        var before = keyframes[idx - 1];
        var after = keyframes[idx];

        // Return whichever is closer
        return Math.Abs(frame - before) <= Math.Abs(after - frame) ? before : after;
    }
}
