using Dto;
using FreeAIr.Helper;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FreeAIr.Mcp.Tests
{
    /// <summary>
    /// Covers <see cref="ToolArguments"/>, the conversion between the objects the VSIX builds out of
    /// the model's answer and the JSON text a <see cref="CallToolRequest"/> carries to the proxy.
    /// The MCP specification lets a tool argument be any JSON value, so what is asserted here is
    /// that nothing about a value's shape - nesting, emptiness, the difference between an object and
    /// an array - is decided by the transport.
    /// </summary>
    public class ToolArgumentsTests
    {
        /// <summary>The payload from issue #70: an array of objects, which is what used to arrive as nested empty arrays.</summary>
        private const string CreateEntitiesArguments =
            """
            {"entities":[{"name":"Alice","entityType":"person","observations":["likes tea"]}]}
            """;

        [Fact]
        public void Serialize_NoArguments_YieldsNothingToSend()
        {
            Assert.Null(ToolArguments.Serialize(null));
        }

        [Fact]
        public void Serialize_EmptyArguments_YieldsAnEmptyJsonObject()
        {
            var json = ToolArguments.Serialize(new Dictionary<string, object?>());

            Assert.Equal("{}", json);
        }

        [Fact]
        public void Deserialize_NothingSent_YieldsNoArguments()
        {
            Assert.Null(ToolArguments.Deserialize(null));
            Assert.Null(ToolArguments.Deserialize(string.Empty));
            Assert.Null(ToolArguments.Deserialize("   "));
        }

        [Fact]
        public void RoundTrip_ArrayOfObjects_KeepsEveryMember()
        {
            var arguments = JsonElementDeserializer.DeserializeToObject(
                JsonDocument.Parse(CreateEntitiesArguments).RootElement
                );

            var revived = RoundTrip((Dictionary<string, object?>)arguments!);

            AssertJsonEqual(CreateEntitiesArguments, revived);
        }

        [Fact]
        public void RoundTrip_Scalars_KeepTheirTypes()
        {
            var revived = RoundTrip(
                new Dictionary<string, object?>
                {
                    ["text"] = "hello",
                    ["count"] = 42,
                    ["ratio"] = 0.5,
                    ["enabled"] = true,
                    ["disabled"] = false,
                    ["nothing"] = null,
                });

            Assert.Equal(JsonValueKind.String, revived["text"].ValueKind);
            Assert.Equal("hello", revived["text"].GetString());
            Assert.Equal(JsonValueKind.Number, revived["count"].ValueKind);
            Assert.Equal(42, revived["count"].GetInt32());
            Assert.Equal(0.5, revived["ratio"].GetDouble());
            Assert.Equal(JsonValueKind.True, revived["enabled"].ValueKind);
            Assert.Equal(JsonValueKind.False, revived["disabled"].ValueKind);
            Assert.Equal(JsonValueKind.Null, revived["nothing"].ValueKind);
        }

        [Fact]
        public void RoundTrip_EmptyContainers_StayObjectsAndArrays()
        {
            //an empty object and an empty array are different arguments to a server, and both are
            //valid; a conversion which cannot tell them apart is a conversion which loses one
            var revived = RoundTrip(
                new Dictionary<string, object?>
                {
                    ["emptyObject"] = new Dictionary<string, object?>(),
                    ["emptyArray"] = new List<object?>(),
                });

            Assert.Equal(JsonValueKind.Object, revived["emptyObject"].ValueKind);
            Assert.Equal(JsonValueKind.Array, revived["emptyArray"].ValueKind);
        }

        [Fact]
        public void RoundTrip_DeeplyNestedValues_KeepTheirDepth()
        {
            const string Nested =
                """
                {"a":{"b":[{"c":[[1,2],[3]]},{"c":[]}]},"d":[[["deep"]]]}
                """;

            var arguments = (Dictionary<string, object?>)JsonElementDeserializer.DeserializeToObject(
                JsonDocument.Parse(Nested).RootElement
                )!;

            AssertJsonEqual(Nested, RoundTrip(arguments));
        }

        [Fact]
        public void RoundTrip_NonAsciiAndEscapes_ArePreserved()
        {
            const string Awkward = "кириллица \"quoted\" \\ backslash \n newline \t tab 🙂";

            var revived = RoundTrip(
                new Dictionary<string, object?>
                {
                    ["text"] = Awkward,
                });

            Assert.Equal(Awkward, revived["text"].GetString());
        }

        [Fact]
        public void Deserialize_Values_OutliveTheCall()
        {
            //the elements must not be windows into a JsonDocument this method disposed on its way
            //out: the proxy reads them long after, when it builds the outgoing tools/call
            var revived = ToolArguments.Deserialize(CreateEntitiesArguments)!;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.Equal("Alice", revived["entities"][0].GetProperty("name").GetString());
        }

        /// <summary>Sends the arguments the way <see cref="CallToolRequest"/> does and reads them back the way the proxy does.</summary>
        private static IReadOnlyDictionary<string, JsonElement> RoundTrip(
            Dictionary<string, object?> arguments
            )
        {
            var request = new CallToolRequest("server", "tool", arguments);

            return ToolArguments.Deserialize(request.ArgumentsJson)!;
        }

        /// <summary>Asserts the revived arguments are the same JSON as <paramref name="expectedJson"/>, whitespace and member order aside.</summary>
        private static void AssertJsonEqual(
            string expectedJson,
            IReadOnlyDictionary<string, JsonElement> actual
            )
        {
            var actualObject = new JsonObject();
            foreach (var pair in actual)
            {
                actualObject[pair.Key] = JsonNode.Parse(pair.Value.GetRawText());
            }

            Assert.True(
                JsonNode.DeepEquals(JsonNode.Parse(expectedJson), actualObject),
                $"expected {expectedJson}, got {actualObject.ToJsonString()}"
                );
        }
    }
}
