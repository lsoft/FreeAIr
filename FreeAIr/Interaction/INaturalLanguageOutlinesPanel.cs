using FreeAIr.Chat.Context.Item;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using System.Collections.Generic;

namespace FreeAIr.Interaction
{
    /// <summary>
    /// Opens the "Natural language outlines" panel for a set of solution items, so that a feature
    /// which merely knows which files to re-outline - the git command over the pending diff, for
    /// one - does not have to know the panel, its view model or its tool window.
    /// </summary>
    public interface INaturalLanguageOutlinesPanel
    {
        /// <summary>
        /// Shows the panel and starts a chat which asks the agent to add the summaries, driven by
        /// the given support action, for exactly the items passed in.
        /// </summary>
        Task ShowAsync(
            SupportActionJson action,
            AgentJson agent,
            List<SolutionItemChatContextItem> chosenSolutionItems
            );
    }
}
