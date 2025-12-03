using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.UI.Difference;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public sealed class ReplaceFileBodyTool : VisualStudioMcpServerTool
    {
        public static readonly ReplaceFileBodyTool Instance = new();

        public const string VisualStudioToolName = "ReplaceFileBody";

        public const string FileNamePathParameterName = "file_name_or_full_path";
        public const string NewFileBodyParameterName = "new_file_body";

        public ReplaceFileBodyTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Replaces the file (document, item) body (content, text) with a new one. Use this function if you need to make a changes in the file.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{FileNamePathParameterName}}}": {
                        "type": "string",
                        "description": "Full path or name of the solution file (document, item) in which you want to change the body (text, content)"
                        },
                        "{{{NewFileBodyParameterName}}}": {
                        "type": "string",
                        "description": "A new body (text, content) of solution file."
                        }
                    },
                    "required": ["{{{FileNamePathParameterName}}}", "{{{NewFileBodyParameterName}}}"]
                }
                """
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

            if (!arguments.TryGetValue(FileNamePathParameterName, out var itemNamePathObj))
            {
                return McpServerProxyToolCallResult.CreateFailed($"Parameter {FileNamePathParameterName} does not found.");
            }
            var nameOrPathOfItem = itemNamePathObj as string;

            var item = await SolutionHelper.FindItemByNameOrFilePathAsync(
                nameOrPathOfItem,
                cancellationToken
                );
            if (item is null)
            {
                return McpServerProxyToolCallResult.CreateFailed($"File {nameOrPathOfItem} does not found in current solution.");
            }

            if (!arguments.TryGetValue(NewFileBodyParameterName, out var draftBodyOfNewItemObj))
            {
                return McpServerProxyToolCallResult.CreateFailed($"Parameter {NewFileBodyParameterName} does not found.");
            }
            var draftBodyOfNewItem = draftBodyOfNewItemObj as string;

            var newBody = ComposeItemNewBody(item, draftBodyOfNewItem);

            var solution = await Community.VisualStudio.Toolkit.VS.Solutions.GetCurrentSolutionAsync();

            var itemFullPath = item.SolutionItem.FullPath;
            var itemRelativePath = itemFullPath.MakeRelativeAgainst(solution.FullPath);

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            dte.ExecuteCommand("OtherContextMenus.inlinediffsettings.Diff.InlineView");

            var origFileBody = await SolutionHelper.GetActualItemBodyAsync(
                itemFullPath
                );

            var itemFileInfo = new System.IO.FileInfo(itemFullPath);
            var itemFileName = itemFileInfo.Name;

            var caption = $"Changes for the file {itemRelativePath}";
            var tooltip = "Inline diff between the source file and the modified file";
            var leftLabel = itemRelativePath;
            var rightLabel = itemRelativePath + " (modified)";

            var parameters = new DifferenceShowerParameters(
                fileName: itemFileName,
                originalFileBody: origFileBody,
                modifiedFileBody: newBody,
                caption: caption,
                tooltip: tooltip,
                leftLabel: leftLabel,
                rightLabel: rightLabel
                );

            var twiceChangedBody = await DifferenceShower.ShowAsync(
                parameters
                );
            if (!string.IsNullOrEmpty(twiceChangedBody))
            {
                if (twiceChangedBody != origFileBody)
                {
                    await UpdateItemBodyAsync(
                        itemFullPath,
                        twiceChangedBody
                        );

                    var result = JsonSerializer.Serialize(
                        new UpdateBodyResultJson
                        {
                            ResultMessage = "The document successfully updated"
                        }
                        );

                    return McpServerProxyToolCallResult.CreateSuccess([result]);
                }
            }

            return McpServerProxyToolCallResult.CreatePostponed();
        }

        private static string ComposeItemNewBody(
            SolutionHelper.FoundSolutionItem item,
            string draftBodyOfNewItem
            )
        {
            var lineEndings = LineEndingHelper.Actual.GetDocumentLineEnding(item.SolutionItem.FullPath);
            var lines = draftBodyOfNewItem.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
                );

            var newBody = string.Join(lineEndings, lines);
            return newBody;
        }

        public static async Task UpdateItemBodyAsync(
            string fullPath,
            string body
            )
        {
            var openedDocument = await Community.VisualStudio.Toolkit.VS.Documents.GetDocumentViewAsync(
                fullPath
                );
            if (openedDocument is not null)
            {
                var currentText = openedDocument.Document.TextBuffer.CurrentSnapshot.GetText();

                using var edit = openedDocument.Document.TextBuffer.CreateEdit();
                edit.Replace(0, currentText.Length, body);
                edit.Apply();

                return;
            }

            System.IO.File.WriteAllText(fullPath, body);
        }

        private sealed class UpdateBodyResultJson
        {
            public string ResultMessage
            {
                get;
                set;
            }
        }

    }

}
