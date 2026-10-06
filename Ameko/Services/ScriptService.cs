// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Ameko.DataModels;
using Ameko.ViewModels.Dialogs;
using Ameko.Views.Dialogs;
using AssCS.History;
using Avalonia.Threading;
using CSScriptLib;
using Holo.Configuration.Keybinds;
using Holo.IO;
using Holo.Providers;
using Holo.Scripting;
using Holo.Scripting.Models;
using Jint;
using Jint.Native;
using Microsoft.Extensions.Logging;

namespace Ameko.Services;

/// <summary>
/// Service for executing <see cref="HoloScript"/>s
/// </summary>
public class ScriptService : IScriptService
{
    private static readonly Uri ScriptsRoot = new(Path.Combine(Directories.DataHome, "scripts"));

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFileSystem _fileSystem;
    private readonly IProjectProvider _projectProvider;
    private readonly IKeybindRegistrar _keybindRegistrar;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IWindowService _windowService;
    private readonly ObservableCollection<IHoloExecutable> _scripts;
    private readonly Dictionary<string, HoloScript?> _scriptMap;
    private readonly Dictionary<string, HoloScriptlet?> _scriptletMap;

    /// <inheritdoc />
    public AssCS.Utilities.ReadOnlyObservableCollection<IHoloExecutable> Scripts { get; }

    /// <inheritdoc />
    public bool TryGetScript(string qualifiedName, [NotNullWhen(true)] out HoloScript? script)
    {
        return _scriptMap.TryGetValue(qualifiedName, out script);
    }

    /// <inheritdoc />
    public bool TryGetScriptlet(string qualifiedName, [NotNullWhen(true)] out HoloScriptlet? script)
    {
        return _scriptletMap.TryGetValue(qualifiedName, out script);
    }

    /// <inheritdoc />
    public async Task<ExecutionResult> ExecuteScriptAsync(
        string qualifiedName,
        ScriptArgs? args = null
    )
    {
        // Try running as a script
        if (TryGetScript(qualifiedName, out var script))
        {
            try
            {
                var result = await script.ExecuteAsync(null, args);

                if (
                    script.Info.LogDisplay is LogDisplay.Forced
                    || result.Status is not ExecutionStatus.Success
                )
                {
                    await DisplayLogDialog(script.Info.DisplayName, result, script.Logger);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occured during script execution");
                _logger.LogError("{Error}", ex.Message);
                var result = new ExecutionResult
                {
                    Status = ExecutionStatus.Failure,
                    Message = I18N.Other.ScriptExecution_Exception + Environment.NewLine + ex,
                };
                await DisplayLogDialog(script.Info.DisplayName, result, script.Logger);
                return result;
            }
            finally
            {
                script.Logger.Reset();
            }
        }

        // Try running as an exported function

        if (qualifiedName.LastIndexOf('+') >= 0)
        {
            var scriptName = qualifiedName[..qualifiedName.LastIndexOf('+')];
            var methodName = qualifiedName[(qualifiedName.LastIndexOf('+') + 1)..];
            if (TryGetScript(scriptName, out script))
            {
                try
                {
                    var result = await script.ExecuteAsync(methodName, args);

                    if (
                        script.Info.LogDisplay is LogDisplay.Forced
                        || result.Status is not ExecutionStatus.Success
                    )
                    {
                        var methodDisplayName =
                            script
                                .Info.Exports.FirstOrDefault(m => m.QualifiedName == methodName)
                                ?.DisplayName
                            ?? methodName;
                        await DisplayLogDialog(methodDisplayName, result, script.Logger);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unhandled exception occured during script execution");
                    _logger.LogError("{Error}", ex.Message);
                    var result = new ExecutionResult
                    {
                        Status = ExecutionStatus.Failure,
                        Message = ex.ToString(),
                    };
                    var methodDisplayName =
                        script
                            .Info.Exports.FirstOrDefault(m => m.QualifiedName == methodName)
                            ?.DisplayName
                        ?? methodName;
                    await DisplayLogDialog(methodDisplayName, result, script.Logger);
                    return result;
                }
                finally
                {
                    script.Logger.Reset();
                }
            }
        }

        // Try running a scriptlet
        if (TryGetScriptlet(qualifiedName, out var scriptlet))
        {
            var logger = new HoloLogger(_loggerFactory.CreateLogger(scriptlet.Info.QualifiedName));
            var timeout = TimeSpan.FromSeconds(5);
            var engine = CreateJavaScriptEngine(logger, _projectProvider, timeout);

            try
            {
                var result = await engine.Execute(scriptlet.CompiledScript).InvokeAsync("execute");
                var isSuccess = result is not JsBoolean jsBool || jsBool.AsBoolean(); // false only if false explicitly returned

                if (scriptlet.Info.LogDisplay is LogDisplay.Forced || !isSuccess)
                {
                    await DisplayLogDialog(
                        scriptlet.Info.DisplayName,
                        ExecutionResult.Failure,
                        logger
                    );
                }

                return ExecutionResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occured during scriptlet execution");
                _logger.LogError("{Error}", ex.Message);
                var result = new ExecutionResult
                {
                    Status = ExecutionStatus.Failure,
                    Message = I18N.Other.ScriptExecution_Exception + Environment.NewLine + ex,
                };
                await DisplayLogDialog(scriptlet.Info.DisplayName, result, logger);
                return result;
            }
            finally
            {
                logger.Reset();
            }
        }

        // Not found
        _logger.LogError("Script or method not found: {QualifiedName}", qualifiedName);
        return new ExecutionResult
        {
            Status = ExecutionStatus.Failure,
            Message = $"Script or method not found: {qualifiedName}",
        };
    }

    /// <inheritdoc />
    public async Task<string> ExecutePlaygroundScriptAsync(
        string content,
        PlaygroundLanguage language
    )
    {
        switch (language)
        {
            case PlaygroundLanguage.JavaScript:
            {
                try
                {
                    var logger = new HoloLogger(_loggerFactory.CreateLogger("Playground"));
                    var timeout = TimeSpan.FromSeconds(5);
                    var engine = CreateJavaScriptEngine(logger, _projectProvider, timeout);
                    await engine.ExecuteAsync(content);
                    return I18N.Playground.Playground_Status_Success;
                }
                catch (Exception ex)
                {
                    return string.Format(I18N.Playground.Playground_Status_Failure, ex);
                }
            }
            case PlaygroundLanguage.CSharp:
            {
                try
                {
                    _ = CSScript.Evaluator.Eval(content);
                    return I18N.Playground.Playground_Status_Success;
                }
                catch (Exception ex)
                {
                    return string.Format(I18N.Playground.Playground_Status_Failure, ex);
                }
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(language), language, null);
        }
    }

    /// <inheritdoc />
    public async Task Reload(bool isManual)
    {
        if (Program.IsInSafeMode)
        {
            _logger.LogInformation("Script loading skipped due to Ameko running in Safe Mode");
            await Dispatcher.UIThread.InvokeAsync(() => Reloaded?.Invoke(this, EventArgs.Empty));
            return;
        }

        _logger.LogInformation("Reloading scripts...");
        if (!Directory.Exists(ScriptsRoot.LocalPath))
            Directory.CreateDirectory(ScriptsRoot.LocalPath);

        var scriptPaths = Directory
            .EnumerateFiles(ScriptsRoot.LocalPath, "*.cs")
            .Where(f => !f.EndsWith(".lib.cs"));

        List<HoloScript> loadedScripts = [];

        foreach (var path in scriptPaths)
        {
            try
            {
                _logger.LogDebug("Loading script {Path}...", path);
                var script = CSScript.Evaluator.LoadFile<HoloScript>(path);
                if (script is null)
                {
                    _logger.LogWarning("Script {Path} was invalid!", path);
                    continue;
                }

                loadedScripts.Add(script);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load script");
                _logger.LogError("{Error}", ex.Message);
            }
        }

        List<HoloScriptlet> loadedScriptlets = [];

        var scriptletPaths = Directory.EnumerateFiles(ScriptsRoot.LocalPath, "*.js");

        foreach (var path in scriptletPaths)
        {
            try
            {
                _logger.LogDebug("Loading scriptlet {Path}...", path);
                await using var fs = _fileSystem.FileStream.New(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );
                using var reader = new StreamReader(fs);
                var compiled = Engine.PrepareScript(await reader.ReadToEndAsync());
                var scriptletInfo = await new Engine()
                    .Execute(compiled)
                    .EvaluateAsync("scriptInfo");
                loadedScriptlets.Add(
                    new HoloScriptlet
                    {
                        Info = new PackageInfo
                        {
                            DisplayName = scriptletInfo.Get("displayName").ToString(),
                            QualifiedName = scriptletInfo.Get("qualifiedName").ToString(),
                        },
                        CompiledScript = compiled,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while reloading scripts");
            }
        }

        // For informational purposes
        var libCount = Directory.GetFiles(ScriptsRoot.LocalPath, "*.lib.cs").Length;
        _logger.LogInformation(
            "Reloaded {LoadedScriptsCount} scripts ({LibCount} libraries) and {LoadedScriptletsCount} scriptlets",
            loadedScripts.Count,
            libCount,
            loadedScriptlets.Count
        );

        // Update UI-bound collections and fire event on the UI thread for safety
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _scripts.Clear();
            _scriptMap.Clear();
            _scriptletMap.Clear();

            var qnames = new HashSet<string>();

            foreach (var script in loadedScripts)
            {
                _scripts.Add(script);
                _scriptMap.Add(script.Info.QualifiedName, script);

                if (!script.Info.Headless)
                    qnames.Add(script.Info.QualifiedName);
                foreach (var export in script.Info.Exports)
                    qnames.Add($"{script.Info.QualifiedName}+{export.QualifiedName}");
            }

            foreach (var script in loadedScriptlets)
            {
                _scripts.Add(script);
                _scriptletMap.Add(script.Info.QualifiedName, script);

                qnames.Add(script.Info.QualifiedName);
            }

            // Register the script qualified names for keybinding
            _keybindRegistrar.RegisterKeybinds(
                qnames.Select(name => new Keybind(name, null, KeybindContext.None)).ToList(),
                true
            );

            // Fire event
            Reloaded?.Invoke(this, EventArgs.Empty);
        });

        // Display message box (if manually invoked)
        if (isManual)
        {
            await _messageBoxService.ShowAsync(
                I18N.Other.MsgBox_ScriptService_Title,
                I18N.Other.MsgBox_ScriptService_Reload_Body
            );
        }
    }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? Reloaded;

    private async Task DisplayLogDialog(
        string displayName,
        ExecutionResult result,
        HoloLogger logger
    )
    {
        var dialog = new ScriptLogDialog
        {
            DataContext = new ScriptLogDialogViewModel(displayName, result, logger),
        };
        await _windowService.ShowDialogAsync(dialog);
    }

    private static Engine CreateJavaScriptEngine(
        HoloLogger logger,
        IProjectProvider projectProvider,
        TimeSpan timeoutInterval
    )
    {
        var engine = new Engine(options =>
        {
            options.AllowClr(); // TODO: Do we want to keep this?
            options.LimitRecursion(500);
            options.TimeoutInterval(timeoutInterval);
        });

        engine.SetValue("ChangeType", typeof(ChangeType));
        engine.SetValue("log", new Action<string>(msg => JavaScriptApi.Log(logger, msg)));
        engine.SetValue("err", new Action<string>(msg => JavaScriptApi.Err(logger, msg)));
        engine.SetValue(
            "commitOne",
            new Action<AssCS.Event, ChangeType>(
                (active, changeType) => JavaScriptApi.CommitOne(projectProvider, active, changeType)
            )
        );
        engine.SetValue(
            "commitMany",
            new Action<IEnumerable<AssCS.Event>, ChangeType>(
                (selection, changeType) =>
                    JavaScriptApi.CommitMany(projectProvider, selection, changeType)
            )
        );
        engine.SetValue(
            "selectOne",
            new Action<AssCS.Event>(active => JavaScriptApi.SelectOne(projectProvider, active))
        );
        engine.SetValue(
            "selectMany",
            new Action<AssCS.Event, IEnumerable<AssCS.Event>>(
                (active, selection) => JavaScriptApi.SelectMany(projectProvider, active, selection)
            )
        );

        engine.SetValue("ProjectProvider", projectProvider);
        engine.Execute(
            """
            Object.defineProperty(globalThis, 'project', { get: function() { return ProjectProvider.Current; } });
            Object.defineProperty(globalThis, 'workspace', { get: function() { return ProjectProvider.Current.WorkingSpace; } });
            Object.defineProperty(globalThis, 'eventManager', { get: function() { return ProjectProvider.Current.WorkingSpace?.Document.EventManager; } });
            Object.defineProperty(globalThis, 'activeEvent', { get: function() { return ProjectProvider.Current.WorkingSpace?.SelectionManager.ActiveEvent; } });
            Object.defineProperty(globalThis, 'selectedEvents', {
                get: function() {
                    var col = ProjectProvider.Current.WorkingSpace?.SelectionManager.SelectedEventCollection;
                    return col ? Array.from(col) : [];
                }
            });
            Object.defineProperty(globalThis, 'events', {
                get: function() {
                    var col = ProjectProvider.Current.WorkingSpace?.Document.EventManager.Events;
                    return col ? Array.from(col) : [];
                }
            });
            """
        );
        return engine;
    }

    public ScriptService(
        ILogger<ScriptService> logger,
        ILoggerFactory loggerFactory,
        IFileSystem fileSystem,
        IProjectProvider projectProvider,
        IKeybindRegistrar keybindRegistrar,
        IMessageBoxService messageBoxService,
        IWindowService windowService
    )
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _fileSystem = fileSystem;
        _projectProvider = projectProvider;
        _keybindRegistrar = keybindRegistrar;
        _messageBoxService = messageBoxService;
        _windowService = windowService;

        _scripts = [];
        _scriptMap = [];
        _scriptletMap = [];
        Scripts = new AssCS.Utilities.ReadOnlyObservableCollection<IHoloExecutable>(_scripts);
        CSScripting.Globals.DefaultRoslynCompilationToScript = true;
    }

    /// <summary>
    /// API surface for Scriptlets and Playground scripts
    /// </summary>
    private static class JavaScriptApi
    {
        public static void Log(HoloLogger logger, string? message)
        {
            logger.LogInformation(message ?? "null");
        }

        public static void Err(HoloLogger logger, string? message)
        {
            logger.LogError(message ?? "null");
        }

        public static void CommitOne(
            IProjectProvider projectProvider,
            AssCS.Event active,
            ChangeType changeType
        )
        {
            projectProvider.Current.WorkingSpace?.Commit(active, changeType);
        }

        public static void CommitMany(
            IProjectProvider projectProvider,
            IEnumerable<AssCS.Event> selection,
            ChangeType changeType
        )
        {
            projectProvider.Current.WorkingSpace?.Commit(selection.ToList(), changeType);
        }

        public static void SelectOne(IProjectProvider projectProvider, AssCS.Event active)
        {
            projectProvider.Current.WorkingSpace?.SelectionManager.Select(active);
        }

        public static void SelectMany(
            IProjectProvider projectProvider,
            AssCS.Event active,
            IEnumerable<AssCS.Event> selection
        )
        {
            projectProvider.Current.WorkingSpace?.SelectionManager.Select(
                active,
                selection.ToList()
            );
        }
    }
}
