using FreeAIr.BuildErrors;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>
    /// The `BuildAndReturnBuildResults` MCP tool: builds the current solution and hands the model
    /// back its errors and warnings as JSON, so it can fix a build without a human relaying compiler
    /// output.
    /// </summary>
    public sealed class BuildAndReturnBuildResultsTool : VisualStudioMcpServerTool
    {
        /// <summary>The single shared instance of this tool.</summary>
        public static readonly BuildAndReturnBuildResultsTool Instance = new();

        /// <summary>The tool name exposed to the model, "BuildAndReturnBuildResults".</summary>
        public const string VisualStudioToolName = "BuildAndReturnBuildResults";

        /// <summary>Registers this tool's name and description; it takes no parameters, so a solution build is always the whole solution.</summary>
        public BuildAndReturnBuildResultsTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Build (compile) a solution and returns a JSON-formatted list of build warning and build errors of current solution. The result of this function includes: a type (error, warning), a description, file path, and line and column where error (warning) was found.",
                NoParameters
                )
        {
        }

        /// <summary>Builds the solution on the main thread, then serializes the errors and warnings <see cref="BuildResultProvider"/> collected into the tool's result.</summary>
        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var buildResult = await Community.VisualStudio.Toolkit.VS.Build.BuildSolutionAsync();

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
            /// <summary>The flat list of build errors and warnings collected from the last solution build.</summary>
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
