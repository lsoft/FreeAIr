using Dto;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Serilog;

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
        protected override Task<IsInstalledReply> IsInstalledInternalAsync(
            IParameterProvider parameterProvider
            )
        {
            return Task.FromResult(
                BaseReply.FromError<IsInstalledReply>("Not applicable")
                );
        }

        protected override Task<InstallReply> InstallInternalAsync(
            IParameterProvider parameterProvider
            )
        {
            return Task.FromResult(
                BaseReply.FromError<InstallReply>("Not applicable")
                );
        }

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


        protected override async Task<CallToolReply> CallToolInternalAsync(
            IMcpClient mcpClient,
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments,
            CancellationToken cancellationToken
            )
        {
            var innerResult = await mcpClient.CallToolAsync(
                toolName,
                arguments?.ToDictionary(d => d.Key, d => d.Value),
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

        public async Task<CallToolReply> CallToolAsync(
            IParameterProvider parameterProvider,
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments,
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

        public async Task<InstallReply> InstallAsync(
            IParameterProvider parameterProvider
            )
        {
            return await InstallInternalAsync(
                parameterProvider
                );
        }

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

        protected abstract Task<IsInstalledReply> IsInstalledInternalAsync(
            IParameterProvider parameterProvider
            );

        protected abstract Task<InstallReply> InstallInternalAsync(
            IParameterProvider parameterProvider
            );

        protected abstract Task<GetToolsReply> GetToolsInternalAsync(
            IMcpClient mcpClient
            );

        protected abstract Task<CallToolReply> CallToolInternalAsync(
            IMcpClient mcpClient,
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments,
            CancellationToken cancellationToken
            );

    }
}
