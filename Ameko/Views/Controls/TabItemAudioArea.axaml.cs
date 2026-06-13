// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Reactive.Disposables.Fluent;
using Ameko.Renderers;
using Ameko.ViewModels.Controls;
using AssCS;
using AssCS.History;
using Avalonia;
using Avalonia.Input;
using Holo.Models;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Ameko.Views.Controls;

public partial class TabItemAudioArea : ReactiveUserControl<TabItemViewModel>
{
    public TabItemAudioArea()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.GetObservable(ViewModelProperty)
                .WhereNotNull()
                .Subscribe(vm =>
                {
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
                })
                .DisposeWith(disposables);
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

        var x = e.GetPosition(this).X;

        var mc = wsp.MediaController;
        var ms = Convert.ToInt64(x * mc.VisualizerScaleX + mc.VisualizerPositionMs);

        // When shift is pressed, don't snap
        // TODO: Add a config option for this
        var time =
            (e.KeyModifiers & KeyModifiers.Shift) == 0
                ? Time.FromMillis(mc.VideoInfo.MidpointFromMillis(ms))
                : Time.FromMillis(ms);

        var frame = mc.VideoInfo.FrameFromTime(time);
        var @event = wsp.SelectionManager.ActiveEvent;

        switch (e.Properties.PointerUpdateKind)
        {
            case PointerUpdateKind.LeftButtonPressed:
                if (time < @event.End)
                {
                    @event.Start = time;
                }
                else
                {
                    @event.Start = @event.End;
                    @event.End = time;
                }
                wsp.Commit(@event, ChangeType.ModifyEventMeta);
                break;
            case PointerUpdateKind.RightButtonPressed:
                if (time > @event.Start)
                {
                    @event.End = time;
                }
                else
                {
                    @event.End = @event.Start;
                    @event.Start = time;
                }
                wsp.Commit(@event, ChangeType.ModifyEventMeta);
                break;
            case PointerUpdateKind.MiddleButtonPressed:
                wsp.MediaController.SeekTo(frame);
                break;
        }
    }
}
