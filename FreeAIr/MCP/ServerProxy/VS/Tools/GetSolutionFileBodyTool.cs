using FreeAIr.Helper;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>
    /// The `GetSolutionFileBody` MCP tool: given a file name or full path, finds the matching item in
    /// the open solution and returns its current text content - the model's way to read a file
    /// through Visual Studio rather than the raw filesystem, so unsaved editor changes are included.
    /// </summary>
    public sealed class GetSolutionFileBodyTool : VisualStudioMcpServerTool
    {
        private const string FileNamePathParameterName = "file_name_or_full_path";

        public static readonly GetSolutionFileBodyTool Instance = new();

        public const string VisualStudioToolName = "GetSolutionFileBody";

        public GetSolutionFileBodyTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Returns a JSON-formatted information about solution file (document, item). The result of this function includes: file name, file full path, file kind and its content (body, text) for each found file.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{FileNamePathParameterName}}}": {
                        "type": "string",
                        "description": "Full path or name of solution file"
                        }
                    },
                    "required": ["{{{FileNamePathParameterName}}}"]
                }
                """
                )
        {
        }

        /// <summary>Resolves the requested file via <see cref="SolutionHelper.FindItemByNameOrFilePathAsync"/> and reads its actual (editor-aware) body.</summary>
        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!arguments.TryGetValue(FileNamePathParameterName, out var nameOrPathOfItemObj))
            {
                return McpServerProxyToolCallResult.CreateFailed($"Parameter {FileNamePathParameterName} does not found.");
            }

            var nameOrPathOfItem = nameOrPathOfItemObj as string;

            var item = await SolutionHelper.FindItemByNameOrFilePathAsync(
                nameOrPathOfItem,
                cancellationToken
                );
            if (item is null)
            {
                return McpServerProxyToolCallResult.CreateFailed($"File {nameOrPathOfItem} does not found in current solution.");
            }

            var itemBody = await SolutionHelper.GetActualItemBodyAsync(item.SolutionItem.FullPath);

            var packed = new SolutionItemBodiesJson
            {
                SolutionItemBodies =
                [
                    new SolutionItemBodyJson
                {
                    ItemName = item.SolutionItem.Name,
                    ItemFullPath = item.SolutionItem.FullPath,
                    ItemType = item.SolutionItem.Type.ToString(),
                    ItemBody = itemBody
                }
                ]
            };

            var result = JsonSerializer.Serialize(packed);

            return McpServerProxyToolCallResult.CreateSuccess(result);
        }


        /// <summary>The tool's JSON result envelope, kept a one-element array for parity with the multi-file shape other MCP tools use.</summary>
        private sealed class SolutionItemBodiesJson
        {
            public SolutionItemBodyJson[] SolutionItemBodies
            {
                get;
                set;
            }
        }

        /// <summary>One solution item's name, path, kind and full text content.</summary>
        private sealed class SolutionItemBodyJson
        {
            public string ItemName
            {
                get;
                set;
            }

            public string ItemType
            {
                get;
                set;
            }

            public string? ItemFullPath
            {
                get;
                set;
            }

            public string ItemBody
            {
                get;
                set;
            }
        }
    }

}
