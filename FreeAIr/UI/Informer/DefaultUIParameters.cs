using System.Windows.Media;

namespace FreeAIr.UI.Informer
{
    /// <summary>Snapshot of the ambient text foreground, font family and font size an informer popup should inherit from the editor it is displayed over.</summary>
    public sealed class DefaultUIParameters
    {
        /// <summary>Foreground brush to render text with.</summary>
        public Brush ActualForeground
        {
            get;
        }

        /// <summary>Font family to render text with.</summary>
        public FontFamily FontFamily
        {
            get;
        }

        /// <summary>Font size to render text with.</summary>
        public double FontSize
        {
            get;
        }

        /// <summary>Captures the given foreground brush, font family and font size as one immutable parameter set.</summary>
        public DefaultUIParameters(Brush actualForeground, FontFamily fontFamily, double fontSize)
        {
            ActualForeground = actualForeground;
            FontFamily = fontFamily;
            FontSize = fontSize;
        }
    }
}
