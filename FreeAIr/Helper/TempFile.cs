using System.IO;

namespace FreeAIr.Helper
{
    public sealed class TempFile : IDisposable
    {
        public string FilePath
        {
            get;
        }

        private TempFile(
            string filePath
            )
        {
            FilePath = filePath;
        }

        public FileStream OpenWrite()
        {
            return File.OpenWrite(FilePath);
        }

        public FileStream OpenRead()
        {
            return File.OpenRead(FilePath);
        }

        public static TempFile CreateWithExtension(string extension)
        {
            var filePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString()
                    + (extension.StartsWith(".") ? string.Empty : ".")
                    + extension
                );
            return new TempFile(filePath);
        }

        public static TempFile Create()
        {
            var filePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString()
                );
            return new TempFile(filePath);
        }

        public static TempFile CreateBasedOfExistingName(
            string fileName,
            string suffix
            )
        {
            var tempFileName = Path.GetFileNameWithoutExtension(fileName)
                + "."
                + suffix
                + "."
                + Guid.NewGuid().ToString().Substring(0, 8)
                + Path.GetExtension(fileName)
                ;

            var filePath = Path.Combine(
                Path.GetTempPath(),
                tempFileName
                );

            return new TempFile(filePath);
        }

        public void Dispose()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        public void WriteAllText(string newItemBody)
        {
            File.WriteAllText(FilePath, newItemBody);
        }

        public string ReadAllText()
        {
            return File.ReadAllText(FilePath);
        }
    }
}
