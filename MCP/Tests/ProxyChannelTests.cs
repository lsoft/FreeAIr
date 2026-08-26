using Dto;
using FreeAIr.Helper;
using Nerdbank.Streams;
using StreamJsonRpc;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FreeAIr.Mcp.Tests
{
    /// <summary>
    /// Covers the channel between the VSIX and the proxy process: a real <see cref="JsonRpc"/> pair
    /// wired the way `McpServerProxyApplication` and the proxy's `Program` wire theirs, only over an
    /// in-process duplex stream instead of a spawned process's stdio. What matters here is that a
    /// <see cref="CallToolRequest"/> arrives with its arguments intact, whatever they contain.
    /// </summary>
    public class ProxyChannelTests
    {
        private const string CreateEntitiesArguments =
            """
            {"entities":[{"name":"Alice","entityType":"person","observations":["likes tea"]}],"dryRun":false}
            """;

        [Fact]
        public async Task CallTool_NestedArguments_ArriveUnchanged()
        {
            var recorder = new RecordingProxy();
            using var channel = ProxyChannel.Open(recorder);

            var arguments = (Dictionary<string, object?>)JsonElementDeserializer.DeserializeToObject(
                JsonDocument.Parse(CreateEntitiesArguments).RootElement
                )!;

            await channel.Client.CallToolAsync(
                new CallToolRequest("memory", "create_entities", arguments),
                CancellationToken.None
                );

            var received = recorder.LastCallToolRequest!;
            Assert.Equal("memory", received.MCPServerName);
            Assert.Equal("create_entities", received.ToolName);
            Assert.True(
                JsonNode.DeepEquals(
                    JsonNode.Parse(CreateEntitiesArguments),
                    JsonNode.Parse(received.ArgumentsJson!)
                    ),
                $"arguments arrived as {received.ArgumentsJson}"
                );
        }

        [Fact]
        public async Task CallTool_WithoutArguments_ArrivesWithoutThem()
        {
            var recorder = new RecordingProxy();
            using var channel = ProxyChannel.Open(recorder);

            await channel.Client.CallToolAsync(
                new CallToolRequest("memory", "read_graph", null),
                CancellationToken.None
                );

            Assert.Null(recorder.LastCallToolRequest!.ArgumentsJson);
        }

        [Fact]
        public async Task GetTools_Schema_ArrivesAsTheRawJsonItWasSentAs()
        {
            const string Schema =
                """
                {"type":"object","properties":{"entities":{"type":"array","items":{"type":"object"}}},"required":["entities"]}
                """;

            var recorder = new RecordingProxy
            {
                ToolSchemaJson = Schema
            };
            using var channel = ProxyChannel.Open(recorder);

            var reply = await channel.Client.GetToolsAsync(new GetToolsRequest("memory"));

            Assert.True(
                JsonNode.DeepEquals(
                    JsonNode.Parse(Schema),
                    JsonNode.Parse(Assert.Single(reply.Tools).Parameters)
                    )
                );
        }

        /// <summary>
        /// Why <see cref="CallToolRequest.ArgumentsJson"/> is text and not a dictionary of objects.
        /// The channel's default formatter is a Newtonsoft one, and a weakly typed member survives
        /// it only as a JToken; handing that to System.Text.Json - which is what the MCP SDK builds
        /// `tools/call` with - writes out the token's children instead of its value, which is the
        /// whole of issue #70. Delete this test only together with the design it explains.
        /// </summary>
        [Fact]
        public async Task WeaklyTypedMember_LosesItsShapeOnTheChannel()
        {
            var recorder = new WeaklyTypedRecorder();
            var (clientSide, serverSide) = FullDuplexStream.CreatePair();
            using var serverRpc = JsonRpc.Attach(serverSide, serverSide, recorder);
            using var clientRpc = JsonRpc.Attach(clientSide, clientSide);
            var client = clientRpc.Attach<IWeaklyTypedProbe>();

            await client.SendAsync(
                new WeaklyTypedRequest
                {
                    Arguments = (Dictionary<string, object?>)JsonElementDeserializer.DeserializeToObject(
                        JsonDocument.Parse(CreateEntitiesArguments).RootElement
                        )!
                });

            //what the proxy would hand to the MCP SDK, serialized the way the SDK serializes it
            var reserialized = JsonSerializer.Serialize(recorder.Received!.Arguments);

            Assert.False(
                JsonNode.DeepEquals(
                    JsonNode.Parse(CreateEntitiesArguments),
                    JsonNode.Parse(reserialized)
                    ),
                "the channel now carries weakly typed members losslessly; ArgumentsJson may become a dictionary again"
                );
        }

        /// <summary>A <see cref="JsonRpc"/> pair joined by an in-process duplex stream, standing in for the VSIX and the proxy process.</summary>
        private sealed class ProxyChannel : IDisposable
        {
            private readonly JsonRpc _serverRpc;
            private readonly JsonRpc _clientRpc;
            private readonly Stream _clientSide;
            private readonly Stream _serverSide;

            /// <summary>The proxy as the VSIX sees it: a generated implementation talking over the channel.</summary>
            public IMcpProxyInterface Client
            {
                get;
            }

            private ProxyChannel(
                Stream clientSide,
                Stream serverSide,
                JsonRpc serverRpc,
                JsonRpc clientRpc,
                IMcpProxyInterface client
                )
            {
                _clientSide = clientSide;
                _serverSide = serverSide;
                _serverRpc = serverRpc;
                _clientRpc = clientRpc;
                Client = client;
            }

            /// <summary>Connects <paramref name="target"/> as the far end of the channel.</summary>
            public static ProxyChannel Open(
                IMcpProxyInterface target
                )
            {
                var (clientSide, serverSide) = FullDuplexStream.CreatePair();

                var serverRpc = JsonRpc.Attach(serverSide, serverSide, target);
                var clientRpc = JsonRpc.Attach(clientSide, clientSide);

                return new ProxyChannel(
                    clientSide,
                    serverSide,
                    serverRpc,
                    clientRpc,
                    clientRpc.Attach<IMcpProxyInterface>()
                    );
            }

            public void Dispose()
            {
                _clientRpc.Dispose();
                _serverRpc.Dispose();
                _clientSide.Dispose();
                _serverSide.Dispose();
            }
        }

        /// <summary>Stands in for the proxy process: answers every call with an empty success and keeps what it was asked.</summary>
        private sealed class RecordingProxy : IMcpProxyInterface
        {
            public CallToolRequest? LastCallToolRequest
            {
                get;
                private set;
            }

            public string ToolSchemaJson
            {
                get;
                set;
            } = """{"type":"object"}""";

            public Task<CallToolReply> CallToolAsync(CallToolRequest request, CancellationToken cancellationToken)
            {
                LastCallToolRequest = request;
                return Task.FromResult(new CallToolReply(false, []));
            }

            public Task<GetToolsReply> GetToolsAsync(GetToolsRequest request)
            {
                return Task.FromResult(
                    new GetToolsReply([new GetToolReply(InProcessMcpServer.ToolName, "test tool", ToolSchemaJson)])
                    );
            }

            public Task<InstallReply> InstallAsync(InstallRequest request) => Task.FromResult(new InstallReply());

            public Task<IsInstalledReply> IsInstalledAsync(IsInstalledRequest request) => Task.FromResult(new IsInstalledReply(true));

            public Task<UpdateExternalServersReply> UpdateExternalServersAsync(UpdateExternalServersRequest request)
                => Task.FromResult(new UpdateExternalServersReply(new McpServers()));
        }

        /// <summary>The shape <see cref="CallToolRequest"/> used to have, kept alive only by <see cref="WeaklyTypedMember_LosesItsShapeOnTheChannel"/>.</summary>
        public sealed class WeaklyTypedRequest
        {
            public Dictionary<string, object?>? Arguments
            {
                get;
                set;
            }
        }

        private interface IWeaklyTypedProbe
        {
            Task SendAsync(WeaklyTypedRequest request);
        }

        private sealed class WeaklyTypedRecorder : IWeaklyTypedProbe
        {
            public WeaklyTypedRequest? Received
            {
                get;
                private set;
            }

            public Task SendAsync(WeaklyTypedRequest request)
            {
                Received = request;
                return Task.CompletedTask;
            }
        }
    }
}
