﻿using EnvDTE80;

namespace FreeAIr.Helper
{
    public static class LineEndingHelper
    {
        public static class EditorConfig
        {
            /// <summary>
            /// Получает символ окончания строки для указанного файла.
            /// </summary>
            /// <param name="filePath">Путь к файлу.</param>
            /// <returns>Строку, содержащую символ окончания строки.</returns>
            public static string GetLineEndingFor(
                string filePath
                )
            {
                global::EditorConfig.Core.EditorConfigParser parser = new();
                global::EditorConfig.Core.FileConfiguration file = parser.Parse(filePath);

                if (file.EndOfLine.HasValue)
                {
                    return file.EndOfLine switch
                    {
                        global::EditorConfig.Core.EndOfLine.CRLF => "\r\n",
                        global::EditorConfig.Core.EndOfLine.LF => "\n",
                        global::EditorConfig.Core.EndOfLine.CR => "\r",
                        _ => Environment.NewLine
                    };
                }

                return Environment.NewLine;
            }
        }

        public static class Actual
        {
            /// <summary>
            /// Открывает документ по указанному пути и получает символ окончания строки.
            /// </summary>
            /// <param name="filePath">Путь к файлу.</param>
            /// <returns>Строку, содержащую символ окончания строки.</returns>
            public static string OpenDocumentAndGetLineEnding(
                string filePath
                )
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

                    var w = dte.ItemOperations.OpenFile(filePath);
                    var openDoc = w.Document;

                    var lineEnding = GetDocumentLineEnding(openDoc);
                    return lineEnding;
                }
                catch (Exception excp)
                {
                    //todo log
                }

                return Environment.NewLine;
            }

            /// <summary>
            /// Получает символ окончания строки для открытого документа по указанному пути.
            /// </summary>
            /// <param name="filePath">Путь к файлу.</param>
            /// <returns>Строку, содержащую символ окончания строки.</returns>
            public static string GetOpenedDocumentLineEnding(
                string filePath
                )
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

                    foreach (EnvDTE.Document document in dte.Documents)
                    {
                        if (string.Compare(
                            document.FullName,
                            filePath,
                            true) == 0)
                        {
                            var lineEnding = GetDocumentLineEnding(document);
                            return lineEnding;
                        }
                    }
                }
                catch (Exception excp)
                {
                    //todo log
                }

                return Environment.NewLine;
            }

            /// <summary>
            /// Получает символ окончания строки для активного документа.
            /// </summary>
            /// <returns>Строку, содержащую символ окончания строки.</returns>
            public static string GetActiveDocumentLineEnding()
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                try
                {
                    var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

                    var activeDoc = dte.ActiveDocument;
                    if (activeDoc is null || activeDoc.ReadOnly)
                    {
                        return Environment.NewLine;
                    }

                    var lineEnding = GetDocumentLineEnding(activeDoc);
                    return lineEnding;
                }
                catch (Exception excp)
                {
                    //todo log
                }

                return Environment.NewLine;
            }

            public static string GetDocumentLineEnding(
                string filePath
                )
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return EditorConfig.GetLineEndingFor(filePath);
                }

                var body = System.IO.File.ReadAllText(filePath);
                if (body.Contains("\r\n"))
                {
                    return "\r\n";
                }
                if (body.Contains("\r"))
                {
                    return "\r";
                }
                if (body.Contains("\n"))
                {
                    return "\n";
                }

                return Environment.NewLine;
            }

            private static string GetDocumentLineEnding(
                EnvDTE.Document activeDoc
                )
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (activeDoc.Object("TextDocument") is EnvDTE.TextDocument textDoc)
                {
                    var ep = textDoc.CreateEditPoint();
                    ep.EndOfLine();

                    var lineEnding = ep.GetText(null);
                    return lineEnding;
                }

                return Environment.NewLine;
            }

        }
    }
}
