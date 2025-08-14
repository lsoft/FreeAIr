using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static TempFile Create()
        {
            var filePath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                Guid.NewGuid().ToString()
                );
            return new TempFile(filePath);
        }

        public static TempFile CreateBasedOfExistingName(
            string fileName,
            string suffix
            )
        {
            var filePath = Path.GetFileNameWithoutExtension(fileName)
                + "."
                + suffix
                + "."
                + Path.GetExtension(fileName)
                ;
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
