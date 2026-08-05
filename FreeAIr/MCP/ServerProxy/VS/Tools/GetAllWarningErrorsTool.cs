using FreeAIr.BuildErrors;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>
    /// The `GetAllWarningErrors` MCP tool: reports the solution's current build errors and warnings
    /// as JSON without triggering a build, unlike <see cref="BuildAndReturnBuildResultsTool"/> -
    /// cheap to call repeatedly while a model is iterating on a fix.
    /// </summary>
    public sealed class GetAllWarningErrorsTool : VisualStudioMcpServerTool
    {
        /// <summary>The single shared instance of this tool.</summary>
        public static readonly GetAllWarningErrorsTool Instance = new();

        /// <summary>The tool name exposed to the model, "GetAllWarningErrors".</summary>
        public const string VisualStudioToolName = "GetAllWarningErrors";

        /// <summary>Registers this tool's name and description; it takes no parameters, since it always reports the whole solution's current build items.</summary>
        public GetAllWarningErrorsTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Returns a JSON-formatted list of build warning and build errors of current solution. The result of this function includes: a type (error, warning), a description, file path, and line and column where error (warning) was found.",
                NoParameters
                )
        {
        }

        /// <summary>Serializes the build items <see cref="BuildResultProvider"/> already has cached, without rebuilding.</summary>
        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var informations = await BuildResultProvider.GetBuildResultInformationsAsync();

            var converted = informations
                .Select(i => new BuildInformationJson
                {
                    Description = i.ErrorDescription,
                    Type = i.Type.ToString(),
                    FullPath = i.FilePath,
                    TextLine = i.Line,
                    TextColumn = i.Column
                })
                .ToArray();
            var packed = new BuildInformationsJson
            {
                SolutionItems = converted
            };

            var result = JsonSerializer.Serialize(packed);

            return McpServerProxyToolCallResult.CreateSuccess(result);
        }

        /// <summary>The tool's JSON result: the flat list of build items for the whole solution.</summary>
        private sealed class BuildInformationsJson
        {
            /// <summary>The flat list of build errors and warnings currently known for the solution.</summary>
            public BuildInformationJson[] SolutionItems
            {
                get;
                set;
            }
        }

        /// <summary>One build error or warning: its kind, message and source location.</summary>
        private sealed class BuildInformationJson
        {
            /// <summary>The compiler's error or warning message text.</summary>
            public string Description
            {
                get;
                set;
            }

            /// <summary>Whether this item is an "error" or a "warning".</summary>
            public string Type
            {
                get;
                set;
            }

            /// <summary>Full path of the source file the error or warning was reported against.</summary>
            public string FullPath
            {
                get;
                set;
            }

            /// <summary>Line number in the source file where the error or warning was reported.</summary>
            public int TextLine
            {
                get;
                set;
            }

            /// <summary>Column number in the source file where the error or warning was reported.</summary>
            public int TextColumn
            {
                get;
                set;
            }
        }
    }

}
