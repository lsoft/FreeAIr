using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class UriHelper
    {
        public static Uri? TryBuildEndpointUri(string endpoint)
        {
            try
            {
                return new Uri(endpoint);
            }
            catch (Exception excp)
            {
                //todo log
            }

            return null;
        }

    }
}
