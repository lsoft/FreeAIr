using Agent.Server.External;
using Agent.Server.Github;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Server
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

        public void AddExternalServers(
            McpServers servers
            )
        {
            foreach (var server in servers.Servers)
            {
                var extServer = new ExternalServer(
                    server.Key,
                    server.Value
                    );
                _servers[extServer.Name] = extServer;
            }
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
