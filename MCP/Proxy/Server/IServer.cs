using Dto;

namespace Proxy.Server
{
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
            IReadOnlyDictionary<string, object?>? arguments
            );
    }
}
