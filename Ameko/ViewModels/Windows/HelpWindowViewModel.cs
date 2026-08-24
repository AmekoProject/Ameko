// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Ameko.DataModels;
using Avalonia.Platform;
using Holo.Configuration;
using Holo.Scripting;
using Markdig;
using Markdown.ColorCode;
using ReactiveUI;

namespace Ameko.ViewModels.Windows;

public class HelpWindowViewModel : ViewModelBase
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseColorCode()
        .Build();

    // private static readonly string DarkCss = new StreamReader(
    //     AssetLoader.Open(new Uri("avares://Ameko/Assets/Css/md.dark.css"))
    // ).ReadToEnd();

    private static readonly string LightCss = new StreamReader(
        AssetLoader.Open(new Uri("avares://Ameko/Assets/Css/md.light.css"))
    ).ReadToEnd();

    private static readonly HelpArticle[] HelpArticles =
    [
        new(I18N.Help.Help_Section_UserInterface, "user-interface.md"),
        new(I18N.Help.Help_Section_Projects, "projects.md"),
    ];

    private readonly IPackageManager _packageManager;
    private readonly IFileSystem _fileSystem;
    private readonly IConfiguration _config;

    public List<AmekoHelp> AmekoHelps
    {
        get
        {
            List<AmekoHelp> result = [];
            var culture = _config.Culture;

            foreach (var article in HelpArticles)
            {
                // Fall back to the English help if localized help for the article doesn't exist
                var culturePath = new Uri(
                    $"avares://Ameko/Assets/Help/{culture}/{article.FileName}"
                );
                var fallbackPath = new Uri($"avares://Ameko/Assets/Help/en-US/{article.FileName}");
                var uri = AssetLoader.Exists(culturePath) ? culturePath : fallbackPath;

                var md = new StreamReader(AssetLoader.Open(uri)).ReadToEnd();

                var sb = new StringBuilder();
                sb.AppendLine("<!doctype html>");
                sb.AppendLine("<html><style>");
                sb.AppendLine(LightCss);
                sb.AppendLine("</style>");
                sb.AppendLine("<body><article class=\"markdown-body\">");
                sb.AppendLine(Markdig.Markdown.ToHtml(md, Pipeline));
                sb.AppendLine("</article></body></html>");

                result.Add(new AmekoHelp { DisplayName = article.Name, Content = sb.ToString() });
            }
            return result;
        }
    }

    public List<ScriptHelp> ScriptHelps
    {
        get
        {
            return _packageManager
                .InstalledPackages.Where(m => !string.IsNullOrEmpty(m.HelpUrl))
                .Select(m => new ScriptHelp
                {
                    QualifiedName = m.QualifiedName,
                    DisplayName = m.DisplayName,
                    Content = BuildScriptHelp(PackageManager.HelpPath(m)),
                })
                .ToList();
        }
    }

    public int SelectedTabIndex
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ScriptHelp? SelectedScriptHelp
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public AmekoHelp? SelectedAmekoHelp
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool HasScriptHelps => ScriptHelps.Count > 0;

    public HelpWindowViewModel(
        IPackageManager packageManager,
        IFileSystem fileSystem,
        IConfiguration config
    )
    {
        _packageManager = packageManager;
        _fileSystem = fileSystem;
        _config = config;
    }

    public HelpWindowViewModel(
        IPackageManager packageManager,
        IFileSystem fileSystem,
        IConfiguration config,
        string qualifiedName
    )
    {
        _packageManager = packageManager;
        _fileSystem = fileSystem;
        _config = config;
        SelectedTabIndex = 1;
        SelectedScriptHelp = ScriptHelps.FirstOrDefault(h => h.QualifiedName == qualifiedName);
    }

    private string BuildScriptHelp(string path)
    {
        using var fs = _fileSystem.FileStream.New(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite
        );
        using var reader = new StreamReader(fs);
        var raw = reader.ReadToEnd();

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html><style>");
        sb.AppendLine(LightCss);
        sb.AppendLine("</style>");
        sb.AppendLine("<body><article class=\"markdown-body\">");
        sb.AppendLine(Markdig.Markdown.ToHtml(raw, Pipeline));
        sb.AppendLine("</article></body></html>");

        return sb.ToString();
    }

    private record HelpArticle(string Name, string FileName);
}
