using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.ToolWindows
{
    public class NaturalLanguageResultsToolWindow : BaseToolWindow<NaturalLanguageResultsToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "Natural language Search results";

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(
            int toolWindowId,
            CancellationToken cancellationToken
            )
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var viewModel = componentModel.GetService<NaturalLanguageResultsViewModel>();

            var control = new NaturalLanguageResultsToolWindowControl(viewModel);

            return control;
        }

        [Guid("3b647c48-fb8b-4aa5-8438-41b64d0ade02")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ShowResultsPane;
            }
        }
    }
}
