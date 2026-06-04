// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.Windows.Input;
using Holo.Models;

namespace Holo.Providers;

/// <summary>
/// Provides methods for registering and managing
/// commands and command contexts
/// </summary>
public class CommandRegistrar : ICommandRegistrar
{
    private readonly Dictionary<int, Dictionary<string, ICommand>> _contexts = [];
    private readonly Dictionary<int, HashSet<CommandMetadata>> _metadata = [];

    /// <inheritdoc />
    public bool CreateContext(int id)
    {
        return _contexts.TryAdd(id, new Dictionary<string, ICommand>()) && _metadata.TryAdd(id, []);
    }

    /// <inheritdoc />
    public bool DestroyContext(int id)
    {
        return _contexts.Remove(id) && _metadata.Remove(id);
    }

    /// <inheritdoc />
    public bool RegisterCommand(int contextId, CommandMetadata info)
    {
        return _contexts.TryGetValue(contextId, out var context)
            && _metadata.TryGetValue(contextId, out var metadata)
            && context.TryAdd(info.QualifiedName, info.Command)
            && metadata.Add(info);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ICommand> GetCommandsForContext(int contextId)
    {
        return _contexts.TryGetValue(contextId, out var context)
            ? context.AsReadOnly()
            : ReadOnlyDictionary<string, ICommand>.Empty;
    }

    /// <inheritdoc />
    public IReadOnlySet<CommandMetadata> GetMetadataForContext(int contextId)
    {
        return _metadata.TryGetValue(contextId, out var context)
            ? context.AsReadOnly()
            : ReadOnlySet<CommandMetadata>.Empty;
    }
}
