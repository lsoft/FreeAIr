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
        /// <summary>The JSON schema property name the model must supply the file name or full path under.</summary>
        private const string FileNamePathParameterName = "file_name_or_full_path";

        /// <summary>The single shared instance of this tool.</summary>
        public static readonly GetSolutionFileBodyTool Instance = new();

        /// <summary>The tool name exposed to the model, "GetSolutionFileBody".</summary>
        public const string VisualStudioToolName = "GetSolutionFileBody";

        /// <summary>Registers this tool's name, description and JSON parameter schema with the base MCP tool infrastructure.</summary>
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
            /// <summary>The one-element list of solution item bodies returned by this tool.</summary>
            public SolutionItemBodyJson[] SolutionItemBodies
            {
                get;
                set;
            }
        }

        /// <summary>One solution item's name, path, kind and full text content.</summary>
        private sealed class SolutionItemBodyJson
        {
            /// <summary>The item's display name (file name).</summary>
            public string ItemName
            {
                get;
                set;
            }

            /// <summary>The kind of solution item (e.g. physical file, project, folder).</summary>
            public string ItemType
            {
                get;
                set;
            }

            /// <summary>Full path of the item on disk, if it has one.</summary>
            public string? ItemFullPath
            {
                get;
                set;
            }

            /// <summary>The item's current text content, including unsaved editor changes.</summary>
            public string ItemBody
            {
                get;
                set;
            }
        }
    }

}
