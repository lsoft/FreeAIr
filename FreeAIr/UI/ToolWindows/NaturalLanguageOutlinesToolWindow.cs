using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.ToolWindows
{
    public class NaturalLanguageOutlinesToolWindow : BaseToolWindow<NaturalLanguageOutlinesToolWindow>
    {
        public override string GetTitle(int toolWindowId) => FreeAIr.Resources.Resources.FreeAIr_Natural_Language_Outlines;

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var viewModel = componentModel.GetService<NaturalLanguageOutlinesViewModel>();

            var control = new NaturalLanguageOutlinesToolWindowControl(
                viewModel
                );

            return control;
        }

        [Guid("bff6aa5d-7e90-4068-b4e8-fb58eb759c44")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
