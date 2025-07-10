using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class CommentHelper
    {
        /// <summary>
        /// Словарь, связывающий расширения файлов с шаблонами однострочных комментариев.
        /// 
        /// Логика разделения:
        /// - **C-style (//)**: Официальные языки программирования, где `//` является стандартным синтаксисом для комментариев.
        /// - **Special cases**: Форматы данных, конфигурационные файлы и препроцессоры, где `//` используется неформально или условно.
        /// - **Hash (#)**: Скриптовые языки (Bash, Python) и конфиги, использующие решётку для комментариев.
        /// - **Double hyphen (--)**: Языки запросов (SQL) и функциональные языки (Haskell), использующие двойной дефис.
        /// - **Single quote (')**: Языки вроде VB.NET, где комментарий начинается с апострофа.
        /// - **REM**: Старые скрипты Windows (BAT/CMD), где комментарии начинаются с ключевого слова `REM`.
        /// - **XML-style**: Форматы разметки (XML, HTML), где комментарии обрамляются `<!-- -->`.
        /// 
        /// Примечание: Для некоторых форматов (например, JSON, CSS) `//` не является официальным синтаксисом, 
        /// но часто используется в реальных проектах для удобства.
        /// </summary>
        private static readonly Dictionary<string, string> _commentTemplateMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // C-style (//)
            // Языки программирования, где `//` является ОФИЦИАЛЬНЫМ синтаксисом для комментариев.
            { "cs", "// * {0}" }, { "java", "// * {0}" }, { "js", "// * {0}" }, { "ts", "// * {0}" },
            { "c", "// * {0}" }, { "cpp", "// * {0}" }, { "h", "// * {0}" },
            { "swift", "// * {0}" }, { "go", "// * {0}" }, { "rs", "// * {0}" }, { "kt", "// * {0}" },
            { "dart", "// * {0}" }, { "php", "// * {0}" }, { "groovy", "// * {0}" }, { "fs", "// * {0}" },
            { "ex", "// * {0}" }, { "exs", "// * {0}" },

            // Hash (#)
            // Скриптовые языки и конфигурационные файлы, где комментарии начинаются с решётки `#`.
            { "py", "# * {0}" }, { "rb", "# * {0}" }, { "pl", "# * {0}" }, { "sh", "# * {0}" },
            { "bash", "# * {0}" }, { "zsh", "# * {0}" }, { "tcl", "# * {0}" }, { "r", "# * {0}" },
            { "gemfile", "# * {0}" }, { "yml", "# * {0}" }, { "yaml", "# * {0}" }, { "csv", "# * {0}" },
            { "ini", "# * {0}" }, { "cfg", "# * {0}" }, { "reg", "# * {0}" }, { "txt", "# * {0}" },
            { "toml", "# * {0}" }, { "dockerfile", "# * {0}" }, { "makefile", "# * {0}" },
            { "gitignore", "# * {0}" }, { "npmignore", "# * {0}" }, { "lock", "# * {0}" },
            { "env", "# * {0}" }, { "editorconfig", "# * {0}" }, { "gitattributes", "# * {0}" },
            { "dockerignore", "# * {0}" }, { "npmrc", "# * {0}" }, { "yarnrc", "# * {0}" },
            { "eslintrc", "# * {0}" }, { "prettierrc", "# * {0}" }, { "stylelintrc", "# * {0}" },
            { "babelrc", "# * {0}" }, { "jest", "# * {0}" }, { "mocharc", "# * {0}" }, { "nycrc", "# * {0}" },
            { "eslintignore", "# * {0}" }, { "prettierignore", "# * {0}" }, { "stylelintignore", "# * {0}" },
            { "babelignore", "# * {0}" }, { "jestignore", "# * {0}" }, { "mochaiignore", "# * {0}" },
            { "nycignore", "# * {0}" }, { "eslintcache", "# * {0}" }, { "prettiercache", "# * {0}" },
            { "stylelintcache", "# * {0}" }, { "babelcache", "# * {0}" }, { "jestcache", "# * {0}" },
            { "mochacache", "# * {0}" }, { "nyccache", "# * {0}" }, { "log", "# * {0}" },

            // Double hyphen (--)
            // Языки, где комментарии начинаются с двойного дефиса `--` (например, SQL, Lua).
            { "sql", "-- * {0}" }, { "lua", "-- * {0}" }, { "hs", "-- * {0}" }, { "ml", "-- * {0}" },
            { "mli", "-- * {0}" }, { "ada", "-- * {0}" }, { "vhdl", "-- * {0}" }, { "plpgsql", "-- * {0}" },
            { "psql", "-- * {0}" }, { "erl", "-- * {0}" }, { "erlang", "-- * {0}" }, { "pro", "-- * {0}" },
            { "ampl", "-- * {0}" }, { "octave", "-- * {0}" }, { "m", "-- * {0}" }, { "matlab", "-- * {0}" },

            // Single quote (')
            // Языки, где комментарии начинаются с апострофа `'` (например, VB.NET).
            { "vb", "' * {0}" }, { "bas", "' * {0}" }, { "asp", "' * {0}" },
            { "aspx", "' * {0}" },

            // REM (Windows Batch)
            // Скрипты Windows, где комментарии начинаются с ключевого слова `REM`.
            { "bat", "REM * {0}" }, { "cmd", "REM * {0}" }, { "nt", "REM * {0}" }, { "vbs", "REM * {0}" },

            // XML-style <!-- -->
            // Форматы разметки, где комментарии обрамляются `<!-- -->`.
            { "xml", "<!-- * {0} -->" }, { "xaml", "<!-- * {0} -->" }, { "html", "<!-- * {0} -->" },
            { "xhtml", "<!-- * {0} -->" }, { "svg", "<!-- * {0} -->" }, { "rss", "<!-- * {0} -->" },
            { "plist", "<!-- * {0} -->" },
            { "jspx", "<!-- * {0} -->" }, { "jsp", "<!-- * {0} -->" },

            // Special cases
            // Форматы, где `//` используется НЕОФИЦИАЛЬНО или УСЛОВНО (например, JSON, CSS, препроцессоры).
            // Примечание: Эти форматы могут не поддерживать комментарии официально, но `//` часто используется в реальных проектах.
            { "json", "// * {0}" }, { "css", "// * {0}" }, { "less", "// * {0}" }, { "sass", "// * {0}" },
            { "scss", "// * {0}" }
        };

        public static List<string> GetTextFileExtensions()
        {
            return _commentTemplateMap.Keys.ToList();
        }

        /// <summary>
        /// Возвращает шаблон однострочного комментария для заданного расширения файла.
        /// </summary>
        /// <param name="extension">Расширение файла (например, "cs", ".py", "html").</param>
        /// <returns>Шаблон комментария (например, "// * {0}") или null, если расширение неизвестно.</returns>
        public static string? GetSingleLineCommentTemplate(
            string extension
            )
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return null;
            }

            var ext = extension.Trim().ToLowerInvariant();

            if (ext.StartsWith("."))
            {
                ext = ext.Substring(1);
            }

            return _commentTemplateMap.TryGetValue(ext, out var template) ? template : null;
        }

        /// <summary>
        /// Возвращает только символ однострочного комментария для заданного расширения файла.
        /// </summary>
        /// <param name="extension">Расширение файла (например, "cs", ".py", "html").</param>
        /// <returns>Символ комментария (например, "//", "#") или null, если расширение неизвестно.</returns>
        public static string? GetSingleLineCommentSymbol(string extension)
        {
            var template = GetSingleLineCommentTemplate(extension);
            if (template == null)
            {
                return null;
            }

            return string.Format(template, string.Empty).Trim();
        }
    }
}
