// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Ameko.Services;
using Ameko.Utilities;
using AssCS.Utilities;
using Avalonia.Threading;
using Holo;
using Holo.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace Ameko.ViewModels.Dialogs;

public class ProfilerDialogViewModel : ViewModelBase
{
    private const string GarbageKey = "Profiler Font Directories";
    public Interaction<ProfileResult, RxVoid> DisplayProfileResult { get; }
    public Interaction<string, Uri?> SaveProfileAs { get; }

    /// <summary>
    /// Specify the width/height to render at instead of using the video resolution
    /// </summary>
    public bool OverrideViewportSize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = false;

    /// <summary>
    /// Width to render at when <see cref="OverrideViewportSize"/> is <see langword="true"/>
    /// </summary>
    public int ViewportWidth
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Height to render at when <see cref="OverrideViewportSize"/> is <see langword="true"/>
    /// </summary>
    public int ViewportHeight
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// What to profile
    /// </summary>
    public ProfileTarget SelectedTarget
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = ProfileTarget.All;

    public ObservableCollection<string> FontDirectories { get; }
    public ObservableCollection<string> FontDirSelection { get; } = [];
    public bool CanRemoveFontDirs => FontDirSelection.Count != 0;

    public bool IsProcessing
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = false;

    /// <summary>
    /// Profiling progress between 0 and 1
    /// </summary>
    public double CurrentProgress
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string Description { get; }

    /// <summary>
    /// Result to display
    /// </summary>
    public ProfileResult? Result
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Interaction<RxVoid, Uri[]> OpenDirectoryPicker { get; }

    public ICommand StartCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand AddFontDirCommand { get; }
    public ICommand RemoveFontDirCommand { get; }

    public ProfilerDialogViewModel(
        ILogger<ProfilerDialogViewModel> logger,
        IIoService ioService,
        Workspace workspace
    )
    {
        // Should be a given
        if (!workspace.MediaController.IsVideoLoaded)
            throw new InvalidOperationException("Video must be loaded to profile subtitles");

        Description = string.Format(I18N.Profiler.Profiler_Description, workspace.Title);

        ViewportWidth = workspace.MediaController.VideoInfo.Width;
        ViewportHeight = workspace.MediaController.VideoInfo.Height;

        DisplayProfileResult = new Interaction<ProfileResult, RxVoid>();
        SaveProfileAs = new Interaction<string, Uri?>();

        if (workspace.Document.GarbageManager.TryGetJson(GarbageKey, out string[]? value))
        {
            FontDirectories = new ObservableCollection<string>(value);
        }
        else
        {
            FontDirectories = [];
        }

        FontDirSelection.CollectionChanged += (_, _) =>
            this.RaisePropertyChanged(nameof(CanRemoveFontDirs));

        OpenDirectoryPicker = new Interaction<RxVoid, Uri[]>();

        StartCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                Dispatcher.UIThread.Post(() => IsProcessing = true);
                logger.LogInformation("Starting profile operation!");

                // Update font directories
                workspace.Document.GarbageManager.Remove(GarbageKey);

                Uri[]? preloadDirs = null;
                if (FontDirectories.Count > 0)
                {
                    workspace.Document.GarbageManager.SetJson(
                        GarbageKey,
                        FontDirectories.ToArray()
                    );

                    preloadDirs = FontDirectories
                        .Select(d => new Uri(
                            Path.Combine(
                                Path.GetDirectoryName(workspace.SavePath?.LocalPath) ?? "/",
                                d
                            )
                        ))
                        .ToArray();
                }

                var lastPercent = -1;

                Result = await workspace.MediaController.ProfileSubtitlesAsync(
                    document: workspace.Document,
                    selection: workspace.SelectionManager.SelectedEventCollection,
                    viewWidth: OverrideViewportSize ? ViewportWidth : -1,
                    viewHeight: OverrideViewportSize ? ViewportHeight : -1,
                    target: SelectedTarget,
                    fontDirectories: preloadDirs,
                    progressCallback: (current, total) =>
                    {
                        var progress = (double)current / total;
                        var percent = (int)(100.0d * progress);

                        if (percent == lastPercent)
                            return;

                        lastPercent = percent;
                        Dispatcher.UIThread.Post(() => CurrentProgress = progress);
                    }
                );

                Dispatcher.UIThread.Post(() => IsProcessing = false);

                if (Result is null)
                    throw new InvalidOperationException("Received a null profile result!");
                await DisplayProfileResult.Handle(Result.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to profile subtitles!");
            }
        });

        ExportCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (!Result.HasValue)
                return;

            await ioService.SaveProfileResult(SaveProfileAs, workspace, Result.Value);
        });

        AddFontDirCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var dirs = await OpenDirectoryPicker.Handle(RxVoid.Default);
            if (dirs is null or [])
                return;
            var wspDir = workspace.SavePath is not null
                ? Path.GetDirectoryName(workspace.SavePath.LocalPath) ?? "/"
                : "/";
            FontDirectories.AddRange(
                dirs.Select(d => PathExtensions.GetRelativePath(wspDir, d.LocalPath))
            );
        });

        RemoveFontDirCommand = ReactiveCommand.Create(() =>
        {
            FontDirectories.RemoveAll(f => FontDirSelection.Contains(f));
            FontDirSelection.Clear();
        });
    }
}
