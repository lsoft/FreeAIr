using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Nerdbank.Streams;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FreeAIr.Mcp.Tests
{
    /// <summary>
    /// One `tools/call` as the MCP server on the far end saw it, kept as detached JSON so an
    /// assertion can outlive the connection it arrived on.
    /// </summary>
    internal sealed record RecordedToolCall(
        string ToolName,
        JsonObject? Arguments
        );

    /// <summary>
    /// What every server this test run starts writes its incoming calls into, and what the next
    /// call is answered with. Shared across connections because the proxy opens a fresh one per
    /// tool call.
    /// </summary>
    internal sealed class McpServerScript
    {
        /// <summary>Every `tools/call` the server received, oldest first.</summary>
        public List<RecordedToolCall> Calls
        {
            get;
        } = [];

        /// <summary>The text content blocks the tool answers with.</summary>
        public string[] ReplyContent
        {
            get;
            set;
        } = ["ok"];

        /// <summary>Whether the tool reports itself as failed, i.e. sets `isError` on the result.</summary>
        public bool ReplyIsError
        {
            get;
            set;
        }

        /// <summary>The input schema the server publishes for its single tool, as raw JSON.</summary>
        public string InputSchemaJson
        {
            get;
            set;
        } = """{"type":"object","properties":{"entities":{"type":"array"}},"required":["entities"]}""";

        /// <summary>The one and only call, when a test expects exactly one.</summary>
        public RecordedToolCall SingleCall => Assert.Single(Calls);
    }

    /// <summary>
    /// A real MCP server and a real <see cref="IMcpClient"/> talking to each other over an
    /// in-process duplex pair, so a test exercises the actual protocol - initialize, `tools/list`,
    /// `tools/call` - without spawning a process. The server is scripted through
    /// <see cref="McpServerScript"/> and records what it was asked to do.
    /// </summary>
    internal sealed class InProcessMcpServer : IAsyncDisposable
    {
        /// <summary>The name of the single tool every scripted server publishes.</summary>
        public const string ToolName = "echo";

        private readonly Stream _clientSide;
        private readonly Stream _serverSide;
        private readonly IMcpServer _server;
        private readonly Task _serverTask;

        /// <summary>The connected client, ready to be handed to production code as if it had dialled a real server.</summary>
        public IMcpClient Client
        {
            get;
        }

        private InProcessMcpServer(
            Stream clientSide,
            Stream serverSide,
            IMcpServer server,
            Task serverTask,
            IMcpClient client
            )
        {
            _clientSide = clientSide;
            _serverSide = serverSide;
            _server = server;
            _serverTask = serverTask;
            Client = client;
        }

        /// <summary>Starts a server driven by <paramref name="script"/> and connects a client to it.</summary>
        public static async Task<InProcessMcpServer> StartAsync(
            McpServerScript script
            )
        {
            var (clientSide, serverSide) = FullDuplexStream.CreatePair();

            var options = new McpServerOptions
            {
                ServerInfo = new Implementation
                {
                    Name = "freeair-test-server",
                    Version = "1.0.0"
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability
                    {
                        ListToolsHandler = (_, _) => ValueTask.FromResult(
                            new ListToolsResult
                            {
                                Tools =
                                [
                                    new Tool
                                    {
                                        Name = ToolName,
                                        Description = "Records the arguments it is called with.",
                                        InputSchema = JsonSerializer.Deserialize<JsonElement>(script.InputSchemaJson)
                                    }
                                ]
                            }),
                        CallToolHandler = (context, _) =>
                        {
                            script.Calls.Add(
                                new RecordedToolCall(
                                    context.Params?.Name ?? string.Empty,
                                    Detach(context.Params?.Arguments)
                                    )
                                );

                            return ValueTask.FromResult(
                                new CallToolResult
                                {
                                    IsError = script.ReplyIsError,
                                    Content = [.. script.ReplyContent.Select(t => new TextContentBlock { Text = t })]
                                });
                        }
                    }
                }
            };

            var server = McpServerFactory.Create(
                new StreamServerTransport(serverSide, serverSide, "freeair-test-server"),
                options
                );
            var serverTask = server.RunAsync();

            var client = await McpClientFactory.CreateAsync(
                new StreamClientTransport(clientSide, clientSide)
                );

            return new InProcessMcpServer(clientSide, serverSide, server, serverTask, client);
        }

        /// <summary>Copies the received arguments out of the transport's buffers into a standalone JSON object.</summary>
        private static JsonObject? Detach(
            IReadOnlyDictionary<string, JsonElement>? arguments
            )
        {
            if (arguments is null)
            {
                return null;
            }

            var result = new JsonObject();
            foreach (var pair in arguments)
            {
                result[pair.Key] = JsonNode.Parse(pair.Value.GetRawText());
            }

            return result;
        }

        /// <summary>Tears the connection down; the client may already be disposed by the code under test.</summary>
        public async ValueTask DisposeAsync()
        {
            try
            {
                await Client.DisposeAsync();
            }
            catch
            {
                //the production code under test owns the client and disposes it itself
            }

            await _server.DisposeAsync();
            await _clientSide.DisposeAsync();
            await _serverSide.DisposeAsync();

            try
            {
                await _serverTask;
            }
            catch
            {
                //the run loop ends by the transport being closed, which it reports as a failure
            }
        }
    }
}
