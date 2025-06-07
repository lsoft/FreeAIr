using Proxy.Server.External;
using Proxy.Server.Github;
using Dto;

namespace Proxy.Server
{
    public sealed class Servers
    {
        private Dictionary<string, IServer> _servers = new()
        {
            [GithubServer.PublicMCPServerName] = new GithubServer(),
        };

        public Servers()
        {
        }

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
