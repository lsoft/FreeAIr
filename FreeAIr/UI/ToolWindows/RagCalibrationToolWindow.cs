using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// The window where the threshold of the `Use RAG` search is measured, see
    /// <see cref="RagCalibrationViewModel"/>.
    /// </summary>
    public class RagCalibrationToolWindow : BaseToolWindow<RagCalibrationToolWindow>
    {
        public override string GetTitle(int toolWindowId) => FreeAIr.Resources.Resources.RAG_calibration__window_title;

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(
            int toolWindowId,
            CancellationToken cancellationToken
            )
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var viewModel = componentModel.GetService<RagCalibrationViewModel>();

            var control = new RagCalibrationToolWindowControl(
                viewModel
                );

            return control;
        }

        public static async Task ShowPaneAsync(
            )
        {
            var pane = await RagCalibrationToolWindow.ShowAsync();

            var viewModel = (pane.Content as FrameworkElement)?.DataContext as RagCalibrationViewModel;
            if (viewModel is null)
            {
                return;
            }

            //the window is a singleton and may have been opened against another solution, or before
            //the index it is about to calibrate was built
            await viewModel.ReloadAsync();
        }

        [Guid("fdecebd1-551e-472a-a0ab-56654a47ab63")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }
        }
    }
}
