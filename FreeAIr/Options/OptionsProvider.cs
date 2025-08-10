using System.Runtime.InteropServices;

namespace FreeAIr
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class UIPageOptions : BaseOptionPage<UIPage>
        {
        }

        [ComVisible(true)]
        public class FontSizePageOptions : BaseOptionPage<FontSizePage>
        {
        }

        [ComVisible(true)]
        public class InternalPageOptions : BaseOptionPage<InternalPage>
        {
        }
    }
}
