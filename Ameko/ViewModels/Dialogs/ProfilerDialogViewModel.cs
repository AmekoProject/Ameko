// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows.Input;
using Avalonia.Threading;
using Holo;
using Holo.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace Ameko.ViewModels.Dialogs;

public class ProfilerDialogViewModel : ViewModelBase
{
    public Workspace Workspace { get; }

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

    public bool IsStartButtonEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    /// <summary>
    /// Profiling progress between 0 and 1
    /// </summary>
    public double CurrentProgress
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

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

    public ProfilerDialogViewModel(ILogger<ProfilerDialogViewModel> logger, Workspace workspace)
    {
        Workspace = workspace;

        // Should be a given
        if (!workspace.MediaController.IsVideoLoaded)
            throw new InvalidOperationException("Video must be loaded to profile subtitles");

        ViewportWidth = workspace.MediaController.VideoInfo.Width;
        ViewportHeight = workspace.MediaController.VideoInfo.Height;

        StartCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                Dispatcher.UIThread.Post(() => IsStartButtonEnabled = false);
                logger.LogInformation("Starting profile operation!");

                Result = await workspace.MediaController.ProfileSubtitlesAsync(
                    document: workspace.Document,
                    selection: workspace.SelectionManager.SelectedEventCollection,
                    viewWidth: OverrideViewportSize ? ViewportWidth : -1,
                    viewHeight: OverrideViewportSize ? ViewportHeight : -1,
                    target: SelectedTarget,
                    progressCallback: (current, total) =>
                    {
                        var progress = (double)current / total;
                        Dispatcher.UIThread.Post(() => CurrentProgress = progress);
                    }
                );

                Dispatcher.UIThread.Post(() => CurrentProgress = 1d); // Make sure progress displays as 100%
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to profile subtitles!");
            }
        });
    }
}
