using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    public static class SupportContextMenu
    {
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
