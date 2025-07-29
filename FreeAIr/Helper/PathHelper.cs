using System.IO;

namespace FreeAIr.Helper
{
    public static class PathHelper
    {
        public static string MakeRelativeAgainst(
            this string filePath,
            string referencePath
            )
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));
            }

            if (string.IsNullOrEmpty(referencePath))
            {
                throw new ArgumentException($"'{nameof(referencePath)}' cannot be null or empty.", nameof(referencePath));
            }

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
