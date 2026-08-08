using System.IO;
using System.Reflection;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Resolves relative paths against the FreeAIr extension assembly's own directory, since a VSIX
    /// has no single well-known "current directory" to resolve bundled files against.
    /// </summary>
    public static class ExtensionPath
    {
        /// <summary>
        /// Combines the extension assembly's directory with the given relative folder path.
        /// </summary>
        public static string GetWorkingDirectory(
            this string folderPath
            )
        {
            if (folderPath is null)
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            if (Path.IsPathRooted(folderPath))
            {
                throw new InvalidOperationException("Relative path should not be rooted!");
            }


            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var di = fi.Directory.FullName;

            var result = Path.Combine(
                di,
                folderPath
                );

            return result;
        }

        /// <summary>
        /// Resolves a file name to an absolute path: returned unchanged when already rooted,
        /// otherwise combined with the extension assembly's own directory.
        /// </summary>
        public static string GetFullPathToFile(
            this string fileName
            )
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (Path.IsPathRooted(fileName))
            {
                return
                    fileName;
            }

            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var di = fi.Directory.FullName;

            var result = Path.Combine(
                di,
                fileName
                );

            return result;
        }
    }
}
