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
    /// Tool window listing the ranked hits returned by a natural language (RAG) search over the
    /// solution's outline index, letting the user jump from a result straight to its code.
    /// </summary>
    public class NaturalLanguageResultsToolWindow : BaseToolWindow<NaturalLanguageResultsToolWindow>
    {
        /// <summary>
        /// Returns the localized caption shown on the tool window's tab.
        /// </summary>
        public override string GetTitle(int toolWindowId) => FreeAIr.Resources.Resources.Natural_language_search_results;

        /// <summary>
        /// Identifies the <see cref="Pane"/> type Visual Studio should host for this tool window.
        /// </summary>
        public override Type PaneType => typeof(Pane);

        /// <summary>
        /// Builds the WPF control for the search results list, resolving its view model through MEF.
        /// </summary>
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

        /// <summary>
        /// The Visual Studio tool window pane hosting the search results control.
        /// </summary>
        [Guid("3b647c48-fb8b-4aa5-8438-41b64d0ade02")]
        internal class Pane : ToolkitToolWindowPane
        {
            /// <summary>
            /// Assigns the pane's toolbar icon.
            /// </summary>
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.CommentCode;
            }
        }
    }
}
