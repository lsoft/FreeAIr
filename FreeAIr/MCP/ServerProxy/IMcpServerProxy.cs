using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy
{
    /// <summary>
    /// The VSIX side's view of one MCP server, whatever is behind it - a VS-internal tool set, the
    /// GitHub server, or an external one reached through the out-of-process proxy. Everything above
    /// this (tool discovery, tool calling, UI) works only against this interface.
    /// </summary>
    public interface IMcpServerProxy
    {
        /// <summary>The server's identifier, e.g. `github.com` or the name chosen for a user-configured external server. Used to route tool calls to the right proxy and to key its tools in <see cref="AvailableToolContainer"/>.</summary>
        string Name
        {
            get;
        }

        /// <summary>Whether the server is ready to be started - the backing executable is present and, for the proxied servers, the out-of-process host is running.</summary>
        Task<bool> IsInstalledAsync();

        /// <summary>Performs whatever one-time setup the server needs before its tools can be listed - downloading an executable, or just confirming the host process is up.</summary>
        Task InstallAsync();

        /// <summary>Retrieves the list of tools this server currently publishes.</summary>
        Task<McpServerTools> GetToolsAsync();

        /// <summary>Invokes one of this server's tools by name, returning a failure result rather than throwing when the call could not be completed.</summary>
        Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            Dictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            );
    }
}
