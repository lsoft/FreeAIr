using System;
using System.Collections.Generic;
using System.Text;

namespace FreeAIr.Grep
{
    /// <summary>
    /// Turning the bytes of a file into lines a search can look at, and refusing the files where
    /// that makes no sense.
    ///
    /// The point is not to be right about every encoding on earth - it is to never hand a pile of
    /// binary to the model, and to never fail on a file which is merely saved in something older
    /// than UTF-8.
    /// </summary>
    public static class TextFile
    {
        /// <summary>
        /// How far into the file the binary sniffing looks. A text file which is binary after the
        /// first few kilobytes does not exist in a source tree.
        /// </summary>
        public const int SniffLength = 8000;

        private static readonly HashSet<string> _binaryExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".dll", ".exe", ".pdb", ".lib", ".obj", ".so", ".dylib", ".class", ".jar", ".wasm",
            ".zip", ".7z", ".rar", ".gz", ".tar", ".nupkg", ".vsix", ".cab", ".msi",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".tif", ".tiff", ".psd", ".webp",
            ".mp3", ".mp4", ".wav", ".avi", ".mov", ".mkv", ".ogg", ".flac",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".ttf", ".otf", ".woff", ".woff2", ".eot",
            ".snk", ".pfx", ".cache", ".bin", ".dat", ".db", ".suo", ".ipch", ".idb", ".res"
        };

        /// <summary>
        /// Whether the extension alone is enough to skip the file. Cheaper than reading it, and it
        /// keeps the scan away from the megabytes a source tree is full of.
        /// </summary>
        public static bool HasBinaryExtension(
            string path
            )
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var dot = path.LastIndexOf('.');
            if (dot < 0)
            {
                return false;
            }

            return _binaryExtensions.Contains(path.Substring(dot));
        }

        /// <summary>
        /// Decodes the bytes of a file, or reports that there is no text in them.
        ///
        /// A byte order mark is believed. Without one the bytes have to be valid UTF-8, which every
        /// ASCII file also is; anything else is decoded leniently, so a file in a single byte
        /// codepage still matches on its ASCII - which is where identifiers live.
        /// </summary>
        public static bool TryDecode(
            byte[] bytes,
            out string text
            )
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            text = string.Empty;

            if (bytes.Length == 0)
            {
                return true;
            }

            var bomEncoding = DetectByteOrderMark(bytes, out var bomLength);
            if (bomEncoding is not null)
            {
                try
                {
                    text = bomEncoding.GetString(bytes, bomLength, bytes.Length - bomLength);
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            if (LooksBinary(bytes))
            {
                return false;
            }

            try
            {
                //strict: a file which is not UTF-8 has to be told from one which is, and the
                //replacement characters of a lenient decoder would hide exactly that
                text = new UTF8Encoding(false, true).GetString(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                text = new UTF8Encoding(false, false).GetString(bytes);
                return true;
            }
        }

        /// <summary>
        /// Splits into lines, understanding all three line endings and keeping none of them. The
        /// index of a line plus one is its number, which is what the result of a search reports.
        /// </summary>
        public static List<string> SplitLines(
            string text
            )
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var lines = new List<string>();

            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c != '\r' && c != '\n')
                {
                    continue;
                }

                lines.Add(text.Substring(start, i - start));

                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }

            if (start < text.Length)
            {
                lines.Add(text.Substring(start));
            }

            return lines;
        }

        private static Encoding? DetectByteOrderMark(
            byte[] bytes,
            out int bomLength
            )
        {
            if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
            {
                bomLength = 4;
                return new UTF32Encoding(false, true);
            }

            if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
            {
                bomLength = 4;
                return new UTF32Encoding(true, true);
            }

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                bomLength = 3;
                return new UTF8Encoding(false, false);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                bomLength = 2;
                return new UnicodeEncoding(false, true);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                bomLength = 2;
                return new UnicodeEncoding(true, true);
            }

            bomLength = 0;
            return null;
        }

        private static bool LooksBinary(
            byte[] bytes
            )
        {
            var length = Math.Min(bytes.Length, SniffLength);
            for (var i = 0; i < length; i++)
            {
                if (bytes[i] == 0x00)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
