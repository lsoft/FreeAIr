using Dto;
using Proxy.Server;
using Serilog;

namespace Proxy
{
    /// <summary>
    /// The proxy process's implementation of the RPC surface: looks up the target server in
    /// <see cref="Servers"/> by name and forwards to it, turning any exception into a
    /// <see cref="BaseReply.ErrorMessage"/> instead of letting it cross the pipe.
    /// </summary>
    public sealed class McpProxyInterface : IMcpProxyInterface
    {
        /// <summary>Serilog logger scoped to this class, used to record request failures before they are turned into an <see cref="BaseReply.ErrorMessage"/>.</summary>
        private static readonly ILogger _log = SerilogLogger.Logger.ForContext<McpProxyInterface>();

        /// <summary>Registry of MCP servers the proxy currently knows about, used to resolve a request's target server by name.</summary>
        private readonly Servers _servers;

        /// <summary>Creates the proxy's RPC surface backed by the given server registry.</summary>
        public McpProxyInterface(
            Servers servers
            )
        {
            ArgumentNullException.ThrowIfNull(servers);
            _servers = servers;
        }

        /// <summary>Replaces the whole set of externally configured servers with <paramref name="request"/>'s, dropping the old ones first.</summary>
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

        /// <summary>Looks up the named server and asks it whether its dependency is already installed.</summary>
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

        /// <summary>Looks up the named server and installs its dependency.</summary>
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

        /// <summary>Looks up the named server and returns its tool list.</summary>
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

        /// <summary>Looks up the named server and invokes one of its tools.</summary>
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
