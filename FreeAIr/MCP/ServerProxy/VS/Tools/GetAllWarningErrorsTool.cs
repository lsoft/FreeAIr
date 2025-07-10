using FreeAIr.BuildErrors;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public sealed class GetAllWarningErrorsTool : VisualStudioMcpServerTool
    {
        public static readonly GetAllWarningErrorsTool Instance = new();

        public const string VisualStudioToolName = "GetAllWarningErrors";

        public GetAllWarningErrorsTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Returns a JSON-formatted list of build warning and build errors of current solution. The result of this function includes: a type (error, warning), a description, file path, and line and column where error (warning) was found.",
                "{}"
                )
        {
        }

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

            return new McpServerProxyToolCallResult([result]);
        }

        private sealed class BuildInformationsJson
        {
            public BuildInformationJson[] SolutionItems
            {
                get;
                set;
            }
        }

        private sealed class BuildInformationJson
        {
            public string Description
            {
                get;
                set;
            }

            public string Type
            {
                get;
                set;
            }

            public string FullPath
            {
                get;
                set;
            }
            public int TextLine
            {
                get;
                set;
            }
            public int TextColumn
            {
                get;
                set;
            }
        }
    }

}
