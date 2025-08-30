using Dto;
using Proxy.Server;
using Serilog;

namespace Proxy
{
    public sealed class McpProxyInterface : IMcpProxyInterface
    {
        private static readonly ILogger _log = SerilogLogger.Logger.ForContext<McpProxyInterface>();

        private readonly Servers _servers;

        public McpProxyInterface(
            Servers servers
            )
        {
            ArgumentNullException.ThrowIfNull(servers);
            _servers = servers;
        }

        public async Task<UpdateExternalServersReply> UpdateExternalServersAsync(
            UpdateExternalServersRequest request
            )
        {
            try
            {
                if (request is null)
                {
                    _log.Error("Cannot parse request");
                    return BaseReply.FromError<UpdateExternalServersReply>("Cannot parse request");
                }

                _servers.RemoveAllExternalServers();
                var approvedMcpServers = await _servers.UpdateExternalServersAsync(request.McpServers);

                return new UpdateExternalServersReply(
                    approvedMcpServers
                    );
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during updating external servers");

                return BaseReply.FromError<UpdateExternalServersReply>("Error during updating external servers: " + excp.Message + Environment.NewLine + excp.StackTrace);
            }
        }

        public async Task<IsInstalledReply> IsInstalledAsync(
            IsInstalledRequest request
            )
        {

            Console.WriteLine(":: Begin");
            try
            {
                if (request is null)
                {
                    _log.Error("Cannot parse request");
                    return BaseReply.FromError<IsInstalledReply>("Cannot parse request");
                }

                var serverName = request.MCPServerName;
                var server = _servers.GetServer(serverName);

                var result = await server.IsInstalledAsync(
                    request
                    );

                return result;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during installation");

                return new IsInstalledReply(false);
            }
        }

        public async Task<InstallReply> InstallAsync(
            InstallRequest request
            )
        {
            try
            {
                if (request is null)
                {
                    _log.Error("Cannot parse request");
                    return BaseReply.FromError<InstallReply>("Cannot parse request");
                }

                var serverName = request.MCPServerName;
                var server = _servers.GetServer(serverName);

                var result = await server.InstallAsync(
                    request
                    );

                return result;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during installation");

                return BaseReply.FromError<InstallReply>("Error during installation: " + excp.Message + Environment.NewLine + excp.StackTrace);
            }
        }

        public async Task<GetToolsReply> GetToolsAsync(
            GetToolsRequest request
            )
        {
            try
            {
                if (request is null)
                {
                    _log.Error("Cannot parse request");
                    return BaseReply.FromError<GetToolsReply>("Cannot parse request");
                }

                var serverName = request.MCPServerName;
                var server = _servers.GetServer(serverName);

                var result = await server.GetToolsAsync(
                    request
                    );

                return result;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during getting tools");

                return BaseReply.FromError<GetToolsReply>("Error during getting tools: " + excp.Message + Environment.NewLine + excp.StackTrace);
            }
        }

        public async Task<CallToolReply> CallToolAsync(
            CallToolRequest request,
            CancellationToken cancellationToken
            )
        {
            try
            {
                if (request is null)
                {
                    _log.Error("Cannot parse request");
                    return BaseReply.FromError<CallToolReply>("Cannot parse request");
                }

                var serverName = request.MCPServerName;
                var server = _servers.GetServer(serverName);

                var result = await server.CallToolAsync(
                    request,
                    request.ToolName,
                    request.Arguments?.ToDictionary(d => d.Key, d => (object?)d.Value),
                    cancellationToken
                    );

                return result;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during calling tools");

                return BaseReply.FromError<CallToolReply>("Error during calling tools: " + excp.Message + Environment.NewLine + excp.StackTrace);
            }
        }
    }
}
