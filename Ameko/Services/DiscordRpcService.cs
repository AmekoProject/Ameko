// SPDX-License-Identifier: GPL-3.0-only

using System.IO;
using Ameko.Messages;
using DiscordRPC;
using DiscordRPC.Logging;
using Holo.Configuration;
using Holo.Providers;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Primitives;
using LogLevel = DiscordRPC.Logging.LogLevel;

namespace Ameko.Services;

/// <summary>
/// Manages Discord RPC
/// </summary>
public class DiscordRpcService
{
    private readonly IProjectProvider _projectProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordRpcService> _logger;
    private readonly DiscordRpcClient _client;
    private readonly Timestamps _timestamps;

    private void Update(WorkingSpaceChangedMessage? _)
    {
        var prjName = _projectProvider.Current.Title;
        var wspName = _projectProvider.Current.WorkingSpace?.SavePath is not null
            ? Path.GetFileNameWithoutExtension(
                _projectProvider.Current.WorkingSpace.SavePath.LocalPath
            )
            : _projectProvider.Current.WorkingSpace?.Title;

        switch (_configuration.RichPresenceLevel)
        {
            case RichPresenceLevel.Disabled:
                _client.ClearPresence();
                break;
            case RichPresenceLevel.Enabled:
                _logger.LogDebug("Setting rich presence to {PrjName}/{WspName}", prjName, wspName);
                _client.SetPresence(
                    new RichPresence
                    {
                        Details = wspName is not null ? $"Editing {wspName}.ass" : string.Empty,
                        State = $"in {prjName}.aproj",
                        Timestamps = _timestamps,
                    }
                );
                break;
            case RichPresenceLevel.TimeOnly:
                _logger.LogDebug("Refreshing rich presence");
                _client.SetPresence(
                    new RichPresence { Details = "Authoring Subtitles", Timestamps = _timestamps }
                );
                break;
        }
    }

    public DiscordRpcService(
        IProjectProvider projectProvider,
        IConfiguration config,
        ILogger<DiscordRpcService> logger
    )
    {
        _projectProvider = projectProvider;
        _configuration = config;
        _logger = logger;

        _client = new DiscordRpcClient("1209896771719921704");
        _client.Logger = new ConsoleLogger { Level = LogLevel.None };
        _client.Initialize();
        _timestamps = Timestamps.Now;

        _logger.LogInformation("Discord RPC Initialized");
        MessageBus.Current.Listen<WorkingSpaceChangedMessage>().Subscribe(Update);
        Update(null);
    }

    ~DiscordRpcService()
    {
        _client.ClearPresence();
        _client.Dispose();
    }
}
