// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Ameko.DataModels;
using Ameko.Messages;
using Ameko.Utilities;
using Ameko.ViewModels.Dialogs;
using Ameko.Views.Dialogs;
using AssCS.IO;
using AssCS.Utilities;
using Holo;
using Holo.Configuration;
using Holo.IO;
using Holo.Media.Providers;
using Holo.Models;
using Holo.Providers;
using Material.Icons;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
using SkiaSharp;

namespace Ameko.Services;

public class IoService(
    IProjectProvider projectProvider,
    ITabFactory tabFactory,
    IFileSystem fileSystem,
    IMessageBoxService messageBoxService,
    IMessageService messageService,
    IWindowService windowService,
    IConfiguration configuration,
    IPersistence persistence,
    ILogger<IoService> logger
) : IIoService
{
    private static readonly AssWriter _assWriter = new(ConsumerService.AmekoInfo);

    /// <inheritdoc />
    public async Task<bool> SaveSubtitle(Interaction<string, Uri?> interaction, Workspace wsp)
    {
        logger.LogDebug("Preparing to save subtitle file {WspTitle}", wsp.Title);
        var uriChanged = wsp.SavePath is null;
        var uri = wsp.SavePath ?? await interaction.Handle(wsp.Title);

        if (uri is null)
        {
            logger.LogInformation("Saving {WspTitle} cancelled", wsp.Title);
            return false;
        }

        // Set the audio/video path, if applicable
        if (wsp.MediaController.IsVideoLoaded)
        {
            var dir = Path.GetDirectoryName(uri.LocalPath) ?? "/";
            var relPath = PathExtensions.GetRelativePath(dir, wsp.MediaController.VideoInfo.Path);
            wsp.Document.GarbageManager.Set("Video File", relPath);
            wsp.Document.GarbageManager.Set("Audio File", relPath);
            wsp.Document.GarbageManager.Set("Video Position", wsp.MediaController.CurrentFrame);
            if (!string.IsNullOrEmpty(wsp.MediaController.VideoInfo.KeyframesFile))
            {
                var relKfPath = PathExtensions.GetRelativePath(
                    dir,
                    wsp.MediaController.VideoInfo.KeyframesFile
                );
                wsp.Document.GarbageManager.Set("Keyframes File", relKfPath);
            }
        }

        wsp.Document.GarbageManager.Set("Active Line", wsp.SelectionManager.ActiveEvent.Index - 1);

        try
        {
            wsp.SavePath = uri;
            wsp.IsSaved = true;
            _assWriter.Write(wsp.Document, fileSystem, uri);
        }
        catch (Exception ex)
        {
            wsp.IsSaved = false;
            return await DisplayIoErrorMessageBox(ex, uri);
        }
        logger.LogInformation("Saved subtitle file {WspTitle}", wsp.Title);

        if (uriChanged)
        {
            persistence.AddRecentDocument(uri);
            var projItem = projectProvider.Current.FindItemById(wsp.Id);
            if (projItem is not null)
                projectProvider.Current.SetNameAndUri(projItem, wsp.Title, uri);
        }

        return true;
    }

    /// <inheritdoc />
    public bool SaveSubtitle(Workspace wsp, Uri path)
    {
        var origin = wsp.SavePath;

        // Set the audio/video path, if applicable
        if (origin is not null && wsp.MediaController.IsVideoLoaded)
        {
            var dir = Path.GetDirectoryName(origin.LocalPath) ?? "/";
            var relPath = PathExtensions.GetRelativePath(dir, wsp.MediaController.VideoInfo.Path);
            wsp.Document.GarbageManager.Set("Video File", relPath);
            wsp.Document.GarbageManager.Set("Audio File", relPath);
            wsp.Document.GarbageManager.Set("Video Position", wsp.MediaController.CurrentFrame);
            if (!string.IsNullOrEmpty(wsp.MediaController.VideoInfo.KeyframesFile))
            {
                var relKfPath = PathExtensions.GetRelativePath(
                    dir,
                    wsp.MediaController.VideoInfo.KeyframesFile
                );
                wsp.Document.GarbageManager.Set("Keyframes File", relKfPath);
            }
        }

        wsp.Document.GarbageManager.Set("Active Line", wsp.SelectionManager.ActiveEvent.Index - 1);

        try
        {
            _assWriter.Write(wsp.Document, fileSystem, path);
        }
        catch (Exception ex)
        {
            return DisplayIoErrorMessageBox(ex, path).Result;
        }
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SaveSubtitleAs(Interaction<string, Uri?> interaction, Workspace wsp)
    {
        logger.LogDebug("Preparing to save subtitle file {WspTitle}", wsp.Title);
        var isForking = wsp.SavePath is not null;
        var uri = await interaction.Handle(wsp.Title);

        if (uri is null)
        {
            logger.LogInformation("Saving {WspTitle} cancelled", wsp.Title);
            return false;
        }

        // Set the audio/video path, if applicable
        if (wsp.MediaController.IsVideoLoaded)
        {
            var dir = Path.GetDirectoryName(uri.LocalPath) ?? "/";
            var relPath = PathExtensions.GetRelativePath(dir, wsp.MediaController.VideoInfo.Path);
            wsp.Document.GarbageManager.Set("Video File", relPath);
            wsp.Document.GarbageManager.Set("Audio File", relPath);
            wsp.Document.GarbageManager.Set("Video Position", wsp.MediaController.CurrentFrame);
        }

        wsp.Document.GarbageManager.Set("Active Line", wsp.SelectionManager.ActiveEvent.Index - 1);

        try
        {
            wsp.SavePath = uri;
            wsp.IsSaved = true;
            _assWriter.Write(wsp.Document, fileSystem, uri);
        }
        catch (Exception ex)
        {
            wsp.IsSaved = false;
            return await DisplayIoErrorMessageBox(ex, uri);
        }

        logger.LogInformation("Saved subtitle file {WspTitle}", wsp.Title);

        var projItem = projectProvider.Current.FindItemById(wsp.Id);
        // The user is forking the file into a second file
        if (isForking)
        {
            if (projItem is DocumentItem current)
            {
                persistence.AddRecentDocument(uri);
                var original = projectProvider.Current.Copy(current);
                original.Workspace = null;
            }
        }

        if (projItem is not null)
            projectProvider.Current.SetNameAndUri(projItem, wsp.Title, uri);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExportSubtitle(Interaction<string, Uri?> interaction, Workspace wsp)
    {
        logger.LogDebug("Preparing to export subtitle file {WspTitle}", wsp.Title);
        var uri = await interaction.Handle(wsp.Title);

        if (uri is null)
        {
            logger.LogInformation("Exporting {WspTitle} cancelled", wsp.Title);
            return false;
        }

        try
        {
            var writer = new TxtWriter(ConsumerService.AmekoInfo);
            writer.Write(wsp.Document, fileSystem, uri, true);
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }

        logger.LogInformation("Exported {WspTitle}", wsp.Title);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SafeCloseWorkspace(
        Workspace wsp,
        Interaction<string, Uri?> saveAs,
        bool replaceIfLast = true
    )
    {
        logger.LogDebug("Closing workspace {WspTitle}", wsp.Title);
        var prj = projectProvider.Current;

        if (wsp.IsSaved)
        {
            wsp.MediaController.CloseVideo();
            prj.CloseDocument(wsp.Id, replaceIfLast);
            tabFactory.Release(wsp);
            return true;
        }

        logger.LogTrace(
            "Displaying message box because workspace {WspTitle} is not saved",
            wsp.Title
        );

        var boxResult = await messageBoxService.ShowAsync(
            I18N.Other.MsgBox_Save_Title,
            string.Format(I18N.Other.MsgBox_Save_Body, wsp.Title),
            MsgBoxButtonSet.YesNoCancel,
            MsgBoxButton.Yes,
            MaterialIconKind.QuestionMark
        );

        switch (boxResult)
        {
            case MsgBoxButton.Yes:
                var saved = await SaveSubtitle(saveAs, wsp);
                if (!saved)
                {
                    logger.LogInformation("Tab close operation aborted");
                    return false;
                }

                goto case MsgBoxButton.No; // lol
            case MsgBoxButton.No:
                wsp.MediaController.CloseVideo();
                prj.CloseDocument(wsp.Id, replaceIfLast);
                tabFactory.Release(wsp);
                return true;
            default:
                return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SafeCloseProject(Project prj, Interaction<string, Uri?> saveAs)
    {
        logger.LogDebug("Closing workspace {WspTitle}", prj.Title);
        if (prj.IsSaved)
            return true;

        logger.LogTrace(
            "Displaying message box because workspace {PrjTitle} is not saved",
            prj.Title
        );

        var boxResult = await messageBoxService.ShowAsync(
            I18N.Other.MsgBox_SaveProject_Title,
            string.Format(I18N.Other.MsgBox_SaveProject_Body, prj.Title),
            MsgBoxButtonSet.YesNoCancel,
            MsgBoxButton.Yes,
            MaterialIconKind.QuestionMark
        );

        switch (boxResult)
        {
            case MsgBoxButton.Yes:
                var saved = await SaveProject(saveAs, prj);
                if (saved)
                    return true;

                logger.LogInformation("Project close operation aborted");
                return false;
            case MsgBoxButton.No:
                return true;
            default:
                return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SaveProject(Interaction<string, Uri?> interaction, Project prj)
    {
        logger.LogDebug("Preparing to save project file {PrjTitle}", prj.Title);
        var uri = prj.SavePath ?? await interaction.Handle(prj.Title);

        if (uri is null)
        {
            logger.LogInformation("Saving {PrjTitle} cancelled", prj.Title);
            return false;
        }

        try
        {
            prj.SavePath = uri;
            prj.Save();
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }

        logger.LogInformation("Saved project file {PrjTitle}", prj.Title);
        return true;
    }

    /// <inheritdoc />
    public async Task<Workspace[]> OpenSubtitleFilesAsync(
        Interaction<RxVoid, Uri[]?> interaction,
        Project prj
    )
    {
        var uris = await interaction.Handle(RxVoid.Default);
        if (uris is null || uris.Length == 0)
            return [];

        var workspaces = new List<Workspace>();
        foreach (var uri in uris)
        {
            var result = await OpenSubtitleFileAsync(uri, prj);
            if (result is not null)
                workspaces.Add(result);
        }

        return workspaces.ToArray();
    }

    /// <inheritdoc />
    public async Task<Workspace[]> OpenSubtitleFilesAsync(Uri[] uris, Project prj)
    {
        var workspaces = new List<Workspace>();
        foreach (var uri in uris)
        {
            var result = await OpenSubtitleFileAsync(uri, prj);
            if (result is not null)
                workspaces.Add(result);
        }
        return workspaces.ToArray();
    }

    /// <summary>
    /// Load a subtitle file into a Workspace
    /// </summary>
    /// <param name="uri">URI to the subtitle file</param>
    /// <param name="prj">Project to add the workspace to</param>
    /// <returns>The created workspace</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the format is invalid</exception>
    public async Task<Workspace?> OpenSubtitleFileAsync(Uri uri, Project prj)
    {
        logger.LogDebug("Opening subtitle file {Uri}", uri);

        try
        {
            var ext = Path.GetExtension(uri.LocalPath);
            var doc = ext switch
            {
                ".ass" or ".ssa" => new AssParser().Parse(fileSystem, uri),
                ".srt" => new SrtParser().Parse(fileSystem, uri),
                ".txt" => new TxtParser().Parse(fileSystem, uri),
                _ => throw new ArgumentOutOfRangeException(nameof(uri)),
            };

            // Check for lame duck file
            int? lameDuckId = null;
            if (
                prj.LoadedWorkspaces.Count == 1
                && prj.WorkingSpace is { IsSaved: true, SavePath: null } lameDuck
            )
                lameDuckId = lameDuck.Id;

            Workspace wsp;

            if (ext is ".ass" or ".ssa")
            {
                wsp = prj.AddWorkspace(doc, uri);
                wsp.IsSaved = true;
                persistence.AddRecentDocument(uri);
            }
            else
            {
                // Non-ass sourced documents need to be re-saved as an ass file
                wsp = prj.AddWorkspace(doc);
                wsp.FallbackTitle = Path.GetFileName(uri.LocalPath); // With extension, for clarity
                wsp.IsSaved = false;
            }

            logger.LogInformation("Opened subtitle file {WspTitle}", wsp.Title);

            // If we're replacing a lame duck file, close it
            if (lameDuckId is not null)
            {
                prj.CloseDocument(lameDuckId.Value);
            }

            prj.WorkingSpace = wsp;
            return wsp;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException or IOException)
        {
            logger.LogError(ex, "Failed to parse file {UriLocalPath}", uri.LocalPath);
            await messageBoxService.ShowAsync(
                I18N.Resources.Error,
                $"{I18N.Resources.Error_FailedToParse}\n\n{ex.Message}",
                MsgBoxButtonSet.Ok,
                MsgBoxButton.Ok,
                MaterialIconKind.Error
            );
            return null;
        }
        catch (Exception ex)
        {
            await DisplayIoErrorMessageBox(ex, uri);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> OpenProjectFile(Uri uri, Interaction<string, Uri?> saveAs)
    {
        logger.LogDebug("Preparing to open project file");
        foreach (var wsp in projectProvider.Current.LoadedWorkspaces.ToArray())
        {
            await SafeCloseWorkspace(wsp, saveAs, false);
        }

        if (projectProvider.Current.LoadedWorkspaces.Count > 0)
        {
            logger.LogInformation(
                "Opening project file aborted - {LoadedWorkspacesCount} workspaces remain open",
                projectProvider.Current.LoadedWorkspaces.Count
            );
            return false;
        }

        try
        {
            projectProvider.Current = projectProvider.CreateFromFile(uri);
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }

        persistence.AddRecentProject(uri);
        logger.LogInformation("Loaded project file");
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> OpenProjectDirectory(Uri uri, Interaction<string, Uri?> saveAs)
    {
        logger.LogDebug("Preparing to open project directory");
        foreach (var wsp in projectProvider.Current.LoadedWorkspaces.ToArray())
        {
            await SafeCloseWorkspace(wsp, saveAs, false);
        }

        if (projectProvider.Current.LoadedWorkspaces.Count > 0)
        {
            logger.LogInformation(
                "Opening project directory aborted - {LoadedWorkspacesCount} workspaces remain open",
                projectProvider.Current.LoadedWorkspaces.Count
            );
            return false;
        }

        try
        {
            projectProvider.Current = projectProvider.CreateFromDirectory(uri);
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }

        logger.LogInformation("Loaded project directory");
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> AttachReferenceFile(
        Interaction<RxVoid, Uri?> interaction,
        Workspace wsp
    )
    {
        var uri = await interaction.Handle(RxVoid.Default);
        if (uri is null)
            return false;

        return await AttachReferenceFile(uri, wsp);
    }

    /// <inheritdoc />
    public async Task<bool> AttachReferenceFile(Uri uri, Workspace wsp)
    {
        logger.LogDebug("Preparing to attach a reference file");
        var ext = Path.GetExtension(uri.LocalPath);
        try
        {
            wsp.ReferenceFileManager.Reference = ext switch
            {
                ".ass" or ".ssa" => new AssParser().Parse(fileSystem, uri),
                ".srt" => new SrtParser().Parse(fileSystem, uri),
                _ => throw new ArgumentOutOfRangeException(nameof(uri)),
            };
            return true;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException or IOException)
        {
            logger.LogError(ex, "Failed to parse file {UriLocalPath}", uri.LocalPath);
            await messageBoxService.ShowAsync(
                I18N.Resources.Error,
                $"{I18N.Resources.Error_FailedToParse}\n\n{ex.Message}",
                MsgBoxButtonSet.Ok,
                MsgBoxButton.Ok,
                MaterialIconKind.Error
            );
            return false;
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ProcessProjectGarbageAsync(
        Workspace workspace,
        Project project,
        ISourceProvider.ProgressCallback? progressCallback = null
    )
    {
        var doc = workspace.Document;

        // Active line
        if (doc.GarbageManager.TryGetInt("Active Line", out var lineIdx))
        {
            var line = doc.EventManager.Events.FirstOrDefault(e => e.Index == lineIdx + 1);
            if (line is not null)
                workspace.SelectionManager.ForceSelect(line, [line]);
        }

        // Video
        if (!doc.GarbageManager.TryGetString("Video File", out var relVideoPath))
            return true;
        var videoPath = Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(workspace.SavePath?.LocalPath) ?? "/", relVideoPath)
        );

        if (!fileSystem.File.Exists(videoPath))
        {
            messageService.Enqueue(
                string.Format(I18N.Other.Message_VideoNotFound, Path.GetFileName(videoPath)),
                TimeSpan.FromSeconds(7)
            );
            return true;
        }

        // Stop here if media loading is ongoing (maybe video was drag-n-dropped in at the same time?)
        if (workspace.MediaController.IsLoadingFile)
        {
            logger.LogWarning(
                "Skipping referenced video load prompt because another video is already being loaded!"
            );
            return true;
        }

        // Ask the user if they want to open the video
        var result = await messageBoxService.ShowAsync(
            I18N.Other.MsgBox_LoadVideo_Title,
            $"{I18N.Other.MsgBox_LoadVideo_Body}\n\n{relVideoPath}",
            MsgBoxButtonSet.YesNo,
            MsgBoxButton.Yes
        );
        if (result != MsgBoxButton.Yes)
            return true;

        await OpenVideoFileAsync(new Uri(videoPath, UriKind.Absolute), workspace, progressCallback);

        if (doc.GarbageManager.TryGetInt("Video Position", out var frame))
            workspace.MediaController.SeekTo(frame.Value); // Seek for clamp safety

        // Keyframes
        if (!doc.GarbageManager.TryGetString("Keyframes File", out var relKfPath))
            return true;
        var kfPath = Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(workspace.SavePath?.LocalPath) ?? "/", relKfPath)
        );
        if (!fileSystem.File.Exists(kfPath))
        {
            messageService.Enqueue(
                string.Format(I18N.Other.Message_KeyframesNotFound, Path.GetFileName(kfPath)),
                TimeSpan.FromSeconds(7)
            );
            return true;
        }

        try
        {
            workspace.MediaController.OpenKeyframes(kfPath);
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, new Uri(kfPath));
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> OpenVideoFileAsync(
        Interaction<RxVoid, Uri?> interaction,
        Workspace workspace,
        ISourceProvider.ProgressCallback? progressCallback = null
    )
    {
        var uri = await interaction.Handle(RxVoid.Default);
        if (uri is null)
            return false;

        return await OpenVideoFileAsync(uri, workspace, progressCallback);
    }

    /// <inheritdoc />
    public async Task<bool> OpenVideoFileAsync(
        Uri uri,
        Workspace workspace,
        ISourceProvider.ProgressCallback? progressCallback = null
    )
    {
        if (!workspace.MediaController.IsEnabled)
        {
            await messageBoxService.ShowAsync(
                I18N.Other.MsgBox_MediaDisabled_Title,
                I18N.Other.MsgBox_MediaDisabled_Body,
                MsgBoxButtonSet.Ok,
                MsgBoxButton.Ok,
                MaterialIconKind.Error
            );
            return true;
        }

        try
        {
            var vLoaded = await workspace.MediaController.OpenVideoAsync(
                uri.LocalPath,
                progressCallback
            );
            if (!vLoaded)
            {
                logger.LogError("Failed to open video file");
                return false;
            }
            progressCallback?.Invoke(0, 1); // Reset
            await OpenAudioFileAsync(uri, workspace, progressCallback);
            workspace.MediaController.SetSubtitles(workspace.Document);
            return true;
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }
    }

    /// <inheritdoc />
    public async Task<bool> OpenAudioFileAsync(
        Interaction<RxVoid, Uri?> interaction,
        Workspace workspace,
        ISourceProvider.ProgressCallback? progressCallback = null
    )
    {
        var uri = await interaction.Handle(RxVoid.Default);
        if (uri is null)
            return false;

        return await OpenAudioFileAsync(uri, workspace, progressCallback);
    }

    /// <inheritdoc />
    public async Task<bool> OpenAudioFileAsync(
        Uri uri,
        Workspace workspace,
        ISourceProvider.ProgressCallback? progressCallback = null,
        bool allowAutoload = true
    )
    {
        try
        {
            var audioTracks = await workspace.MediaController.GetAudioTrackInfoAsync(uri.LocalPath);
            if (audioTracks.Length > 0)
            {
                var index = audioTracks[0].Index;
                if (audioTracks.Length > 1)
                {
                    var hashedPath = GetHashedFilename(uri.LocalPath);
                    var savedIdx = persistence.GetAudioTrackForVideo(hashedPath);

                    if (allowAutoload && configuration.AutoloadAudioTracks && savedIdx != -1) // Autoload the track
                    {
                        index = savedIdx;
                        messageService.Enqueue(
                            string.Format(I18N.Other.Message_AudioAutoloaded, index),
                            TimeSpan.FromSeconds(5)
                        );
                    }
                    else // Ask the user which track to load
                    {
                        // TODO: get this out of here, this sucks
                        var dialogResult = await windowService.ShowDialogAsync<SelectTrackMessage>(
                            new SelectTrackDialog
                            {
                                DataContext = new SelectTrackDialogViewModel(audioTracks),
                            }
                        );
                        index = dialogResult?.TrackIndex ?? audioTracks[0].Index;
                        persistence.SetAudioTrackForVideo(hashedPath, index); // Save the index for next time
                    }
                }
                var aResult = await workspace.MediaController.OpenAudioAsync(
                    uri.LocalPath,
                    index,
                    audioTracks.Length,
                    progressCallback
                );
                if (!aResult)
                {
                    logger.LogError("Failed to open audio file");
                }
            }
            progressCallback?.Invoke(0, 1); // Reset
            return true;
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }
    }

    /// <inheritdoc />
    public async Task<bool> OpenKeyframesAsync(
        Interaction<RxVoid, Uri?> interaction,
        Workspace workspace
    )
    {
        var uri = await interaction.Handle(RxVoid.Default);
        if (uri is null)
            return false;

        var kfPath = uri.LocalPath;

        try
        {
            return fileSystem.File.Exists(kfPath)
                && workspace.MediaController.OpenKeyframes(kfPath);
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SaveFrameToFile(
        Interaction<RxVoid, Uri?> interaction,
        Workspace workspace,
        SaveFrameMode mode
    )
    {
        if (!workspace.MediaController.IsVideoLoaded)
            return false;

        string path;
        var videoInfo = workspace.MediaController.VideoInfo;
        var frame = workspace.MediaController.CurrentFrame;

        switch (configuration.SaveFrames)
        {
            case SaveFrames.WithVideo:
                var sourceName = Path.GetFileNameWithoutExtension(videoInfo.Path);
                var outputName =
                    $"{sourceName}-{frame}{mode switch {
                        SaveFrameMode.VideoOnly => "-video",
                        SaveFrameMode.SubtitlesOnly => "-subs",
                        _ => string.Empty
                    }}.png";
                path = Path.Combine(Path.GetDirectoryName(videoInfo.Path) ?? "/", outputName);
                break;
            case SaveFrames.WithSubtitles when workspace.SavePath is not null:
                sourceName = Path.GetFileNameWithoutExtension(workspace.SavePath!.LocalPath);
                outputName =
                    $"{sourceName}-{frame}{mode switch {
                        SaveFrameMode.VideoOnly => "-video",
                        SaveFrameMode.SubtitlesOnly => "-subs",
                        _ => string.Empty
                    }}.png";
                path = Path.Combine(
                    Path.GetDirectoryName(workspace.SavePath!.LocalPath) ?? "/",
                    outputName
                );
                break;
            default: // Subs aren't saved to disk, so we need to request a filename from the user
                var uri = await interaction.Handle(RxVoid.Default);
                if (uri is null)
                    return false;
                path = uri.LocalPath;
                break;
        }

        SKData? result;
        unsafe
        {
            result = GetImageData(workspace, workspace.MediaController.GetCurrentFrame(), mode);
        }

        if (result is null)
            return false;

        await using var fs = fileSystem.FileStream.New(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None
        );
        result.SaveTo(fs);

        messageService.Enqueue(
            string.Format(I18N.Other.Message_SavedFrame, path),
            TimeSpan.FromSeconds(5)
        );
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CopyFrameToClipboard(
        Interaction<string, RxVoid> interaction,
        Workspace workspace,
        SaveFrameMode mode
    )
    {
        if (!workspace.MediaController.IsVideoLoaded)
            return false;

        SKData? result;
        unsafe
        {
            result = GetImageData(workspace, workspace.MediaController.GetCurrentFrame(), mode);
        }

        if (result is null)
            return false;

        // Save to temp file because apparently copying the byte[] to the clipboard doesn't work
        var path = Path.Combine(Directories.CacheHome, $"{DateTime.Now:yyyyMMddHHmmssfff}.png");
        await using var fs = fileSystem.FileStream.New(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None
        );
        result.SaveTo(fs);

        await interaction.Handle(path);

        messageService.Enqueue(I18N.Other.Message_CopiedFrame, TimeSpan.FromSeconds(5));
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SaveProfileResult(
        Interaction<string, Uri?> interaction,
        Workspace wsp,
        ProfileResult result
    )
    {
        logger.LogDebug("Preparing to save profile result for workspace {WspTitle}", wsp.Title);

        var uri = await interaction.Handle(wsp.Title);
        if (uri is null)
        {
            logger.LogInformation("Saving profile result for {WspTitle} cancelled", wsp.Title);
            return false;
        }

        try
        {
            const string Header = "Frame,RenderTimeMs,ImageSizeKp,ImageCount";
            var sb = new StringBuilder();
            sb.AppendLine(Header);

            for (var i = 0; i < result.Frames.Length; i++)
            {
                sb.AppendLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0:F0},{1},{2},{3:F0}",
                        result.Frames[i],
                        result.RenderTimeMs[i],
                        result.ImageSizeKp[i],
                        result.ImageCount[i]
                    )
                );
            }

            await using var fs = fileSystem.FileStream.New(
                uri.LocalPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            await using var writer = new StreamWriter(fs);
            await writer.WriteAsync(sb.ToString());
            await writer.FlushAsync();
        }
        catch (Exception ex)
        {
            return await DisplayIoErrorMessageBox(ex, uri);
        }

        logger.LogInformation("Saved profile result file for {WspTitle}", wsp.Title);
        return true;
    }

    /// <summary>
    /// Get the SKData needed for saving or copying a frame
    /// </summary>
    /// <param name="workspace">Workspace</param>
    /// <param name="group">Frame group to copy or save</param>
    /// <param name="mode">Mode to use</param>
    /// <returns>SKData if successful</returns>
    private static unsafe SKData? GetImageData(
        Workspace workspace,
        FrameGroup* group,
        SaveFrameMode mode
    )
    {
        var videoInfo = workspace.MediaController.VideoInfo!;
        var imageInfo = new SKImageInfo
        {
            Width = videoInfo.Width,
            Height = videoInfo.Height,
            ColorType = SKColorType.Bgra8888,
            AlphaType = SKAlphaType.Premul,
        };
        var bmp = new SKBitmap(imageInfo);

        switch (mode)
        {
            case SaveFrameMode.VideoOnly:
                bmp.InstallPixels(imageInfo, (nint)group->VideoFrame->Data);
                break;
            case SaveFrameMode.SubtitlesOnly:
                bmp.InstallPixels(imageInfo, (nint)group->SubtitleFrame->Data);
                break;
            case SaveFrameMode.Full:
            {
                using var vid = new SKBitmap(imageInfo);
                using var sub = new SKBitmap(imageInfo);
                vid.InstallPixels(imageInfo, (nint)group->VideoFrame->Data);
                sub.InstallPixels(imageInfo, (nint)group->SubtitleFrame->Data);

                var canvas = new SKCanvas(bmp);
                canvas.DrawBitmap(vid, 0, 0);
                canvas.DrawBitmap(sub, 0, 0);
                canvas.Flush();
                break;
            }
        }

        return SKImage.FromBitmap(bmp).Encode(SKEncodedImageFormat.Png, 100);
    }

    /// <summary>
    /// Hash a filename
    /// </summary>
    /// <param name="filePath">File being loaded</param>
    /// <returns>Filepath</returns>
    private static string GetHashedFilename(string filePath)
    {
        // Try to get the last-modified time
        var modified = File.Exists(filePath)
            ? new FileInfo(filePath).LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"{filePath}-{modified}"));
        return Base64Url.EncodeToString(bytes);
    }

    /// <summary>
    /// Display a message box detailing the error
    /// </summary>
    /// <param name="ex">Exception</param>
    /// <param name="uri">URI to the failing file, if applicable</param>
    /// <returns><see langword="false"/></returns>
    private async Task<bool> DisplayIoErrorMessageBox(Exception ex, Uri? uri = null)
    {
        var title = I18N.Other.MsgBox_IoError_Title;
        string message;

        switch (ex)
        {
            case UnauthorizedAccessException when uri is not null:
                logger.LogError(
                    "Failed to access file {Path}: {Message}",
                    uri.LocalPath,
                    ex.Message
                );
                message = $"{I18N.Other.MsgBox_IoError_Body_Unauthorized}\n{ex.Message}";
                break;
            case UnauthorizedAccessException when uri is null:
                logger.LogError("Failed to access file: {Message}", ex.Message);
                message = $"{I18N.Other.MsgBox_IoError_Body_IoError}\n{ex.Message}";
                break;
            case IOException when uri is not null:
                logger.LogError(
                    "Failed to access file {Path}: {Message}",
                    uri.LocalPath,
                    ex.Message
                );
                message = $"{I18N.Other.MsgBox_IoError_Body_Unauthorized}\n{ex.Message}";
                break;
            case IOException when uri is null:
                logger.LogError("Failed to access file: {Message}", ex.Message);
                message = $"{I18N.Other.MsgBox_IoError_Body_IoError}\n{ex.Message}";
                break;
            case JsonException when uri is not null:
                logger.LogError(
                    "Failed to parse file {Path}: {Message}",
                    uri.LocalPath,
                    ex.Message
                );
                message = $"{I18N.Other.MsgBox_IoError_Body_Json}\n{ex.Message}";
                break;
            case JsonException when uri is null:
                logger.LogError("Failed to parse file: {Message}", ex.Message);
                message = $"{I18N.Other.MsgBox_IoError_Body_Json}\n{ex.Message}";
                break;
            default:
                logger.LogError("Failed to access file: {Message}", ex.Message);
                message = $"{I18N.Other.MsgBox_IoError_Body_IoError}\n{ex.Message}";
                break;
        }

        await messageBoxService.ShowAsync(
            title,
            message,
            MsgBoxButtonSet.Ok,
            MsgBoxButton.Ok,
            MaterialIconKind.Error
        );
        return false;
    }
}
