// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Net.Http;
using Ameko.Messages;
using AvaloniaEdit.Document;
using Holo.Scripting.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class SourceViewerDialogViewModel : ViewModelBase
{
    public string Title { get; }
    public bool IsLocalScript { get; }
    public TextDocument Document { get; }

    public ReactiveCommand<RxVoid, PackageType> LoadSourceCommand { get; }
    public ReactiveCommand<RxVoid, EmptyMessage> SaveCommand { get; }

    public SourceViewerDialogViewModel(
        ILogger<SourceViewerDialogViewModel> logger,
        HttpClient httpClient,
        PackageType scriptType,
        string scriptName,
        Uri scriptUri
    )
    {
        Title = string.Format(I18N.Other.SourceViewer_Title, scriptName);
        IsLocalScript = scriptUri.IsFile;
        Document = new TextDocument();

        LoadSourceCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                if (IsLocalScript)
                {
                    await using var fs = new FileStream(
                        scriptUri.LocalPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite
                    );
                    using var reader = new StreamReader(fs);
                    Document.Text = reader.ReadToEnd();
                }
                else
                {
                    await using var dlStream = await httpClient.GetStreamAsync(scriptUri);
                    using var reader = new StreamReader(dlStream);
                    Document.Text = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read from {ScriptPath}", scriptUri.LocalPath);
                Document.Text = string.Empty;
            }

            Document.UndoStack.ClearAll();
            return scriptType;
        });

        SaveCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            // Just in case
            if (!IsLocalScript)
                return new EmptyMessage();

            try
            {
                await using var fs = new FileStream(
                    scriptUri.LocalPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                await using var writer = new StreamWriter(fs);
                await writer.WriteAsync(Document.Text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write to {ScriptPath}", scriptUri.LocalPath);
            }

            return new EmptyMessage();
        });
    }
}
