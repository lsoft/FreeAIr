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
    /// Tool window that lets the user browse and pick an OpenRouter model to use for chats,
    /// showing what OpenRouter currently offers.
    /// </summary>
    public class ChooseModelToolWindow : BaseToolWindow<ChooseModelToolWindow>
    {
        /// <summary>
        /// Returns the localized caption shown on the tool window's tab.
        /// </summary>
        public override string GetTitle(int toolWindowId) => FreeAIr.Resources.Resources.Choose_OpenRouter_model;

        /// <summary>
        /// Identifies the <see cref="Pane"/> type Visual Studio should host for this tool window.
        /// </summary>
        public override Type PaneType => typeof(Pane);

        /// <summary>
        /// Builds the WPF control for the model picker, resolving its view model through MEF.
        /// </summary>
        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var viewModel = componentModel.GetService<ChooseModelViewModel>();

            var control = new ChooseModelToolWindowControl(viewModel);

            return control;
        }

        /// <summary>
        /// The Visual Studio tool window pane hosting the model picker control.
        /// </summary>
        [Guid("e297c066-64d0-40d7-88d4-52afe21989de")]
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
