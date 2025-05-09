using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class PathHelper
    {
        public static string MakeRelativeAgainst(
            this string filePath,
            string referencePath
            )
        {
            var fileUri = new Uri(filePath);
            var referenceUri = new Uri(referencePath);
            return Uri.UnescapeDataString(referenceUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        public static string GetFullPath(
            string rootPath,
            string somePath
            )
        {
            var rooted = Path.IsPathRooted(somePath);

            var result = rooted
                ? somePath
                : Path.Combine(rootPath, somePath);

            //на случай путей типа ../../somefile.cs
            result = Path.GetFullPath(result);

            return result;
        }
    }
}
