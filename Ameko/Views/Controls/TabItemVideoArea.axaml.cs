// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Controls;
using Avalonia.Input;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace Ameko.Views.Controls;

public partial class TabItemVideoArea : ReactiveUserControl<TabItemViewModel>
{
    public TabItemVideoArea()
    {
        InitializeComponent();

        this.WhenActivated(
            (MultipleDisposable disposables) =>
            {
                if (ViewModel is not { } vm)
                    return;

                SeekBar.DragStarted += (_, _) =>
                {
                    vm.Workspace.MediaController.Pause();
                };
                SeekBar.DragEnded += (_, _) =>
                {
                    if (vm.Workspace.MediaController.IsPaused)
                        vm.Workspace.MediaController.Resume();
                };
                new ActionDisposable(() => { }).DisposeWith(disposables);
            }
        );
    }

    private void VideoTarget_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (ViewModel is null)
            return;

        switch (e.KeyModifiers)
        {
            case KeyModifiers.Control:
                if (e.Delta.Y > 0)
                    ViewModel.ZoomInCommand.Execute(null);
                else
                    ViewModel.ZoomOutCommand.Execute(null);
                e.Handled = true;
                break;
            case KeyModifiers.Alt:
                // Only seek if not playing
                var controller = ViewModel.Workspace.MediaController;
                if (!controller.IsVideoPlaying)
                    controller.SeekTo(controller.CurrentFrame - (int)e.Delta.Y); // Down = forwards
                e.Handled = true;
                break;
            default:
                e.Handled = false; // Fall through
                break;
        }
    }
}
