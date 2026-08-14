// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Ameko.Utilities;
using Avalonia.Controls;
using Holo.Scripting;
using Material.Icons;
using Material.Icons.Avalonia;

namespace Ameko.Services;

/// <summary>
/// Service for managing the Scripts menu
/// </summary>
public static class ScriptMenuService
{
    /// <summary>
    /// Generate menu items
    /// </summary>
    /// <param name="scripts">List of scripts</param>
    /// <param name="overrides">Map of submenu overrides</param>
    /// <param name="executeScriptCommand">Command for executing scripts</param>
    /// <returns>List of top-level menu items</returns>
    public static List<MenuItem> GenerateMenuItemSource(
        IList<IHoloExecutable> scripts,
        IDictionary<string, string> overrides,
        ICommand executeScriptCommand
    )
    {
        var root = new MenuNode<MenuItem>("ROOT");

        foreach (var script in scripts)
        {
            var menu = new MenuItem
            {
                Header = script.Info.DisplayName,
                Command = executeScriptCommand,
                CommandParameter = script.Info.QualifiedName,
                Icon = new MaterialIcon { Kind = MaterialIconKind.CodeBlockBraces },
            };

            var headless = script.Info.Headless && script.Info.Exports.Length != 0;
            if (!headless) // Only add if not headless
            {
                if (!overrides.TryGetValue(script.Info.QualifiedName, out var submenu))
                    submenu = script.Info.Submenu;

                AddToTree(root, submenu, menu);
            }

            foreach (var methodInfo in script.Info.Exports)
            {
                var fullQualifiedName = $"{script.Info.QualifiedName}+{methodInfo.QualifiedName}";
                var methodMenu = new MenuItem
                {
                    Header = methodInfo.DisplayName,
                    Command = executeScriptCommand,
                    CommandParameter = fullQualifiedName,
                    Icon = new MaterialIcon { Kind = MaterialIconKind.CodeBlockParentheses },
                };

                if (!overrides.TryGetValue(fullQualifiedName, out var submenu))
                    submenu = methodInfo.Submenu;

                AddToTree(root, submenu, methodMenu);
            }
        }

        return BuildMenu(root);
    }

    /// <summary>
    /// Generate native menu items
    /// </summary>
    /// <param name="scripts">List of scripts</param>
    /// <param name="overrides">Map of submenu overrides</param>
    /// <param name="executeScriptCommand">Command for executing scripts</param>
    /// <returns>List of top-level menu items</returns>
    public static List<NativeMenuItem> GenerateNativeMenuItemSource(
        IList<IHoloExecutable> scripts,
        IDictionary<string, string> overrides,
        ICommand executeScriptCommand
    )
    {
        var root = new MenuNode<NativeMenuItem>("ROOT");

        foreach (var script in scripts)
        {
            var menu = new NativeMenuItem
            {
                Header = script.Info.DisplayName,
                Command = executeScriptCommand,
                CommandParameter = script.Info.QualifiedName,
            };

            var headless = script.Info.Headless && script.Info.Exports.Length != 0;
            if (!headless) // Only add if not headless
            {
                if (!overrides.TryGetValue(script.Info.QualifiedName, out var submenu))
                    submenu = script.Info.Submenu;

                AddToTree(root, submenu, menu);
            }

            foreach (var methodInfo in script.Info.Exports)
            {
                var fullQualifiedName = $"{script.Info.QualifiedName}+{methodInfo.QualifiedName}";
                var methodMenu = new NativeMenuItem
                {
                    Header = methodInfo.DisplayName,
                    Command = executeScriptCommand,
                    CommandParameter = fullQualifiedName,
                };

                if (!overrides.TryGetValue(fullQualifiedName, out var submenu))
                    submenu = methodInfo.Submenu;

                AddToTree(root, submenu, methodMenu);
            }
        }

        return BuildNativeMenu(root);
    }

    public static MenuItem GenerateReloadMenuItem(ICommand reloadCommand)
    {
        return new MenuItem
        {
            Header = I18N.Resources.Menu_ReloadScripts,
            Command = reloadCommand,
            CommandParameter = true,
            Icon = new MaterialIcon { Kind = MaterialIconKind.Reload },
        };
    }

    public static NativeMenuItem GenerateReloadNativeMenuItem(ICommand reloadCommand)
    {
        return new NativeMenuItem
        {
            Header = I18N.Resources.Menu_ReloadScripts,
            Command = reloadCommand,
            CommandParameter = true,
        };
    }

    public static MenuItem GeneratePkgManMenuItem(ICommand pkgManCommand)
    {
        return new MenuItem
        {
            Header = I18N.Resources.Menu_PackageManager,
            Command = pkgManCommand,
            Icon = new MaterialIcon { Kind = MaterialIconKind.PackageVariant },
        };
    }

    public static NativeMenuItem GeneratePkgManNativeMenuItem(ICommand pkgManCommand)
    {
        return new NativeMenuItem
        {
            Header = I18N.Resources.Menu_PackageManager,
            Command = pkgManCommand,
        };
    }

    public static MenuItem GeneratePlaygroundMenuItem(ICommand playgroundCommand)
    {
        return new MenuItem
        {
            Header = I18N.Resources.Menu_Playground,
            Command = playgroundCommand,
            Icon = new MaterialIcon { Kind = MaterialIconKind.PinwheelOutline },
        };
    }

    public static NativeMenuItem GeneratePlaygroundNativeMenuItem(ICommand playgroundCommand)
    {
        return new NativeMenuItem
        {
            Header = I18N.Resources.Menu_Playground,
            Command = playgroundCommand,
        };
    }

    #region Recursive menu stuff

    private static void AddToTree<T>(MenuNode<T> root, string? submenu, T item)
    {
        if (string.IsNullOrWhiteSpace(submenu))
        {
            root.Items.Add(item);
            return;
        }

        var hierarchy = submenu.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        var dest = root;
        foreach (var level in hierarchy)
        {
            if (!dest.Children.TryGetValue(level, out var child))
            {
                child = new MenuNode<T>(level);
                dest.Children[level] = child;
            }
            dest = child;
        }
        dest.Items.Add(item);
    }

    private static List<MenuItem> BuildMenu(MenuNode<MenuItem> root)
    {
        var congregation = root
            .Children.Values.OrderByDescending(c => c.Children.Count > 0)
            .ThenBy(c => c.Header)
            .Select(child => new MenuItem { Header = child.Header, ItemsSource = BuildMenu(child) })
            .ToList();

        congregation.AddRange(root.Items);

        return congregation;
    }

    private static List<NativeMenuItem> BuildNativeMenu(MenuNode<NativeMenuItem> root)
    {
        var congregation = new List<NativeMenuItem>();

        foreach (
            var child in root
                .Children.Values.OrderByDescending(c => c.Children.Count > 0)
                .ThenBy(c => c.Header)
        )
        {
            var menu = new NativeMenu();
            menu.Items.AddRange(BuildNativeMenu(child));

            congregation.Add(new NativeMenuItem { Header = child.Header, Menu = menu });
        }

        congregation.AddRange(root.Items);

        return congregation;
    }

    private class MenuNode<T>(string header)
    {
        public string Header { get; } = header;
        public List<T> Items { get; } = [];
        public Dictionary<string, MenuNode<T>> Children { get; } = [];
    }

    #endregion
}
