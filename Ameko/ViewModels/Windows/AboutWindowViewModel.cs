// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using Ameko.Services;
using Avalonia.Platform;

namespace Ameko.ViewModels.Windows;

public class AboutWindowViewModel : ViewModelBase
{
    public static string Version => VersionService.FullLabel;

    public static string GeneralContent =>
        LoadSectionContent("avares://Ameko/Assets/Text/About.html");

    public static string AssetsContent =>
        LoadSectionContent("avares://Ameko/Assets/Text/Assets.html");

    public static string LibrariesContent =>
        LoadSectionContent("avares://Ameko/Assets/Text/Libraries.html");

    private static string LoadSectionContent(string path)
    {
        return new StreamReader(AssetLoader.Open(new Uri(path))).ReadToEnd();
    }
}
