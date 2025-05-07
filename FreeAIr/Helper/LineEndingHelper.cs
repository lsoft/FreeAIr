using EnvDTE80;

namespace FreeAIr.Helper
{
    public static class LineEndingHelper
    {
        public static class EditorConfig
        {
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
