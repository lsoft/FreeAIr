using FreeAIr.Helper;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// Tool window hosting the AI chat list - the main chat panel where the user picks, creates
    /// and drives conversations with the configured AI providers, and drags solution files into
    /// a chat's context.
    /// </summary>
    public class ChatListToolWindow : BaseToolWindow<ChatListToolWindow>
    {
        /// <summary>
        /// The view model backing the currently created chat list tool window, kept as a static
        /// so other parts of the extension can reach the active chat list without a UI reference.
        /// </summary>
        public static ChatListViewModel? ChatListViewModel
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the localized caption shown on the tool window's tab.
        /// </summary>
        public override string GetTitle(int toolWindowId) => Resources.Resources.AI_chat_list;

        /// <summary>
        /// Identifies the <see cref="Pane"/> type Visual Studio should host for this tool window.
        /// </summary>
        public override Type PaneType => typeof(Pane);

        /// <summary>
        /// Builds the WPF control for the chat list, resolving its view model through MEF and
        /// publishing it as <see cref="ChatListViewModel"/>.
        /// </summary>
        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var componentModel = await MefHelper.GetComponentModelAsync();

            var viewModel = componentModel.GetService<ChatListViewModel>();
            ChatListToolWindow.ChatListViewModel = viewModel;

            var control = new ChatListToolWindowControl(viewModel);

            return control;
        }

        /// <summary>
        /// The Visual Studio tool window pane hosting the chat list control.
        /// </summary>
        [Guid("ff949254-e51d-40a2-afa6-ad4482f5e54d")]
        internal class Pane : ToolkitToolWindowPane
        {
            /// <summary>
            /// Assigns the pane's toolbar icon.
            /// </summary>
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
