using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MarkdownParser.Tests
{
    /// <summary>
    /// A real 2x2 png on disk, so the loading path of an image part can be tested against something
    /// WPF genuinely decodes rather than against a mock. Written to the temp directory and removed
    /// on dispose, best effort - <see cref="BitmapImage"/> may still hold the file open.
    /// </summary>
    internal sealed class TempPngFile : IDisposable
    {
        /// <summary>The full path of the written file, the form an absolute image link takes.</summary>
        public string FilePath
        {
            get;
        }

        /// <summary>Encodes a 2x2 opaque bitmap as a png in a fresh temp file.</summary>
        public TempPngFile()
        {
            FilePath = Path.Combine(
                Path.GetTempPath(),
                "freeair-test-" + Guid.NewGuid().ToString("N") + ".png"
                );

            var pixels = new byte[2 * 2 * 4];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = 0xFF;
            }

            var source = BitmapSource.Create(
                2,
                2,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                pixels,
                2 * 4
                );

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var stream = File.Create(FilePath))
            {
                encoder.Save(stream);
            }
        }

        /// <summary>Deletes the file, ignoring the lock a still-alive bitmap may hold on it.</summary>
        public void Dispose()
        {
            try
            {
                File.Delete(FilePath);
            }
            catch (IOException)
            {
                //the decoded bitmap may still have the file open; the temp directory can have it
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
