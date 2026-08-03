using System.IO;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// A file in the system temp folder which deletes itself when the scope ends.
    ///
    /// Used wherever something outside the process has to be handed a path instead of a stream: the
    /// audio the recorder writes before it is transcribed, and the two sides of a diff shown in the
    /// Visual Studio difference viewer. The name is always a fresh guid, so two of these never
    /// collide even across several instances of the IDE.
    ///
    /// Note that the file is not created here — only its name is reserved. Disposing a
    /// <see cref="TempFile"/> nobody ever wrote to is fine.
    /// </summary>
    public sealed class TempFile : IDisposable
    {
        /// <summary>The full path, valid whether or not the file has been written yet.</summary>
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

        /// <summary>
        /// A temp file whose extension is fixed. Needed whenever the consumer decides what to do
        /// from the name alone — the audio encoders and the diff viewer's syntax highlighting both
        /// do.
        /// </summary>
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

        /// <summary>
        /// A temp file named after a real one, as `original.suffix.xxxxxxxx.ext`.
        ///
        /// The difference viewer puts the file name in its tab caption, so a guid alone would leave
        /// the user with two unlabelled panes. Keeping the original name and extension makes the
        /// tab readable and the highlighting right; the short guid keeps two comparisons of the
        /// same file apart.
        /// </summary>
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

        /// <summary>
        /// Deletes the file if it was ever written. A file still held open elsewhere throws from
        /// here, which is deliberate: silently leaving temp files behind is how a temp folder fills
        /// up over a working day.
        /// </summary>
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
