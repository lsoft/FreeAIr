using Proxy.Server.External;
using Proxy.Server.Github;
using Dto;

namespace Proxy.Server
{
    /// <summary>
    /// The proxy process's registry of connectable MCP servers by name: the built-in GitHub server
    /// plus whatever external ones the VS side has pushed through
    /// <see cref="UpdateExternalServersAsync"/>.
    /// </summary>
    public sealed class Servers
    {
        private Dictionary<string, IServer> _servers = new()
        {
            [GithubServer.PublicMCPServerName] = new GithubServer(),
        };

        public Servers()
        {
        }

        /// <summary>Looks up a registered server by name; throws if it is not (yet) registered.</summary>
        public IServer GetServer(
            string serverName
            )
        {
            if (_servers.TryGetValue(serverName, out IServer? server))
            {
                return server;
            }

            throw new InvalidOperationException($"Server with name {serverName} does not found.");
        }

        /// <summary>Registers each of <paramref name="servers"/> as an <see cref="ExternalServer"/> after confirming it actually answers a ping - a server that fails to connect never enters the registry.</summary>
        public async Task<McpServers> UpdateExternalServersAsync(
            McpServers servers
            )
        {
            var result = new McpServers();

            foreach (var server in servers.Servers)
            {
                var extServer = new ExternalServer(
                    server.Key,
                    server.Value
                    );
                await extServer.PingAsync(
                    FakeParameterProvider.Instance
                    );

                _servers[extServer.Name] = extServer;
                result.Servers[server.Key] = server.Value;
            }

            return result;
        }

        /// <summary>Drops every registered <see cref="ExternalServer"/>, called before re-registering the current set so a removed or renamed server does not linger.</summary>
        public void RemoveAllExternalServers()
        {
            foreach (var pair in _servers.ToList())
            {
                if (pair.Value is ExternalServer)
                {
                    _servers.Remove(pair.Key);
                }
            }
        }
    }
}
