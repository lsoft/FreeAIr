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
    /// Tool window showing the natural language outline (NLO) tree built from the current
    /// solution's comments, letting the user browse and inspect the data that feeds the RAG
    /// search index.
    /// </summary>
    public class NaturalLanguageOutlinesToolWindow : BaseToolWindow<NaturalLanguageOutlinesToolWindow>
    {
        /// <summary>
        /// Returns the localized caption shown on the tool window's tab.
        /// </summary>
        public override string GetTitle(int toolWindowId) => FreeAIr.Resources.Resources.FreeAIr_Natural_Language_Outlines;

        /// <summary>
        /// Identifies the <see cref="Pane"/> type Visual Studio should host for this tool window.
        /// </summary>
        public override Type PaneType => typeof(Pane);

        /// <summary>
        /// Builds the WPF control for the outline tree view, resolving its view model through MEF.
        /// </summary>
        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var viewModel = componentModel.GetService<NaturalLanguageOutlinesViewModel>();

            var control = new NaturalLanguageOutlinesToolWindowControl(
                viewModel
                );

            return control;
        }

        /// <summary>
        /// The Visual Studio tool window pane hosting the outline tree control.
        /// </summary>
        [Guid("bff6aa5d-7e90-4068-b4e8-fb58eb759c44")]
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
