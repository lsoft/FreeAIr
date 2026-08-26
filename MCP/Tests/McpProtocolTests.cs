using Dto;
using FreeAIr.Helper;
using ModelContextProtocol.Client;
using Proxy;
using Proxy.Server;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FreeAIr.Mcp.Tests
{
    /// <summary>
    /// Drives the proxy's own <see cref="BaseServer2{T}"/> against a real MCP server and checks that
    /// what the model asked for is what the server is told, i.e. that FreeAIr speaks `tools/list`
    /// and `tools/call` the way the MCP specification defines them. Only the transport is
    /// substituted: instead of spawning `npx some-server`, the connection is an in-process duplex
    /// pair, and everything above it - the SDK, the request shapes, the reply mapping - is the
    /// shipping code.
    /// </summary>
    public class McpProtocolTests : IAsyncLifetime
    {
        private readonly McpServerScript _script = new();
        private readonly List<InProcessMcpServer> _connections = [];

        /// <summary>The tool arguments from issue #70 - the shape that used to arrive as nested empty arrays.</summary>
        private const string CreateEntitiesArguments =
            """
            {"entities":[{"name":"Alice","entityType":"person","observations":["likes tea","drinks it black"]}]}
            """;

        public Task InitializeAsync()
        {
            //BaseServer logs through the proxy's shared logger, which is null until it is built
            SerilogLogger.Init(
                Path.Combine(Path.GetTempPath(), "FreeAIr.Mcp.Tests"),
                "Log",
                "tests.log"
                );

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            foreach (var connection in _connections)
            {
                await connection.DisposeAsync();
            }
        }

        [Fact]
        public async Task ToolCall_ArgumentsFromTheModel_ReachTheServerUnchanged()
        {
            await CallToolAsync("create_entities", CreateEntitiesArguments);

            AssertServerReceived("create_entities", CreateEntitiesArguments);
        }

        [Fact]
        public async Task ToolCall_ScalarArguments_KeepTheirJsonTypes()
        {
            const string Arguments =
                """
                {"query":"tea","limit":25,"threshold":0.75,"fuzzy":true,"exclude":null}
                """;

            await CallToolAsync("search_nodes", Arguments);

            AssertServerReceived("search_nodes", Arguments);
        }

        [Fact]
        public async Task ToolCall_WithoutArguments_OmitsThemFromTheRequest()
        {
            await CallToolAsync("read_graph", argumentsJson: null);

            var call = _script.SingleCall;
            Assert.Equal("read_graph", call.ToolName);
            //the specification makes `arguments` optional; a tool taking none must not be sent an
            //empty object it would then have to ignore
            Assert.Null(call.Arguments);
        }

        [Fact]
        public async Task ToolCall_TextContent_IsCollectedInOrder()
        {
            _script.ReplyContent = ["first", "second"];

            var reply = await CallToolAsync("read_graph", argumentsJson: null);

            Assert.Null(reply.ErrorMessage);
            Assert.False(reply.IsError);
            Assert.Equal(new[] { "first", "second" }, reply.Content);
        }

        [Fact]
        public async Task ToolCall_ServerReportedFailure_BecomesAnErrorReply()
        {
            _script.ReplyIsError = true;
            _script.ReplyContent = ["entity already exists"];

            var reply = await CallToolAsync("create_entities", CreateEntitiesArguments);

            //a tool which failed reports it through `isError` on a successful call, not through a
            //protocol error, and the proxy has to turn that into a failed reply of its own
            Assert.NotNull(reply.ErrorMessage);
            Assert.Contains("entity already exists", reply.ErrorMessage);
        }

        [Fact]
        public async Task ToolList_CarriesNameDescriptionAndSchema()
        {
            const string Schema =
                """
                {"type":"object","properties":{"entities":{"type":"array","items":{"type":"object"}}},"required":["entities"]}
                """;

            _script.InputSchemaJson = Schema;

            var server = new TestServer(_script, _connections);
            var reply = await server.GetToolsAsync(FakeParameterProvider.Instance);

            var tool = Assert.Single(reply.Tools);
            Assert.Equal(InProcessMcpServer.ToolName, tool.Name);
            Assert.Equal("Records the arguments it is called with.", tool.Description);
            Assert.True(
                JsonNode.DeepEquals(JsonNode.Parse(Schema), JsonNode.Parse(tool.Parameters)),
                $"schema arrived as {tool.Parameters}"
                );
        }

        /// <summary>
        /// Runs one tool call the whole way the VSIX does: the model's raw JSON is parsed the way
        /// <c>ChatToolHelper</c> parses it, packed into a <see cref="CallToolRequest"/>, unpacked the
        /// way <c>McpProxyInterface</c> unpacks it, and replayed through the proxy's server.
        /// </summary>
        private async Task<CallToolReply> CallToolAsync(
            string toolName,
            string? argumentsJson
            )
        {
            Dictionary<string, object?>? arguments = null;
            if (argumentsJson is not null)
            {
                arguments = (Dictionary<string, object?>)JsonElementDeserializer.DeserializeToObject(
                    JsonDocument.Parse(argumentsJson).RootElement
                    )!;
            }

            var request = new CallToolRequest("memory", toolName, arguments);

            var server = new TestServer(_script, _connections);

            return await server.CallToolAsync(
                request,
                request.ToolName,
                ToolArguments.Deserialize(request.ArgumentsJson),
                CancellationToken.None
                );
        }

        /// <summary>Asserts the single recorded call named <paramref name="toolName"/> carries exactly <paramref name="expectedArgumentsJson"/>.</summary>
        private void AssertServerReceived(
            string toolName,
            string expectedArgumentsJson
            )
        {
            var call = _script.SingleCall;

            Assert.Equal(toolName, call.ToolName);
            Assert.True(
                JsonNode.DeepEquals(JsonNode.Parse(expectedArgumentsJson), call.Arguments),
                $"the server was called with {call.Arguments?.ToJsonString() ?? "no arguments"}"
                );
        }

        /// <summary>
        /// The proxy's real server implementation with its transport replaced: every call gets a
        /// fresh connection to a fresh in-process MCP server, exactly as
        /// <see cref="BaseServer{T}"/> expects, since it disposes the client when the call is done.
        /// </summary>
        private sealed class TestServer : BaseServer2<TestServer>
        {
            private readonly McpServerScript _script;
            private readonly List<InProcessMcpServer> _connections;

            public TestServer(
                McpServerScript script,
                List<InProcessMcpServer> connections
                )
            {
                _script = script;
                _connections = connections;
            }

            protected override async Task<IMcpClient?> CreateMcpClientAsync(
                IParameterProvider parameterProvider
                )
            {
                var connection = await InProcessMcpServer.StartAsync(_script);
                _connections.Add(connection);

                return connection.Client;
            }
        }
    }
}
