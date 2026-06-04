// SPDX-License-Identifier: MPL-2.0

using AssCS;
using Holo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Holo.Providers;

/// <summary>
/// Factory for creating <see cref="Workspace"/>s within the DI container
/// </summary>
public class WorkspaceFactory(IServiceProvider provider, ILoggerFactory loggerFactory)
    : IWorkspaceFactory
{
    /// <inheritdoc />
    public Workspace Create(Document document, int id, Uri? savePath = null)
    {
        return new Workspace(
            document,
            id,
            savePath,
            loggerFactory.CreateLogger<Workspace>(),
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<MediaController>()
        );
    }
}
