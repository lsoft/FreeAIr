using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class ResourceHelper
    {
        public static string GetLocalizedResourceByName(
            this string resourceName
            )
        {
            return FreeAIr.Resources.Resources.ResourceManager.GetString(
                resourceName,
                ResponsePage.GetAnswerCulture()
                );
        }
    }
}
