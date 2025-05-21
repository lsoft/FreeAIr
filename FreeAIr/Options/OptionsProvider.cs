using System.Runtime.InteropServices;

namespace FreeAIr
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class ApiPageOptions : BaseOptionPage<ApiPage>
        {
        }

        [ComVisible(true)]
        public class ResponsePageOptions : BaseOptionPage<ResponsePage>
        {
        }

        [ComVisible(true)]
        public class MCPPageOptions : BaseOptionPage<MCPPage>
        {
        }
    }
}
