using OpenAI.Chat;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace FreeAIr.MCP.McpServerProxy
{
    public sealed class McpServerTools
    {
        public IReadOnlyList<McpServerTool> Tools
        {
            get;
        }

        public McpServerTools(
            IReadOnlyList<McpServerTool> tools
            )
        {
            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }

            Tools = tools;
        }
    }

    public /*sealed*/ class McpServerTool
    {
        /// <summary>
        /// The schema a tool taking no arguments has to declare. A bare `{}` is not a valid
        /// function schema: LM Studio validates `function.parameters` and answers the whole
        /// request with 400 Bad Request when a single offered tool fails that validation.
        /// </summary>
        public const string NoParameters =
            """
            {
                "type": "object",
                "properties": {}
            }
            """;

        public string McpServerProxyName
        {
            get;
        }

        public string ToolName
        {
            get;
        }

        public string FullName => McpServerProxyName + "." + ToolName;

        public string Description
        {
            get;
        }

        public string Parameters
        {
            get;
        }

        public McpServerTool(
            string mcpServerProxyName,
            string toolName,
            string description,
            string parameters
            )
        {
            if (string.IsNullOrEmpty(mcpServerProxyName))
            {
                throw new ArgumentException($"'{nameof(mcpServerProxyName)}' cannot be null or empty.", nameof(mcpServerProxyName));
            }

            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException($"'{nameof(toolName)}' cannot be null or empty.", nameof(toolName));
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException($"'{nameof(description)}' cannot be null or empty.", nameof(description));
            }

            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            McpServerProxyName = mcpServerProxyName;
            ToolName = toolName;
            Description = description;
            Parameters = parameters;

            if (FullName.Contains(' '))
            {
                throw new InvalidOperationException($"Function '{FullName}' contain spaces which is not allowed to some LLM providers.");
            }
        }

        public ChatTool CreateChatTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: FullName,
                functionDescription: Description,
                functionParameters: BinaryData.FromString(
                    NormalizeParameterSchema(Parameters)
                    ),
                functionSchemaIsStrict: true
                );
        }

        /// <summary>
        /// Brings a tool schema to the shape every OpenAI compatible endpoint accepts: an object
        /// schema carrying a `properties` member.
        ///
        /// This is not cosmetic. A tool without arguments is habitually declared as `{}` — both by
        /// FreeAIr's own Visual Studio tools and by third party MCP servers, whose schemas arrive
        /// here unseen — and LM Studio rejects the entire completion request with 400 Bad Request
        /// when one such tool is offered, taking the whole chat down with it.
        ///
        /// The schema is repaired rather than replaced, so that a schema which merely forgot its
        /// `type` keeps the parameters it does declare.
        /// </summary>
        private static string NormalizeParameterSchema(
            string parameters
            )
        {
            try
            {
                using var document = JsonDocument.Parse(parameters);

                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return NoParameters;
                }

                var hasObjectType =
                    root.TryGetProperty("type", out var type)
                    && type.ValueKind == JsonValueKind.String
                    && type.ValueEquals("object")
                    ;
                var hasProperties =
                    root.TryGetProperty("properties", out var properties)
                    && properties.ValueKind == JsonValueKind.Object
                    ;
                if (hasObjectType && hasProperties)
                {
                    return parameters;
                }

                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();

                    //the two members the endpoints insist on come first, then everything the
                    //original schema said which we have not just written ourselves
                    writer.WriteString("type", "object");
                    if (!hasProperties)
                    {
                        writer.WriteStartObject("properties");
                        writer.WriteEndObject();
                    }

                    foreach (var member in root.EnumerateObject())
                    {
                        if (member.NameEquals("type"))
                        {
                            continue;
                        }
                        if (member.NameEquals("properties") && !hasProperties)
                        {
                            continue;
                        }

                        member.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (JsonException)
            {
                //a schema which is not even json cannot be repaired, only replaced
            }

            return NoParameters;
        }
    }

    public sealed class McpServerProxyToolCallResult
    {
        public McpServerProxyToolCallResultEnum Result
        {
            get;
        }

        public string[] Content
        {
            get;
        }

        public McpServerProxyToolCallResult(
            McpServerProxyToolCallResultEnum result,
            string[] content
            )
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Result = result;
            Content = content;
        }

        public static McpServerProxyToolCallResult CreateSuccess(
            IEnumerable<string> content
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Success,
                content.ToArray()
                );
        }

        public static McpServerProxyToolCallResult CreateSuccess(
            string content
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Success,
                [ content ]
                );
        }

        public static McpServerProxyToolCallResult CreateFailed(
            string errorMessage
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Fail,
                [ errorMessage ]
                );
        }

        public static McpServerProxyToolCallResult CreatePostponed(
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Postpone,
                [string.Empty]
                );
        }
    }

    public enum McpServerProxyToolCallResultEnum
    {
        Success,
        Fail,
        Postpone
    }
}
