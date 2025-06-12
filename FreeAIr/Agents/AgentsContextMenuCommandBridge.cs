using FreeAIr.BLogic.Context.Item;
using FreeAIr.Find;
using FreeAIr.Helper;
using FreeAIr.UI.Bridge;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Agents
{
    public sealed class AgentsContextMenuCommandBridge : MenuCommandBridge<AgentContextMenuItem>
    {
        public CommandProcessor CommandProcessor
        {
            get;
            private set;
        }

        protected override int GetMenuID() => PackageIds.AgentsContextMenu;


        public async Task ShowMenuAsync(
            CommandProcessor commandProcessor,
            List<AgentContextMenuItem> menuItems,
            int x,
            int y
            )
        {
            CommandProcessor = commandProcessor;
            try
            {
                await base.ShowAsync(menuItems, x, y);
            }
            finally
            {
                CommandProcessor = null;
            }
        }

        public static void Show(
            CommandProcessor commandProcessor,
            AgentCollection agentCollection
            )
        {
            var menuItems = agentCollection.Agents.ConvertAll(a =>
                new AgentContextMenuItem(
                    a
                    )
                );

            ThreadHelper.JoinableTaskFactory.Run(
                async () =>
                {
                    try
                    {
                        var bridge = await VS.GetRequiredServiceAsync<AgentsContextMenuCommandBridge, AgentsContextMenuCommandBridge>();
                        var point = new System.Windows.Point(
                            System.Windows.Forms.Cursor.Position.X,
                            System.Windows.Forms.Cursor.Position.Y
                            );
                        await bridge.ShowMenuAsync(
                            commandProcessor,
                            menuItems,
                            (int)point.X,
                            (int)point.Y
                            );
                    }
                    catch (Exception excp)
                    {
                        int g = 0;
                        //todo log
                    }
                });
        }
    }

    public sealed class AgentContextMenuItem
    {
        public string Name
        {
            get;
        }

        public Agent Agent
        {
            get;
        }

        public AgentContextMenuItem(
            Agent agent
            )
        {
            Name = agent.Name;
            Agent = agent;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public abstract class CommandProcessor
    {
        public virtual async System.Threading.Tasks.Task ProcessAsync(
            Agent agent
            )
        {
            var contextItems = await CreateContextItemsAsync();
            if (contextItems is null || contextItems.Count == 0)
            {
                return;
            }

            contextItems = contextItems.FindAll(i => FileTypeHelper.GetFileType(i.SelectedIdentifier.FilePath) == FileTypeEnum.Text);

            await ShowAsync(agent, contextItems);
        }

        protected abstract System.Threading.Tasks.Task<List<SolutionItemChatContextItem>> CreateContextItemsAsync(
            );


        private static async Task ShowAsync(
            Agent agent,
            List<SolutionItemChatContextItem> contextItems
            )
        {
            var pane = await NaturalLanguageOutlinesToolWindow.ShowAsync();
            var toolWindow = pane.Content as NaturalLanguageOutlinesToolWindowControl;
            var viewModel = toolWindow.DataContext as NaturalLanguageOutlinesViewModel;
            viewModel.SetNewChatAsync(
                agent,
                contextItems
                )
                .FileAndForget(nameof(NaturalLanguageOutlinesViewModel.SetNewChatAsync));
        }
    }
}
