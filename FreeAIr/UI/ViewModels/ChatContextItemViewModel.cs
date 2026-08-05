using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using WpfHelpers;
using FreeAIr.Chat.Context;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Displays one item attached to a chat's context (a file, selection, or related code found
    /// automatically) as a chip in the chat context list, showing where it came from.
    /// </summary>
    public sealed class ChatContextItemViewModel : BaseViewModel
    {
        /// <summary>
        /// The underlying context item (file, selection, or related code) this view model displays.
        /// </summary>
        public IChatContextItem ContextItem
        {
            get;
        }

        /// <summary>
        /// The text shown for this context item in the chat's context list.
        /// </summary>
        public string ChatContextDescription => ContextItem.ContextUIDescription;

        /// <summary>
        /// The icon distinguishing an item the software found automatically from one the user
        /// added by hand.
        /// </summary>
        public ImageMoniker Moniker =>
            ContextItem.IsAutoFound
                ? KnownMonikers.Computer
                : KnownMonikers.User
                ;

        /// <summary>
        /// The tooltip explaining whether this context item was added automatically by software
        /// logic or by the user.
        /// </summary>
        public string Tooltip =>
            ContextItem.IsAutoFound
                ? FreeAIr.Resources.Resources.This_item_came_from_software_logic
                : FreeAIr.Resources.Resources.This_item_came_from_user
                ;

        /// <summary>
        /// Wraps a chat context item for display in the chat's context list.
        /// </summary>
        public ChatContextItemViewModel(
            IChatContextItem contextItem
            )
        {
            if (contextItem is null)
            {
                throw new ArgumentNullException(nameof(contextItem));
            }

            ContextItem = contextItem;
        }
    }

}
