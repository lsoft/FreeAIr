namespace Dto
{
    /// <summary>
    /// The whole request/reply surface of the MCP proxy, shared verbatim between the VS-side client
    /// and the out-of-process server that implements it - see <see cref="McpProxyInterface"/> and
    /// <see cref="BaseServer"/> in the `Proxy` assembly.
    /// </summary>
    public interface IMcpProxyInterface
    {
        Task<UpdateExternalServersReply> UpdateExternalServersAsync(
            UpdateExternalServersRequest request
            );

        Task<IsInstalledReply> IsInstalledAsync(
            IsInstalledRequest request
            );

        Task<InstallReply> InstallAsync(
            InstallRequest request
            );

        Task<GetToolsReply> GetToolsAsync(
            GetToolsRequest request
            );

        Task<CallToolReply> CallToolAsync(
            CallToolRequest request,
            CancellationToken cancellationToken
            );
    }
}
