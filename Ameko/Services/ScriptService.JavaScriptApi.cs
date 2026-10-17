// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using AssCS.History;
using Holo.Providers;
using Holo.Scripting;
using Jint;

namespace Ameko.Services;

public partial class ScriptService
{
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
            //lang=js
            """
            Object.defineProperty(globalThis, 'project', { get: function() { return ProjectProvider.Current; } });
            Object.defineProperty(globalThis, 'workspace', { get: function() { return ProjectProvider.Current.WorkingSpace; } });
            Object.defineProperty(globalThis, 'eventManager', { get: function() { return ProjectProvider.Current.WorkingSpace?.Document.EventManager; } });
            Object.defineProperty(globalThis, 'activeEvent', { get: function() { return ProjectProvider.Current.WorkingSpace?.SelectionManager.ActiveEvent; } });
            Object.defineProperty(globalThis, 'selectedEvents', {
                get: function() {
                    const col = ProjectProvider.Current.WorkingSpace?.SelectionManager.SelectedEventCollection;
                    return col ? Array.from(col) : [];
                }
            });
            Object.defineProperty(globalThis, 'events', {
                get: function() {
                    const col = ProjectProvider.Current.WorkingSpace?.Document.EventManager.Events;
                    return col ? Array.from(col) : [];
                }
            });
            """
        );
        return engine;
    }
}
