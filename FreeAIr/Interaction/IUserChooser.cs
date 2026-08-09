using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using System.Collections.Generic;

namespace FreeAIr.Interaction
{
    /// <summary>
    /// Asks the user to pick a support action or an agent, the two questions every FreeAIr command
    /// starts with. The MEF implementation shows the Visual Studio context menus; behind this
    /// interface the caller only knows that a choice was made, or that there was nothing to choose
    /// from and it should give up quietly.
    /// </summary>
    public interface IUserChooser
    {
        /// <summary>
        /// Offers the support actions configured for the given scope. Returns null when the user
        /// dismissed the menu or no action applies; picks silently when only one does.
        /// </summary>
        System.Threading.Tasks.Task<SupportActionJson?> ChooseSupportActionAsync(
            string title,
            SupportScopeEnum scope
            );

        /// <summary>
        /// Offers the agents that carry an authentication token, preferring the one named by
        /// <paramref name="preferredAgentName"/> without asking. Returns null when the user
        /// dismissed the menu or no agent is configured.
        /// </summary>
        System.Threading.Tasks.Task<AgentJson?> ChooseAgentWithTokenAsync(
            string title,
            string? preferredAgentName = null
            );

        /// <summary>
        /// Offers every configured agent, whether it carries a token or not. This is the picker for
        /// an embedding model: those normally run on a local server which wants no token, so the
        /// token-filtered list above would hide exactly the agent wanted here.
        /// </summary>
        System.Threading.Tasks.Task<AgentJson?> ChooseAnyAgentAsync(
            string title,
            string? preferredAgentName = null
            );

        /// <summary>
        /// Offers an arbitrary list of labelled values - the scope of a natural language search,
        /// and whatever else a caller outside the UI has to ask about. Returns null when the user
        /// dismissed the menu.
        /// </summary>
        System.Threading.Tasks.Task<T?> ChooseOneOfAsync<T>(
            string title,
            List<(string Title, T Value)> variants
            )
            where T : class;
    }
}
