using FreeAIr.Llm;
using FreeAIr.Llm.Wire;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.MCP.McpServerProxy
{
    /// <summary>
    /// An immutable list of the tools a single MCP server proxy currently publishes, as offered
    /// to the chat completion request.
    /// </summary>
    public sealed class McpServerTools
    {
        /// <summary>The tools published by the server, in discovery order.</summary>
        public IReadOnlyList<McpServerTool> Tools
        {
            get;
        }

        /// <summary>
        /// Wraps a fixed list of tools published by an MCP server proxy.
        /// </summary>
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

    /// <summary>
    /// A single tool offered by an MCP server proxy, carrying the name, description and JSON
    /// schema needed to advertise it to the chat model and to route a tool call back to its server.
    /// </summary>
    public /*sealed*/ class McpServerTool
    {
        /// <summary>
        /// The schema a tool taking no arguments has to declare, for the built-in Visual Studio
        /// tools which take none. A bare `{}` is not a valid function schema: LM Studio validates
        /// `function.parameters` and answers the whole request with 400 Bad Request when a single
        /// offered tool fails that validation. The same string as
        /// <see cref="ToolSchemaNormalizer.NoParameters"/>, named here so a tool does not have to
        /// reach into the transport layer to declare itself.
        /// </summary>
        public const string NoParameters = ToolSchemaNormalizer.NoParameters;

        /// <summary>The name of the MCP server proxy (e.g. "Github", "VS") that publishes this tool.</summary>
        public string McpServerProxyName
        {
            get;
        }

        /// <summary>The tool's own name, as published by the server, without the proxy prefix.</summary>
        public string ToolName
        {
            get;
        }

        /// <summary>The fully qualified <c>Server.Tool</c> name used as the function name sent to the model.</summary>
        public string FullName => McpServerProxyName + "." + ToolName;

        /// <summary>Human-readable description of what the tool does, shown to the model as function description.</summary>
        public string Description
        {
            get;
        }

        /// <summary>The tool's JSON parameter schema, as published by its MCP server.</summary>
        public string Parameters
        {
            get;
        }

        /// <summary>
        /// Wraps a tool published by an MCP server proxy, validating that its fully qualified
        /// name contains no spaces (which some LLM providers reject as a function name).
        /// </summary>
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

        /// <summary>
        /// Describes this tool to the model in the protocol-neutral shape. The schema is left as it
        /// was published; the repair every endpoint needs is applied by the transport, because it
        /// is a rule of the wire rather than of MCP - see <see cref="ToolSchemaNormalizer"/>.
        /// </summary>
        public LlmToolDefinition CreateToolDefinition()
        {
            return new LlmToolDefinition(
                FullName,
                Description,
                Parameters
                );
        }

    }

    /// <summary>
    /// The outcome of invoking a tool through an MCP server proxy: whether it succeeded, failed,
    /// or must be postponed, together with the resulting content lines to feed back to the model.
    /// </summary>
    public sealed class McpServerProxyToolCallResult
    {
        /// <summary>Which of success, failure or postponement this call resulted in.</summary>
        public McpServerProxyToolCallResultEnum Result
        {
            get;
        }

        /// <summary>The result content (or error message) lines to return to the chat model.</summary>
        public string[] Content
        {
            get;
        }

        /// <summary>
        /// Creates a tool call result with the given outcome and content lines.
        /// </summary>
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

        /// <summary>
        /// Creates a successful result carrying multiple content lines.
        /// </summary>
        public static McpServerProxyToolCallResult CreateSuccess(
            IEnumerable<string> content
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Success,
                content.ToArray()
                );
        }

        /// <summary>
        /// Creates a successful result carrying a single content string.
        /// </summary>
        public static McpServerProxyToolCallResult CreateSuccess(
            string content
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Success,
                [ content ]
                );
        }

        /// <summary>
        /// Creates a failed result carrying the error message to surface back to the model.
        /// </summary>
        public static McpServerProxyToolCallResult CreateFailed(
            string errorMessage
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Fail,
                [ errorMessage ]
                );
        }

        /// <summary>
        /// Creates a postponed result, used when the tool call cannot complete yet (e.g. it needs
        /// user confirmation or the server is still starting) and should be retried later.
        /// </summary>
        public static McpServerProxyToolCallResult CreatePostponed(
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Postpone,
                [string.Empty]
                );
        }
    }

    /// <summary>
    /// Outcome of a single MCP tool call: <see cref="Success"/>, <see cref="Fail"/>, or
    /// <see cref="Postpone"/> when it needs to be retried later.
    /// </summary>
    public enum McpServerProxyToolCallResultEnum
    {
        /// <summary>The tool call completed and produced usable content.</summary>
        Success,
        /// <summary>The tool call failed; the content carries the error message.</summary>
        Fail,
        /// <summary>The tool call could not run yet and should be retried later.</summary>
        Postpone
    }
}
