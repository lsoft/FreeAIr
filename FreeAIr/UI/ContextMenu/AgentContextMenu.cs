using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    public sealed class AgentContextMenu
    {
        public static async Task<AgentJson?> ChooseAgentWithTokenAsync(
            string title
            )
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            var chosenAgent = await VisualStudioContextMenuCommandBridge.ShowAsync<AgentJson>(
                title,
                (await FreeAIrOptions.DeserializeAgentCollectionAsync())
                    .Agents
                    .FindAll(a => !string.IsNullOrEmpty(a.Technical.Token))
                    .ConvertAll(a => (a.Name, a as object))
                );

            return chosenAgent;
        }

    }
}
