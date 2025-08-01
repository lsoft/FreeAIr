using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class DisposeHelper
    {
        public static void SafelyDispose(
            this IDisposable d
            )
        {
            try
            {
                d.Dispose();
            }
            catch (Exception excp)
            {
                //todo log
            }
        }
    }
}
