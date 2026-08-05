using FreeAIr.Shared.Dto;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows;

namespace FreeAIr.UI.CodeLens
{
    [Export(typeof(IViewElementFactory))]
    [Name("FreeAIr bind details UI factory")]
    [TypeConversion(@from: typeof(CodeLensUnitInfo), to: typeof(FrameworkElement))]
    [Order]
    /// <summary>
    /// MEF-exported factory that converts a <see cref="CodeLensUnitInfo"/> data model into the WPF
    /// <see cref="CodeLenseUserControl"/> shown as the CodeLens details popup for a method or class.
    /// </summary>
    internal class ViewElementFactory : IViewElementFactory
    {
        /// <summary>
        /// Builds the CodeLens details popup for the given <see cref="CodeLensUnitInfo"/> model,
        /// binding it to a fresh <see cref="CodeLenseUserControlViewModel"/>.
        /// </summary>
        public TView CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            // Should never happen if the service's code is correct, but it's good to be paranoid.
            if (typeof(FrameworkElement) != typeof(TView))
            {
                throw new ArgumentException($"Invalid type conversion. Unsupported {nameof(model)} or {nameof(TView)} type");
            }

            if (model is CodeLensUnitInfo sic)
            {
                var view = new CodeLenseUserControl();

                var viewModel = new CodeLenseUserControlViewModel(
                    sic
                    );

                view.DataContext = viewModel;
                return (view as TView)!;
            }

            return null!;
        }
    }
}
