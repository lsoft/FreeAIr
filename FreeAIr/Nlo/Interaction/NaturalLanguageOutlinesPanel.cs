using FreeAIr.Chat.Context.Item;
using FreeAIr.Interaction;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FreeAIr.Nlo.Interaction
{
    /// <summary>
    /// Answers <see cref="INaturalLanguageOutlinesPanel"/> with the tool window the NLO feature
    /// owns. The only export the feature publishes to the rest of the extension - everything else
    /// inside Nlo/ talks to its view models directly.
    /// </summary>
    [Export(typeof(INaturalLanguageOutlinesPanel))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class NaturalLanguageOutlinesPanel : INaturalLanguageOutlinesPanel
    {
        /// <inheritdoc/>
        public async Task ShowAsync(
            SupportActionJson action,
            AgentJson agent,
            List<SolutionItemChatContextItem> chosenSolutionItems
            )
        {
            await NaturalLanguageOutlinesViewModel.ShowPanelAsync(
                action,
                agent,
                chosenSolutionItems
                );
        }
    }
}
