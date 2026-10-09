// SPDX-License-Identifier: GPL-3.0-only

using Ameko.DataModels;
using Ameko.Messages;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class ResolutionDialogViewModel : ViewModelBase
{
    public ResolutionDialogViewModel(
        string? playResX,
        string? playResY,
        int videoResX,
        int videoResY
    )
    {
        VideoResX = videoResX;
        VideoResY = videoResY;

        if (!string.IsNullOrEmpty(playResX) && !string.IsNullOrEmpty(playResY))
        {
            CanSetToScriptRes = true;
            ScriptResX = playResX;
            ScriptResY = playResY;
        }
        else
        {
            CanSetToScriptRes = false;
            ScriptResX = string.Empty;
            ScriptResY = string.Empty;
        }

        OkCommand = ReactiveCommand.Create(() => new ResolutionSelectionMessage(SelectedOption));
    }

    public int VideoResX { get; }
    public int VideoResY { get; }
    public string ScriptResX { get; }
    public string ScriptResY { get; }
    public bool CanSetToScriptRes { get; }

    public ReactiveCommand<RxVoid, ResolutionSelectionMessage> OkCommand { get; }

    public string VideoResOption =>
        string.Format(I18N.Other.ResDialog_Option_SetToVideo, VideoResX, VideoResY);

    public string ScriptResOption =>
        CanSetToScriptRes
            ? string.Format(I18N.Other.ResDialog_Option_SetToScript, ScriptResX, ScriptResY)
            : I18N.Other.ResDialog_Option_SetToScript_Undefined;

    public ResolutionSelection SelectedOption { get; set; } = ResolutionSelection.SetToVideoRes;
}
