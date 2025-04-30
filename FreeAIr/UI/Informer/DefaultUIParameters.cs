using System.Windows.Media;

namespace SauronEye.UI.Informer
{
    public sealed class DefaultUIParameters
    {
        public Brush ActualForeground
        {
            get;
        }
        
        public FontFamily FontFamily
        {
            get;
        }

        public double FontSize
        {
            get;
        }

        public DefaultUIParameters(Brush actualForeground, FontFamily fontFamily, double fontSize)
        {
            ActualForeground = actualForeground;
            FontFamily = fontFamily;
            FontSize = fontSize;
        }
    }
}
