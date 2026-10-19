// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Ameko.Views.Sdk;
using Ameko.Views.Windows;
using Holo.Models;
using Holo.Providers;
using Holo.Scripting.Models;
using Material.Icons;

namespace Ameko.Services;

/// <summary>
/// Service for displaying message boxes
/// </summary>
/// <param name="mainWindow">Application main window</param>
public class MessageBoxService(MainWindow mainWindow) : IMessageBoxService
{
    /// <inheritdoc />
    public async Task<MsgBoxButton?> ShowAsync(
        string title,
        string text,
        MaterialIconKind iconKind = MaterialIconKind.Info
    )
    {
        var box = new MessageBox(title, text, iconKind: iconKind);
        return await box.ShowDialog<MsgBoxButton>(mainWindow);
    }

    /// <inheritdoc />
    public async Task<MsgBoxButton?> ShowAsync(
        string title,
        string text,
        MsgBoxButtonSet buttonSet,
        MsgBoxButton primary,
        MaterialIconKind iconKind = MaterialIconKind.Info
    )
    {
        var box = new MessageBox(title, text, buttonSet, primary, iconKind);
        return await box.ShowDialog<MsgBoxButton>(mainWindow);
    }

    /// <inheritdoc />
    public async Task<(MsgBoxButton, string)?> ShowInputAsync(
        string title,
        string text,
        string initialText,
        MsgBoxButtonSet buttonSet,
        MsgBoxButton primary,
        MaterialIconKind iconKind = MaterialIconKind.Information
    )
    {
        var box = new InputBox(title, text, initialText, buttonSet, primary, iconKind);
        var result = await box.ShowDialog<MsgBoxButton>(mainWindow);
        return (result, box.InputText);
    }

    /// <inheritdoc />
    public async Task<MsgBoxButton?> ShowAsync(InstallationResult result)
    {
        var box = new MessageBox(
            I18N.PkgMan.PkgManWindow_Title,
            result switch
            {
                InstallationResult.Success => I18N.PkgMan.PkgMan_Result_Success,
                InstallationResult.Failure => I18N.PkgMan.PkgMan_Result_Failure,
                InstallationResult.DependencyNotFound => I18N.PkgMan.PkgMan_Result_DepNotFound,
                InstallationResult.AlreadyInstalled => I18N.PkgMan.PkgMan_Result_AlreadyInstalled,
                InstallationResult.FilesystemFailure => I18N.PkgMan.PkgMan_Result_FS_Failure,
                InstallationResult.NotInstalled => I18N.PkgMan.PkgMan_Result_NotInstalled,
                InstallationResult.InvalidName => I18N.PkgMan.PkgMan_Result_InvalidName,
                InstallationResult.IsRequiredDependency => I18N.PkgMan.PkgMan_Result_IsRequiredDep,
                _ => throw new ArgumentOutOfRangeException(nameof(result)),
            },
            MsgBoxButtonSet.Ok,
            MsgBoxButton.Ok,
            result switch
            {
                InstallationResult.Success => MaterialIconKind.CheckBold,
                _ => MaterialIconKind.ExclamationThick,
            }
        );
        return await box.ShowDialog<MsgBoxButton>(mainWindow);
    }
}
