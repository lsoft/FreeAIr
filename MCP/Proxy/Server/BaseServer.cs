using Dto;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Serilog;
using System.Text.Json;

namespace Proxy.Server
{
    /// <summary>
    /// A <see cref="BaseServer{T}"/> for MCP servers reached through a real MCP client - tool
    /// listing and calling are delegated to that client, and installation is meaningless (there is
    /// nothing to check or install), so both report "Not applicable".
    /// </summary>
    public abstract class BaseServer2<T> : BaseServer<T>
        where T : IServer
    {
        /// <summary>Always reports "Not applicable" - a real MCP client has no separate install-check step.</summary>
        protected override Task<IsInstalledReply> IsInstalledInternalAsync(
            IParameterProvider parameterProvider
            )
        {
            return Task.FromResult(
                BaseReply.FromError<IsInstalledReply>("Not applicable")
                );
        }

        /// <summary>Always reports "Not applicable" - a real MCP client has nothing to install.</summary>
        protected override Task<InstallReply> InstallInternalAsync(
            IParameterProvider parameterProvider
            )
        {
            return Task.FromResult(
                BaseReply.FromError<InstallReply>("Not applicable")
                );
        }

        /// <summary>Asks the connected MCP client for its tool list and maps it to <see cref="GetToolsReply"/>.</summary>
        protected override async Task<GetToolsReply> GetToolsInternalAsync(
            IMcpClient mcpClient
            )
        {
            var mcpTools = await mcpClient.ListToolsAsync();

            var result = new GetToolsReply(
                mcpTools.Select(t => new GetToolReply(t.Name, t.Description, t.JsonSchema.GetRawText())).ToArray()
                );

            return result;
        }


        /// <summary>Invokes the tool through the connected MCP client and collects its text content into a <see cref="CallToolReply"/>.</summary>
        protected override async Task<CallToolReply> CallToolInternalAsync(
            IMcpClient mcpClient,
            string toolName,
            IReadOnlyDictionary<string, JsonElement>? arguments,
            CancellationToken cancellationToken
            )
        {
            //the SDK takes the arguments weakly typed and serializes each of them itself; a
            //JsonElement is the one shape it can copy into the request without reinterpreting it
            var innerResult = await mcpClient.CallToolAsync(
                toolName,
                arguments?.ToDictionary(d => d.Key, d => (object?)d.Value),
                cancellationToken: cancellationToken
                );

            var texts = innerResult.Content
                .Where(t => t.Type == "text")
                .Cast<TextContentBlock>()
                .Select(t => t.Text)
                .ToArray()
                ;

            if (innerResult.IsError.GetValueOrDefault())
            {
                var msg = "Error call tool: " + string.Join(string.Empty, texts);
                _log.Error(msg);
                return BaseReply.FromError<CallToolReply>(msg);
            }

            var result = new CallToolReply(
                false,
                texts
                );

            return result;
        }
    }


    /// <summary>
    /// Shared plumbing for every server kind the proxy hosts (VS, GitHub, external): opens an
    /// <see cref="IMcpClient"/> through <see cref="CreateMcpClientAsync"/> for each call, turns a
    /// failure to connect into an error reply instead of an exception, and leaves the kind-specific
    /// work to the `*InternalAsync` methods a subclass overrides.
    /// </summary>
    public abstract class BaseServer<T> : IServer
        where T : IServer
    {
        protected readonly ILogger _log = SerilogLogger.Logger.ForContext<T>();

        /// <summary>Round-trips a ping through the server's MCP client, to confirm the connection is alive.</summary>
        public async Task PingAsync(
            IParameterProvider parameterProvider
            )
        {
            ArgumentNullException.ThrowIfNull(parameterProvider);

            await using var mcpClient = await CreateMcpClientAsync(
                parameterProvider
                );
            if (mcpClient is null)
            {
                _log.Error("Cannot create McpClient");
                throw new InvalidOperationException("Cannot create McpClient");
            }

            await mcpClient.PingAsync();
        }

        /// <summary>Connects to the server and invokes one of its tools, or returns an error reply if the connection fails.</summary>
        public async Task<CallToolReply> CallToolAsync(
            IParameterProvider parameterProvider,
            string toolName,
            IReadOnlyDictionary<string, JsonElement>? arguments,
            CancellationToken cancellationToken
            )
        {
            await using var mcpClient = await CreateMcpClientAsync(
                parameterProvider
                );
            if (mcpClient is null)
            {
                _log.Error("Cannot create McpClient");
                return BaseReply.FromError<CallToolReply>("Cannot create McpClient");
            }

            return await CallToolInternalAsync(
                mcpClient,
                toolName,
                arguments,
                cancellationToken
                );
        }

        /// <summary>Connects to the server and returns its tool list, or an error reply if the connection fails.</summary>
        public async Task<GetToolsReply> GetToolsAsync(
            IParameterProvider parameterProvider
            )
        {
            await using var mcpClient = await CreateMcpClientAsync(
                parameterProvider
                );
            if (mcpClient is null)
            {
                _log.Error("Cannot create McpClient");
                return BaseReply.FromError<GetToolsReply>("Cannot create McpClient");
            }

            return await GetToolsInternalAsync(
                mcpClient
                );
        }

        /// <summary>Delegates to the server kind's install logic, e.g. running `npm install` for a GitHub-hosted server.</summary>
        public async Task<InstallReply> InstallAsync(
            IParameterProvider parameterProvider
            )
        {
            return await InstallInternalAsync(
                parameterProvider
                );
        }

        /// <summary>Delegates to the server kind's install-check logic.</summary>
        public async Task<IsInstalledReply> IsInstalledAsync(
            IParameterProvider parameterProvider
            )
        {
            return await IsInstalledInternalAsync(
                parameterProvider
                );
        }




        /// <summary>Connects to this server kind (spawns a process, opens an HTTP session, ...), returning null when the connection cannot be established.</summary>
        protected abstract Task<IMcpClient?> CreateMcpClientAsync(
            IParameterProvider parameterProvider
            );

        /// <summary>Checks whether this server kind's dependency is already installed.</summary>
        protected abstract Task<IsInstalledReply> IsInstalledInternalAsync(
            IParameterProvider parameterProvider
            );

        /// <summary>Installs this server kind's dependency.</summary>
        protected abstract Task<InstallReply> InstallInternalAsync(
            IParameterProvider parameterProvider
            );

        /// <summary>Reads the tool list from an already-connected client.</summary>
        protected abstract Task<GetToolsReply> GetToolsInternalAsync(
            IMcpClient mcpClient
            );

        /// <summary>Invokes a tool on an already-connected client.</summary>
        protected abstract Task<CallToolReply> CallToolInternalAsync(
            IMcpClient mcpClient,
            string toolName,
            IReadOnlyDictionary<string, JsonElement>? arguments,
            CancellationToken cancellationToken
            );

    }
}
