// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using Avalonia.Platform;
using Markdig;
using Markdown.ColorCode;

namespace Ameko.ViewModels.Dialogs;

public class ChangelogDialogViewModel(string changelog) : ViewModelBase
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseColorCode()
        .Build();

    private static readonly string LightCss = new StreamReader(
        AssetLoader.Open(new Uri("avares://Ameko/Assets/Css/md.light.css"))
    ).ReadToEnd();

    public string ChangelogContent { get; } =
        $"<style>\n{LightCss}\n</style>\n<body class=\"markdown-body\">\n"
        + Markdig.Markdown.ToHtml(changelog, Pipeline)
        + "\n</body>";
}
