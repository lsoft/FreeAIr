using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.ToolWindows
{
    public class ChatListToolWindow : BaseToolWindow<ChatListToolWindow>
    {
        public static ChatListViewModel? ChatListViewModel
        {
            get;
            private set;
        }

        public override string GetTitle(int toolWindowId) => Resources.Resources.AI_chat_list;

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var viewModel = componentModel.GetService<ChatListViewModel>();
            ChatListToolWindow.ChatListViewModel = viewModel;

            var control = new ChatListToolWindowControl(viewModel);

            return control;
        }

        [Guid("ff949254-e51d-40a2-afa6-ad4482f5e54d")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
