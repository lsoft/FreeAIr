using FreeAIr.Interaction;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace FreeAIr.UI.Interaction
{
    /// <summary>
    /// Answers <see cref="IUserChooser"/> with the Visual Studio context menus FreeAIr builds
    /// through <see cref="VisualStudioContextMenuCommandBridge"/>. Commands that live in the UI
    /// assembly still call those menus directly; this exists for the code that must not know them.
    /// </summary>
    [Export(typeof(IUserChooser))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class UserChooser : IUserChooser
    {
        /// <inheritdoc/>
        public async System.Threading.Tasks.Task<SupportActionJson?> ChooseSupportActionAsync(
            string title,
            SupportScopeEnum scope
            )
        {
            return await SupportContextMenu.ChooseSupportAsync(
                title,
                scope
                );
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task<AgentJson?> ChooseAgentWithTokenAsync(
            string title,
            string? preferredAgentName = null
            )
        {
            return await AgentContextMenu.ChooseAgentWithTokenAsync(
                title,
                preferredAgentName
                );
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task<AgentJson?> ChooseAnyAgentAsync(
            string title,
            string? preferredAgentName = null
            )
        {
            return await AgentContextMenu.ChooseAnyAgentAsync(
                title,
                preferredAgentName
                );
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task<T?> ChooseOneOfAsync<T>(
            string title,
            List<(string Title, T Value)> variants
            )
            where T : class
        {
            //the bridge takes the value as object and casts it back, so the labelled pairs are
            //flattened here and nowhere else
            return await VisualStudioContextMenuCommandBridge.ShowAsync<T>(
                title,
                variants.ConvertAll(v => (v.Title, (object)v.Value))
                );
        }
    }
}
