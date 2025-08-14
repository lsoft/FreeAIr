using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.Helper.Frame;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.MCP.McpServerProxy.VS.Tools;
using FreeAIr.UI.Difference;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Shell.Interop;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfHelpers;

namespace FreeAIr.UI.Dialog.Content.Tools
{
    /// <summary>
    /// Interaction logic for VisualStudio_ReplaceDocumentBody_UserControl.xaml
    /// </summary>
    public partial class VisualStudio_ReplaceDocumentBody_UserControl : UserControl
    {
        private readonly StreamingChatToolCallUpdate _toolCall;
        private readonly string _itemFullPath;
        private readonly string _relativePath;
        private readonly string _newItemBody;
        private ICommand _previewContentCommand;

        public string PreviewContent => $"Preview changes for {_relativePath}...";

        public ICommand PreviewContentCommand
        {
            get
            {
                if (_previewContentCommand is null)
                {
                    _previewContentCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            try
                            {
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                                var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
                                dte.ExecuteCommand("OtherContextMenus.inlinediffsettings.Diff.InlineView");

                                var origFileBody = await GetSolutionItemBodyTool.GetItemBodyAsync(
                                    _itemFullPath
                                    );

                                var itemFileInfo = new FileInfo(_itemFullPath);
                                var itemFileName = itemFileInfo.Name;

                                //var origFile = TempFile.CreateBasedOfExistingName(
                                //    itemFileName,
                                //    "current"
                                //    );
                                //origFile.WriteAllText(origFileBody);

                                //var changedFile = TempFile.CreateBasedOfExistingName(
                                //    itemFileName,
                                //    "modified"
                                //    );
                                //changedFile.WriteAllText(_newItemBody);

                                var caption = $"Changes for the file {_relativePath}";
                                var tooltip = "Inline diff between the source file and the modified file";
                                var leftLabel = _relativePath;
                                var rightLabel = _relativePath + " (modified)";

                                await DifferenceShower.ShowAsync(
                                    new DifferenceShowerParameters(
                                        fileName: itemFileName,
                                        originalFileBody: origFileBody,
                                        modifiedFileBody: _newItemBody,
                                        caption: caption,
                                        tooltip: tooltip,
                                        leftLabel: leftLabel,
                                        rightLabel: rightLabel,
                                        frameClosedAction:
                                            async (p) =>
                                            {
                                                var twiceChangedBody = p.RightFile.ReadAllText();
                                                if (twiceChangedBody != origFileBody)
                                                {
                                                    await ReplaceDocumentBodyTool.UpdateItemBodyAsync(
                                                        _itemFullPath,
                                                        twiceChangedBody
                                                        );
                                                }
                                            }
                                        )
                                    );
                            }
                            catch (Exception excp)
                            {
                                excp.ActivityLogException();
                            }
                        }
                        );
                }

                return _previewContentCommand;
            }
        }

        public VisualStudio_ReplaceDocumentBody_UserControl(
            StreamingChatToolCallUpdate toolCall,
            string itemFullPath,
            string relativePath,
            string newItemBody
            )
        {
            if (toolCall is null)
            {
                throw new ArgumentNullException(nameof(toolCall));
            }

            if (itemFullPath is null)
            {
                throw new ArgumentNullException(nameof(itemFullPath));
            }

            if (relativePath is null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (newItemBody is null)
            {
                throw new ArgumentNullException(nameof(newItemBody));
            }

            _toolCall = toolCall;
            _itemFullPath = itemFullPath;
            _relativePath = relativePath;
            _newItemBody = newItemBody;

            InitializeComponent();
        }


        public static VisualStudio_ReplaceDocumentBody_UserControl? Create(
            StreamingChatToolCallUpdate toolCall
            )
        {
            if (toolCall is null)
            {
                throw new ArgumentNullException(nameof(toolCall));
            }


            var toolArguments = toolCall.ParseToolInvocationArguments();
            if (!toolArguments.TryGetValue(ReplaceDocumentBodyTool.ItemNamePathParameterName, out var itemNamePathObj))
            {
                return null;
            }
            var itemNamePath = itemNamePathObj as string;
            if (!System.IO.File.Exists(itemNamePath))
            {
                return null;
            }

            if (!toolArguments.TryGetValue(ReplaceDocumentBodyTool.NewItemBodyParameterName, out var newItemBodyObj))
            {
                return null;
            }
            var newItemBody = newItemBodyObj as string;


            var itemFullPath = new FileInfo(itemNamePath).FullName;

            var solution = VS.Solutions.GetCurrentSolution();
            var itemRelativePath = itemFullPath.MakeRelativeAgainst(solution.FullPath);

            return new VisualStudio_ReplaceDocumentBody_UserControl(
                toolCall,
                itemFullPath,
                itemRelativePath,
                newItemBody
                );
        }
    }
}
