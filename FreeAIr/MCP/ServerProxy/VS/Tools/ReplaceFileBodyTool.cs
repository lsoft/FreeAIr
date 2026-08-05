using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.UI.Difference;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>
    /// The `ReplaceFileBody` MCP tool: writes a new body for a solution file, but only after showing
    /// the user an inline diff (<see cref="DifferenceShower"/>) between the old and new content and
    /// letting them adjust or reject it - the model never edits a file unseen.
    /// </summary>
    public sealed class ReplaceFileBodyTool : VisualStudioMcpServerTool
    {
        /// <summary>Shared singleton instance registered with the MCP tool catalog.</summary>
        public static readonly ReplaceFileBodyTool Instance = new();

        /// <summary>The MCP tool name exposed to the model.</summary>
        public const string VisualStudioToolName = "ReplaceFileBody";

        /// <summary>JSON schema property name for the target file's name or path.</summary>
        public const string FileNamePathParameterName = "file_name_or_full_path";
        /// <summary>JSON schema property name for the proposed new file body.</summary>
        public const string NewFileBodyParameterName = "new_file_body";

        /// <summary>Registers the tool under <see cref="VisualStudioToolName"/> with its JSON input schema for the target file and new body.</summary>
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

        /// <summary>
        /// Resolves the target file, opens the inline-diff view against the proposed body, and applies
        /// the (possibly user-edited) result only if it differs from the original; otherwise reports
        /// the call as postponed rather than failed, since a user-driven rejection is not an error.
        /// </summary>
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

        /// <summary>Re-splits the model's proposed body and rejoins it with the target file's actual line-ending style, so a model that always writes `\n` does not flip a CRLF file to LF.</summary>
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

        /// <summary>Writes the new body into the file's already-open editor buffer if there is one, so undo and unsaved markers behave normally; falls back to a plain file write otherwise.</summary>
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

        /// <summary>The tool's success result: a one-line confirmation that the document was updated.</summary>
        private sealed class UpdateBodyResultJson
        {
            /// <summary>The confirmation text returned to the model, e.g. "The document successfully updated".</summary>
            public string ResultMessage
            {
                get;
                set;
            }
        }

    }

}
