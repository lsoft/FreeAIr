using Dto;
using System.Text.Json;

namespace Proxy.Server
{
    /// <summary>One connectable MCP server as the proxy sees it, regardless of kind (VS-hosted, GitHub, external) - what <see cref="Servers"/> looks up by name to serve an <see cref="IMcpProxyInterface"/> call.</summary>
    public interface IServer
    {
        /// <summary>Confirms the server can be reached by round-tripping a ping through it.</summary>
        Task PingAsync(
            IParameterProvider parameterProvider
            );

        /// <summary>Checks whether this server's dependency is already installed.</summary>
        Task<IsInstalledReply> IsInstalledAsync(
            IParameterProvider parameterProvider
            );

        /// <summary>Installs this server's dependency.</summary>
        Task<InstallReply> InstallAsync(
            IParameterProvider parameterProvider
            );

        /// <summary>Lists the tools this server exposes.</summary>
        Task<GetToolsReply> GetToolsAsync(
            IParameterProvider parameterProvider
            );

        /// <summary>Invokes one of this server's tools, the arguments still being the JSON values they arrived as.</summary>
        Task<CallToolReply> CallToolAsync(
            IParameterProvider parameterProvider,
            string toolName,
            IReadOnlyDictionary<string, JsonElement>? arguments,
            CancellationToken cancellationToken
            );
    }
}
