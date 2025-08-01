﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeAIr.Helper
{
    public static class FileTypeHelper
    {
        // Список известных текстовых расширений
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

        private static bool IsTextExtension(string filePath)
        {
            var ext = Path.GetExtension(filePath);
            return !string.IsNullOrEmpty(ext) && TextFileExtensions.Contains(ext);
        }

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

    public enum FileTypeEnum
    {
        Empty,
        Text,
        Binary
    }
}
