namespace Dto
{
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
