using Dto;
using ModelContextProtocol.Client;
using Serilog;

namespace Proxy.Server
{
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
            IReadOnlyDictionary<string, object?>? arguments
            )
        {
            var innerResult = await mcpClient.CallToolAsync(
                toolName,
                arguments?.ToDictionary(d => d.Key, d => d.Value)
                );

            var texts = innerResult.Content
                .Where(t => t.Type == "text")
                .Select(t => t.Text)
                .ToArray()
                ;

            if (innerResult.IsError)
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


    public abstract class BaseServer<T> : IServer
        where T : IServer
    {
        protected readonly ILogger _log = SerilogLogger.Logger.ForContext<T>();

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
            IReadOnlyDictionary<string, object?>? arguments
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
                arguments
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
            IReadOnlyDictionary<string, object?>? arguments
            );

    }
}
