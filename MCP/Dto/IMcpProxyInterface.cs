namespace Dto
{
    /// <summary>
    /// The whole request/reply surface of the MCP proxy, shared verbatim between the VS-side client
    /// and the out-of-process server that implements it - see <see cref="McpProxyInterface"/> and
    /// <see cref="BaseServer"/> in the `Proxy` assembly.
    /// </summary>
    public interface IMcpProxyInterface
    {
        /// <summary>Pushes the current externally configured MCP servers to the proxy so it stays in sync with the VS side.</summary>
        Task<UpdateExternalServersReply> UpdateExternalServersAsync(
            UpdateExternalServersRequest request
            );

        /// <summary>Checks whether a server's dependency is already installed.</summary>
        Task<IsInstalledReply> IsInstalledAsync(
            IsInstalledRequest request
            );

        /// <summary>Installs a server's missing dependency.</summary>
        Task<InstallReply> InstallAsync(
            InstallRequest request
            );

        /// <summary>Lists the tools one MCP server exposes.</summary>
        Task<GetToolsReply> GetToolsAsync(
            GetToolsRequest request
            );

        /// <summary>Invokes one tool of one MCP server with the given arguments.</summary>
        Task<CallToolReply> CallToolAsync(
            CallToolRequest request,
            CancellationToken cancellationToken
            );
    }
}
