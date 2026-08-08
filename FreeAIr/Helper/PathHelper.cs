using System.IO;

namespace FreeAIr.Helper
{
    /// <summary>
    /// File path helpers used across FreeAIr to turn absolute paths into solution-relative ones
    /// (for display and for prompts sent to the model) and to resolve paths that may be relative
    /// to a project or solution root.
    /// </summary>
    public static class PathHelper
    {
        /// <summary>
        /// Computes the path of <paramref name="filePath"/> relative to <paramref name="referencePath"/>,
        /// using platform directory separators. Used to show file paths compactly relative to the
        /// solution or project root instead of as full absolute paths.
        /// </summary>
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

        /// <summary>
        /// Resolves <paramref name="somePath"/> to an absolute path, combining it with
        /// <paramref name="rootPath"/> when it is relative (including paths with "../" segments)
        /// and leaving it unchanged when it is already rooted.
        /// </summary>
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
