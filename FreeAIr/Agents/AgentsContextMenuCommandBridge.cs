using FreeAIr.BLogic.Context.Item;
using FreeAIr.Find;
using FreeAIr.UI.Bridge;
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
        public IContextItemsBuilder ContextItemsBuilder
        {
            get;
            private set;
        }

        protected override int GetMenuID() => PackageIds.AgentsContextMenu;


        public async Task ShowMenuAsync(
            IContextItemsBuilder contextItemsBuilder,
            List<AgentContextMenuItem> menuItems,
            int x,
            int y
            )
        {
            ContextItemsBuilder = contextItemsBuilder;
            try
            {
                await base.ShowAsync(menuItems, x, y);
            }
            finally
            {
                ContextItemsBuilder = null;
            }
        }

        public static void Show(
            IContextItemsBuilder contextItemsBuilder,
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
                            contextItemsBuilder,
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

    public interface IContextItemsBuilder
    {
        System.Threading.Tasks.Task<List<SolutionItemChatContextItem>> CreateContextItemsAsync(
            );
    }
}
