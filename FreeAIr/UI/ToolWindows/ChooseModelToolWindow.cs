using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.ToolWindows
{
    public class ChooseModelToolWindow : BaseToolWindow<ChooseModelToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "Choose OpenRouter model";

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var viewModel = componentModel.GetService<ChooseModelViewModel>();

            var control = new ChooseModelToolWindowControl(viewModel);

            return control;
        }

        [Guid("e297c066-64d0-40d7-88d4-52afe21989de")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
