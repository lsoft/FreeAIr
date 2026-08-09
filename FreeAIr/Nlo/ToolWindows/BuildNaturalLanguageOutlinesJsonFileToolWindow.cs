using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.UI
{
    /// <summary>
    /// Tool window that drives the export of the natural language outline (NLO) index to a JSON
    /// file, used to review or share the RAG search data outside of Visual Studio.
    /// </summary>
    public class BuildNaturalLanguageOutlinesJsonFileToolWindow : BaseToolWindow<BuildNaturalLanguageOutlinesJsonFileToolWindow>
    {
        /// <summary>
        /// Returns the localized caption shown on the tool window's tab.
        /// </summary>
        public override string GetTitle(int toolWindowId) => Resources.Resources.Build_Natural_Language_Outlines_Json_File;

        /// <summary>
        /// Identifies the <see cref="Pane"/> type Visual Studio should host for this tool window.
        /// </summary>
        public override Type PaneType => typeof(Pane);

        /// <summary>
        /// Builds the WPF control for this tool window, resolving its view model through MEF and
        /// refreshing the page contents before display.
        /// </summary>
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

        /// <summary>
        /// Opens (or activates) the tool window and sets whether the JSON export should rebuild
        /// the whole natural language outline index from scratch or reuse cached nodes.
        /// </summary>
        public static async Task ShowPaneAsync(
            bool completeRebuild
            )
        {
            var pane = await BuildNaturalLanguageOutlinesJsonFileToolWindow.ShowAsync();
            var viewModel = (pane.Content as FrameworkElement).DataContext as BuildNaturalLanguageOutlinesJsonFileToolViewModel;
            viewModel.CompleteRebuild = completeRebuild;
        }

        /// <summary>
        /// The Visual Studio tool window pane hosting the JSON export control.
        /// </summary>
        [Guid("a1f5bea3-1dc6-4031-8326-61429827cbd1")]
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
