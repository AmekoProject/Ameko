// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Ameko.ViewModels.Dialogs;
using Avalonia.Platform.Storage;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ScottPlot;
using ScottPlot.MultiplotLayouts;

namespace Ameko.Views.Dialogs;

public partial class ProfilerDialog : ReactiveWindow<ProfilerDialogViewModel>
{
    private async Task DoShowSaveAsDialogAsync(IInteractionContext<string, Uri?> interaction)
    {
        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = I18N.Other.FileDialog_SaveProfileResult_Title,
                FileTypeChoices =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Csv)
                    {
                        Patterns = ["*.csv"],
                    },
                ],
                SuggestedFileName = interaction.Input,
            }
        );

        if (file is not null)
        {
            var path = file.Path;
            if (!Path.HasExtension(path.LocalPath))
                path = new Uri(Path.ChangeExtension(path.LocalPath, ".csv"));

            interaction.SetOutput(path);
            return;
        }
        interaction.SetOutput(null);
    }

    public ProfilerDialog()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is not null)
            {
                ViewModel.SaveProfileAs.RegisterHandler(DoShowSaveAsDialogAsync);
                ViewModel.DisplayProfileResult.RegisterHandler(context =>
                {
                    context.SetOutput(Unit.Default);
                    var data = context.Input;

                    var mp = ProfilePlot.Multiplot;
                    mp.Reset();
                    mp.AddPlots(3);

                    var renderTimePlot = mp.GetPlot(0);
                    var imageSizePlot = mp.GetPlot(1);
                    var imageCountPlot = mp.GetPlot(2);
                    var plots = new[] { renderTimePlot, imageSizePlot, imageCountPlot };

                    renderTimePlot.YLabel(I18N.Profiler.Profiler_Axis_RenderTime);
                    imageSizePlot.YLabel(I18N.Profiler.Profiler_Axis_ImageSize);
                    imageCountPlot.YLabel(I18N.Profiler.Profiler_Axis_ImageCount);

                    renderTimePlot.Axes.Bottom.IsVisible = false;
                    renderTimePlot.Axes.Bottom.Min = 0;
                    imageSizePlot.Axes.Bottom.IsVisible = false;
                    imageCountPlot.XLabel(I18N.Profiler.Profiler_Axis_Frame);

                    var sigRenderTime = renderTimePlot.Add.SignalXY(data.Frames, data.RenderTimeMs);
                    var sigImageSize = imageSizePlot.Add.SignalXY(data.Frames, data.ImageSizeKp);
                    var sigImageCount = imageCountPlot.Add.SignalXY(data.Frames, data.ImageCount);

                    sigRenderTime.Color = Colors.CornflowerBlue;
                    sigImageSize.Color = Colors.MediumSeaGreen;
                    sigImageCount.Color = Colors.Tomato;

                    mp.Layout = new Rows();

                    mp.SharedAxes.ShareX(plots);

                    foreach (var plot in plots)
                        plot.Axes.AutoScale();

                    ProfilePlot.Refresh();
                });
            }
        });
    }
}
