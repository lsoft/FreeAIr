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
        /// <summary>
        /// Returns the localized caption shown on the tool window's tab.
        /// </summary>
        public override string GetTitle(int toolWindowId) => FreeAIr.Resources.Resources.RAG_calibration__window_title;

        /// <summary>
        /// Identifies the <see cref="Pane"/> type Visual Studio should host for this tool window.
        /// </summary>
        public override Type PaneType => typeof(Pane);

        /// <summary>
        /// Builds the WPF control for the RAG calibration window, resolving its view model through MEF.
        /// </summary>
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

        /// <summary>
        /// Opens (or activates) the RAG calibration window and reloads its view model, since the
        /// window is a singleton that may already be showing data from a different solution or
        /// from before the index it calibrates was (re)built.
        /// </summary>
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

        /// <summary>
        /// The Visual Studio tool window pane hosting the RAG calibration control.
        /// </summary>
        [Guid("fdecebd1-551e-472a-a0ab-56654a47ab63")]
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
