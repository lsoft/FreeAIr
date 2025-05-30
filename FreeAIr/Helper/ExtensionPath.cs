using System.IO;
using System.Reflection;

namespace FreeAIr.Helper
{
    public static class ExtensionPath
    {
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
                throw new InvalidOperationException("Relative path sound not be rooted!");
            }


            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var di = fi.Directory.FullName;

            var result = Path.Combine(
                di,
                folderPath
                );

            return result;
        }

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
