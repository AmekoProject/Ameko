// SPDX-License-Identifier: GPL-3.0-only

using System;
using Ameko.Renderers;
using Ameko.ViewModels.Controls;
using AssCS;
using AssCS.History;
using Avalonia;
using Avalonia.Input;
using Holo;
using Holo.Models;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace Ameko.Views.Controls;

public partial class TabItemAudioArea : ReactiveUserControl<TabItemViewModel>
{
    /// <summary>
    /// Types of dragging
    /// </summary>
    private enum DragMode
    {
        None,
        SetStart,
        SetEnd,
        EdgeStart,
        EdgeEnd,
        Seek,
    }

    private const double EdgeHitPx = 6d;
    private DragMode _dragMode = DragMode.None;

    public TabItemAudioArea()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            if (ViewModel is not { } vm)
                return;

            // TODO: Don't do this!!
            var mc = vm.Workspace.MediaController;
            var renderer = new OpenAlAudioRenderer(mc);
            renderer.Initialize();
            mc.PlaybackStarted += (_, e) =>
            {
                // Always play audio target, only play video target if not muted
                if (e.Target is PlaybackTarget.Audio || !mc.IsMuted)
                    renderer.Play(e.StartTime, e.GoalTime);
            };
            mc.PlaybackStopped += (_, _) =>
            {
                renderer.Stop();
            };

            new ActionDisposable(() => { }).DisposeWith(disposables);
        });
    }

    private void AudioTarget_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel is null)
        {
            e.Handled = true;
            return;
        }
        if (e.Delta.Y > 0)
            ViewModel.Workspace.MediaController.VisualizerPositionMs -= 250; // Quarter second
        if (e.Delta.Y < 0)
            ViewModel.Workspace.MediaController.VisualizerPositionMs += 250;
        e.Handled = true;
    }

    private void AudioTarget_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        var wsp = ViewModel?.Workspace;
        if (wsp is null || !wsp.MediaController.IsVideoLoaded || !wsp.MediaController.IsAudioLoaded)
            return;

        var modifiers = e.KeyModifiers;
        var @event = wsp.SelectionManager.ActiveEvent;
        var x = e.GetPosition(this).X;

        switch (e.Properties.PointerUpdateKind)
        {
            case PointerUpdateKind.LeftButtonPressed:
            {
                // Check if we're near the edge of the active event
                var startX = TimeToPosition(@event.Start);
                var endX = TimeToPosition(@event.End);

                if (Math.Abs(x - startX) <= EdgeHitPx) // Drag start
                {
                    _dragMode = DragMode.EdgeStart;
                }
                else if (Math.Abs(x - endX) <= EdgeHitPx) // Drag end
                {
                    _dragMode = DragMode.EdgeEnd;
                }
                else // Set start
                {
                    _dragMode = DragMode.SetStart;
                    var time = PositionToTime(x, modifiers);

                    if (time < @event.End)
                    {
                        @event.Start = time;
                    }
                    else
                    {
                        @event.Start = @event.End;
                        @event.End = time;
                    }
                }
                e.Pointer.Capture(sender as IInputElement);
                break;
            }
            case PointerUpdateKind.RightButtonPressed:
            {
                _dragMode = DragMode.SetEnd;
                var time = PositionToTime(x, modifiers);
                if (time > @event.Start)
                {
                    @event.End = time;
                }
                else
                {
                    @event.End = @event.Start;
                    @event.Start = time;
                }
                e.Pointer.Capture(sender as IInputElement);
                break;
            }
            case PointerUpdateKind.MiddleButtonPressed:
            {
                _dragMode = DragMode.Seek;
                var time = PositionToTime(x, modifiers);
                var frame = wsp.MediaController.VideoInfo.FrameFromTime(time);
                wsp.MediaController.SeekTo(frame);

                e.Pointer.Capture(sender as IInputElement);
                break;
            }
        }
    }

    private void AudioTarget_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        e.Handled = true;
        var wsp = ViewModel?.Workspace;
        if (wsp is null || !wsp.MediaController.IsVideoLoaded || !wsp.MediaController.IsAudioLoaded)
            return;

        var modifiers = e.KeyModifiers;
        var @event = wsp.SelectionManager.ActiveEvent;
        var x = e.GetPosition(this).X;

        if (_dragMode is DragMode.None)
        {
            if (sender is InputElement element)
            {
                var startX = TimeToPosition(@event.Start);
                var endX = TimeToPosition(@event.End);
                if (Math.Abs(x - startX) <= EdgeHitPx || Math.Abs(x - endX) <= EdgeHitPx)
                    element.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                else
                    element.Cursor = Cursor.Default;
            }
            return;
        }

        var time = PositionToTime(x, modifiers);
        switch (_dragMode)
        {
            case DragMode.SetStart:
                if (time < @event.End)
                {
                    @event.Start = time;
                }
                else
                {
                    @event.Start = @event.End;
                    @event.End = time;
                }
                break;
            case DragMode.SetEnd:
                if (time > @event.Start)
                {
                    @event.End = time;
                }
                else
                {
                    @event.End = @event.Start;
                    @event.Start = time;
                }
                break;
            case DragMode.EdgeStart:
                @event.Start = time < @event.End ? time : @event.End;
                break;
            case DragMode.EdgeEnd:
                @event.End = time > @event.Start ? time : @event.Start;
                break;
            case DragMode.Seek:
                var frame = wsp.MediaController.VideoInfo.FrameFromTime(time);
                wsp.MediaController.SeekTo(frame);
                break;
        }
    }

    private void AudioTarget_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        switch (_dragMode)
        {
            case DragMode.None:
                return;
            case DragMode.Seek:
            {
                _dragMode = DragMode.None;
                e.Pointer.Capture(null);

                if (sender is InputElement element)
                    element.Cursor = Cursor.Default;
                return;
            }
            default:
            {
                var wsp = ViewModel?.Workspace;
                wsp?.Commit(wsp.SelectionManager.ActiveEvent, ChangeType.ModifyEventMeta);

                _dragMode = DragMode.None;
                e.Pointer.Capture(null);

                if (sender is InputElement element)
                    element.Cursor = Cursor.Default;
                return;
            }
        }
    }

    private Time PositionToTime(double x, KeyModifiers modifiers)
    {
        var mc = ViewModel!.Workspace.MediaController;

        var startMs = GetClampedPositionMs(mc);
        var ms = Convert.ToInt32(x * mc.VisualizerScaleX + startMs);

        var mode = ViewModel.Configuration.TimingMode;
        var shift = (modifiers & KeyModifiers.Shift) != 0;
        var snap = mode is TimingMode.SnapToFrame ? !shift : shift;

        return snap
            ? Time.FromMillis(mc.VideoInfo!.RoundMillisToNearestFrame(ms))
            : Time.FromMillis(ms);
    }

    private double TimeToPosition(Time time)
    {
        var mc = ViewModel!.Workspace.MediaController;
        var startMs = GetClampedPositionMs(mc);
        return (time.TotalMilliseconds - startMs) / mc.VisualizerScaleX;
    }

    /// <summary>
    /// Re-implementation of the clamping code in Mizuki so we don't scroll past the end
    /// </summary>
    private static double GetClampedPositionMs(MediaController mc)
    {
        var totalDurationMs = mc.AudioInfo!.Duration;
        var visibleDurationMs = mc.VisualizerWidth * mc.VisualizerScaleX;

        var startMs = Convert.ToDouble(mc.VisualizerPositionMs);

        if (totalDurationMs > visibleDurationMs)
        {
            var maxStartMs = totalDurationMs - visibleDurationMs;
            if (startMs > maxStartMs)
                startMs = maxStartMs;
        }
        else
        {
            startMs = 0.0;
        }

        return startMs;
    }
}
