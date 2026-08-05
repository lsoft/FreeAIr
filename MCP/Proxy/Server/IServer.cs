using Dto;

namespace Proxy.Server
{
    /// <summary>One connectable MCP server as the proxy sees it, regardless of kind (VS-hosted, GitHub, external) - what <see cref="Servers"/> looks up by name to serve an <see cref="IMcpProxyInterface"/> call.</summary>
    public interface IServer
    {
        Task PingAsync(
            IParameterProvider parameterProvider
            );

        Task<IsInstalledReply> IsInstalledAsync(
            IParameterProvider parameterProvider
            );

        Task<InstallReply> InstallAsync(
            IParameterProvider parameterProvider
            );

        Task<GetToolsReply> GetToolsAsync(
            IParameterProvider parameterProvider
            );

        Task<CallToolReply> CallToolAsync(
            IParameterProvider parameterProvider,
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments,
            CancellationToken cancellationToken
            );
    }
}
