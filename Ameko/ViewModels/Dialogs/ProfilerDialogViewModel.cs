// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows.Input;
using Ameko.Services;
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

    public ICommand StartCommand { get; }
    public ICommand ExportCommand { get; }

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

        StartCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                Dispatcher.UIThread.Post(() => IsProcessing = true);
                logger.LogInformation("Starting profile operation!");

                var lastPercent = -1;

                Result = await workspace.MediaController.ProfileSubtitlesAsync(
                    document: workspace.Document,
                    selection: workspace.SelectionManager.SelectedEventCollection,
                    viewWidth: OverrideViewportSize ? ViewportWidth : -1,
                    viewHeight: OverrideViewportSize ? ViewportHeight : -1,
                    target: SelectedTarget,
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
    }
}
