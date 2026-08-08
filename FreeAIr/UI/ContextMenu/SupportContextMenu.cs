using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    /// <summary>
    /// Shows the Visual Studio context menu used to pick a configured support action (a
    /// user-defined prompt/command) applicable to a given scope, such as code review or record
    /// post-processing.
    /// </summary>
    public static class SupportContextMenu
    {
        /// <summary>
        /// Lists the support actions configured for <paramref name="scope"/> and shows the picker
        /// menu, auto-selecting when only one action matches.
        /// </summary>
        public static async Task<SupportActionJson?> ChooseSupportAsync(
            string title,
            SupportScopeEnum scope
            )
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            var filteredEntities = await FreeAIrOptions.DeserializeSupportActionsAsync(
                e => e.Scopes.Contains(scope)
                );
            if (filteredEntities.Count == 0)
            {
                return null;
            }
            if (filteredEntities.Count == 1)
            {
                return filteredEntities[0];
            }

            var chosenSupportAction = await VisualStudioContextMenuCommandBridge.ShowAsync<SupportActionJson>(
                title,
                filteredEntities
                    .ConvertAll(e => (e.Name, e as object))
                );

            return chosenSupportAction;
        }

    }
}
