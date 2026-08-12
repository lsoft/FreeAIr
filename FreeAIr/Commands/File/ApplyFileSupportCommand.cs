using EnvDTE;
using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.UI.ToolWindows;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Composer;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.Commands.File
{
    /// <summary>
    /// The Solution Explorer menu command that applies a chosen support action (from
    /// Options2/Support) to the selected files: it starts a new chat, adds the files' composed
    /// context, and seeds the chat with the support action's prompt.
    /// </summary>
    [Command(PackageIds.ApplyFileSupportCommandId)]
    internal sealed class ApplyFileSupportCommand : BaseCommand<ApplyFileSupportCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public ApplyFileSupportCommand(
            )
        {
        }

        /// <summary>
        /// Runs the apply-file-support flow: gathers the selected files, lets the user pick a
        /// support action and agent, starts a chat, adds the files to its context, and sends the
        /// resulting prompt.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = await MefHelper.GetComponentModelAsync();
            var chatContainer = componentModel.GetService<ChatContainer>();

            var allSelectedFiles = await GetSelectedFilesAsync();

            var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                "Choose support action:",
                SupportScopeEnum.FileInSolutionTree
                );
            if (chosenSupportAction is null)
            {
                return;
            }

            var supportContext = await SupportContext.WithSolutionItemsAsync(
                allSelectedFiles
                );

            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                "Choose agent:",
                chosenSupportAction.AgentName
                );
            if (chosenAgent is null)
            {
                return;
            }

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    null
                    ),
                null,
                await FreeAIr.Chat.ChatOptions.GetDefaultAsync(chosenAgent)
                );
            if (chat is null)
            {
                return;
            }

            await AddFilesToContextAsync(
                chat,
                allSelectedFiles
                );

            var promptText = supportContext.ApplyVariablesToPrompt(
                chosenSupportAction.Prompt
                );

            chat.AddPrompt(
                UserPrompt.CreateTextBasedPrompt(
                    promptText
                    )
                );

            await ChatWindowShower.ShowChatWindowAsync(chat);
        }

        /// <summary>
        /// Adds each given solution file to the chat's context, both as a raw file reference and
        /// as its composed C# context items (symbols, outlines, etc.).
        /// </summary>
        public static async Task AddFilesToContextAsync(
            FreeAIr.Chat.Chat chat,
            List<SolutionItem> allSelectedFiles
            )
        {
            foreach (var selectedFile in allSelectedFiles)
            {
                var contextItems = (await CSharpContextComposer.ComposeFromFilePathAsync(
                    selectedFile.FullPath
                    )).ConvertToChatContextItem();

                chat.ChatContext.AddItem(
                    new SolutionItemChatContextItem(
                        SelectedIdentifier.Create(
                            selectedFile.FullPath,
                            null
                            ),
                        false,
                        AddLineNumbersMode.NotRequired
                        )
                    );

                chat.ChatContext.AddItems(
                    contextItems
                    );
            }
        }

        /// <summary>
        /// Returns the text files currently selected in Solution Explorer, expanding any selected
        /// folders into their child files.
        /// </summary>
        public static async System.Threading.Tasks.Task<List<SolutionItem>> GetSelectedFilesAsync()
        {
            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = await sew.GetSelectionAsync();

            return await GetChildrenOfFilesAsync(
                selections
                );
        }

        /// <summary>
        /// Recursively expands each given solution item into its visible, text-file descendants
        /// (or itself, if it already is one), used to flatten folder selections to files.
        /// </summary>
        public static async System.Threading.Tasks.Task<List<SolutionItem>> GetChildrenOfFilesAsync(
            IEnumerable<SolutionItem> selections
            )
        {
            var allChildren = new List<SolutionItem>();
            foreach (var selection in selections)
            {
                var children = await selection.ProcessDownRecursivelyForAsync(
                    item =>
                        !item.IsNonVisibleItem
                        && item.Type == SolutionItemType.PhysicalFile
                        && item.FullPath.GetFileType() == FileTypeEnum.Text
                        ,
                    false,
                    CancellationToken.None
                    );
                allChildren.AddRange(children.Select(c => c.SolutionItem));
            }

            return allChildren;
        }
    }
}
