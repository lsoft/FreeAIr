using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI
{
    public class BuildNaturalLanguageOutlinesJsonFileToolWindow : BaseToolWindow<BuildNaturalLanguageOutlinesJsonFileToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "Build Natural Language Outlines Json File";

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(
            int toolWindowId,
            CancellationToken cancellationToken
            )
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var viewModel = componentModel.GetService<BuildNaturalLanguageOutlinesJsonFileToolViewModel>();

            await viewModel.UpdatePageAsync();

            var control = new BuildNaturalLanguageOutlinesJsonFileToolWindowControl(
                viewModel
                );

            return control;
        }

        public static async Task ShowPaneAsync(
            bool completeRebuild
            )
        {
            var pane = await BuildNaturalLanguageOutlinesJsonFileToolWindow.ShowAsync();
            var viewModel = (pane.Content as FrameworkElement).DataContext as BuildNaturalLanguageOutlinesJsonFileToolViewModel;
            viewModel.CompleteRebuild = completeRebuild;
        }

        [Guid("a1f5bea3-1dc6-4031-8326-61429827cbd1")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
