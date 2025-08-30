using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Commands
{
    [Command(PackageIds.StartDiscussionCommandId)]
    internal sealed class StartDiscussionCommand : BaseCommand<StartDiscussionCommand>
    {
        public StartDiscussionCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var std = await TextDescriptorHelper.GetSelectedTextAsync();
            if (std is null || std.SelectedSpan is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoSelectedCode
                    );
                return;
            }


            //try
            //{
            //    var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            //    dte.ExecuteCommand("OtherContextMenus.inlinediffsettings.Diff.InlineView");

            //    var differenceService = (IVsDifferenceService)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SVsDifferenceService));

            //    var leftPath = @"C:\temp\file1.txt";
            //    var rightPath = @"C:\temp\file2.txt";
            //    var caption = "My Diff";
            //    var tooltip = "Inline diff between two files";

            //    IVsWindowFrame frame = differenceService.OpenComparisonWindow2(
            //        leftFileMoniker: leftPath,
            //        rightFileMoniker: rightPath,
            //        caption: caption,
            //        Tooltip: tooltip,
            //        leftLabel: "left file", // не используется
            //        rightLabel: "right file", // не используется
            //        inlineLabel: "inline label", // не используется
            //        roles: "roles roles", // не используется
            //        grfDiffOptions: (uint)__VSDIFFSERVICEOPTIONS.VSDIFFOPT_DoNotShow
            //    );

            //    frame.Show();

            //    //OtherContextMenus.inlinediffsettings.Diff.InlineView


            //}
            //catch (Exception excp)
            //{
            //    int g = 0;
            //}
            //return;




            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                "Choose agent:"
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
                await FreeAIr.BLogic.ChatOptions.GetDefaultAsync(chosenAgent)
                );
            if (chat is null)
            {
                return;
            }

            chat.ChatContext.AddItem(
                new SolutionItemChatContextItem(
                    std.CreateSelectedIdentifier(),
                    false,
                    AddLineNumbersMode.NotRequired
                    )
                );

            await ChatWindowShower.ShowChatWindowAsync(chat);
        }

    }
}
