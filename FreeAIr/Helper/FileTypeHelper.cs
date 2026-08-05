using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Decides whether a file is text or binary, first by its extension and, when that is
    /// inconclusive, by sniffing its content for a BOM or invalid UTF-8. Used to keep binary files
    /// out of chat context and out of the natural language outline scan.
    /// </summary>
    public static class FileTypeHelper
    {
        /// <summary>
        /// The extensions treated as text without inspecting file content.
        /// </summary>
        public static readonly IReadOnlyCollection<string> TextFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".ada", ".adoc", ".ampl", ".asp", ".aspx", ".babelcache", ".babelignore", ".babelrc", ".bas",
            ".bash", ".bat", ".bib", ".c", ".cfg", ".cmd", ".conf", ".cpp", ".cs", ".css", ".csv", ".dart",
            ".dockerfile", ".dockerignore", ".editorconfig", ".env", ".erl", ".erlang", ".eslintcache",
            ".eslintignore", ".eslintrc", ".ex", ".exs", ".fs", ".gemfile", ".gitattributes", ".gitignore",
            ".go", ".groovy", ".h", ".hs", ".html", ".ini", ".java", ".jest", ".jestcache", ".jestignore",
            ".js", ".json", ".jsp", ".jspx", ".kt", ".less", ".lock", ".log", ".lua", ".m", ".mail",
            ".makefile", ".matlab", ".md", ".ml", ".mli", ".mochacache", ".mochaiignore", ".mocharc", ".nfo",
            ".notes", ".npmignore", ".npmrc", ".nt", ".nyccache", ".nycignore", ".nycrc", ".octave", ".php",
            ".pl", ".plist", ".plpgsql", ".prettiercache", ".prettierignore", ".prettierrc", ".pro",
            ".properties", ".props", ".psql", ".py", ".r", ".rb", ".reg", ".rs", ".rss", ".rst", ".sass",
            ".scss", ".sh", ".sql", ".stylelintcache", ".stylelintignore", ".stylelintrc", ".svg", ".swift",
            ".tcl", ".tex", ".todo", ".toml", ".ts", ".txt", ".url", ".vb", ".vbs", ".vhdl", ".xaml", ".xhtml",
            ".xml", ".yaml", ".yarnrc", ".yml", ".zsh"
        };

        /// <summary>
        /// Classifies a file as empty, text or binary: known text extensions are trusted outright,
        /// otherwise the file's leading bytes are checked for null bytes, a byte-order mark, or
        /// valid UTF-8.
        /// </summary>
        public static FileTypeEnum GetFileType(
            this string filePath
            )
        {
            // 1. Проверка расширения
            if (IsTextExtension(filePath))
            {
                return FileTypeEnum.Text;
            }

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // 2. Читаем первые 4 байта для BOM и нулевых байтов
                var header = new byte[4];
                var bytesRead = fs.Read(header, 0, header.Length);
                if (bytesRead == 0)
                {
                    return FileTypeEnum.Empty;
                }

                // 3. Проверка наличия нулевых байтов
                if (HasNullByte(header, bytesRead))
                {
                    return FileTypeEnum.Binary;
                }

                // 4. Проверка BOM
                Encoding detectedEncoding = DetectEncodingFromBom(header);
                if (detectedEncoding != null)
                {
                    return FileTypeEnum.Text; // Файл с BOM — это текстовый файл
                }

                // 5. Если BOM не найден, читаем больше данных и пытаемся декодировать как UTF-8
                fs.Seek(0, SeekOrigin.Begin);
                var buffer = new byte[1024];
                bytesRead = fs.Read(buffer, 0, buffer.Length);

                try
                {
                    var utf8 = Encoding.GetEncoding("utf-8",
                        new EncoderExceptionFallback(),
                        new DecoderExceptionFallback());

                    utf8.GetString(buffer, 0, bytesRead);

                    return FileTypeEnum.Text; // Данные успешно декодированы как UTF-8
                }
                catch (DecoderFallbackException)
                {
                    return FileTypeEnum.Binary; // Некорректные символы — бинарный файл
                }
            }
        }

        /// <summary>
        /// Whether the file's extension is one of <see cref="TextFileExtensions"/>.
        /// </summary>
        private static bool IsTextExtension(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return !string.IsNullOrEmpty(ext) && TextFileExtensions.Contains(ext);
        }

        /// <summary>
        /// Whether the given byte range contains a null byte, a strong signal of binary content.
        /// </summary>
        private static bool HasNullByte(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (buffer[i] == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Detects the text encoding from a leading byte-order mark (UTF-8, UTF-16 or UTF-32, either
        /// endianness), or null when none of the leading bytes match a known BOM.
        /// </summary>
        private static Encoding? DetectEncodingFromBom(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                return Encoding.UTF8;
            }

            if (data.Length >= 2)
            {
                if (data[0] == 0xFF && data[1] == 0xFE)
                {
                    // UTF-16 LE или UTF-32 LE
                    if (data.Length >= 4 && data[2] == 0x00 && data[3] == 0x00)
                    {
                        return Encoding.UTF32;
                    }
                    else
                    {
                        return Encoding.Unicode;
                    }
                }

                if (data[0] == 0xFE && data[1] == 0xFF)
                {
                    // UTF-16 BE или UTF-32 BE
                    if (data.Length >= 4 && data[2] == 0x00 && data[3] == 0x00)
                    {
                        return Encoding.UTF32;
                    }
                    else
                    {
                        return Encoding.BigEndianUnicode;
                    }
                }
            }

            return null; // BOM не найден
        }
    }

    /// <summary>
    /// The classification <see cref="FileTypeHelper.GetFileType"/> assigns to a file.
    /// </summary>
    public enum FileTypeEnum
    {
        /// <summary>
        /// The file has no content.
        /// </summary>
        Empty,
        /// <summary>
        /// The file is plain text.
        /// </summary>
        Text,
        /// <summary>
        /// The file is not text and should be excluded from text-based processing.
        /// </summary>
        Binary
    }
}
