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
    internal class ViewElementFactory : IViewElementFactory
    {
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
