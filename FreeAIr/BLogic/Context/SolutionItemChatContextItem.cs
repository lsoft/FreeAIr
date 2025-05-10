using FreeAIr.Helper;
using System.IO;

namespace FreeAIr.BLogic.Context
{
    public sealed class SolutionItemChatContextItem : IChatContextItem
    {
        public string FilePath
        {
            get;
        }

        public string ContextUIDescription => FilePath;

        public SolutionItemChatContextItem(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = Path.GetFullPath(filePath);
        }

        public bool IsSame(IChatContextItem other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is not SolutionItemChatContextItem otherDisk)
            {
                return false;
            }

            var result = StringComparer.CurrentCultureIgnoreCase.Compare(
                FilePath,
                otherDisk.FilePath
                ) == 0;

            return result;
        }

        public async Task OpenInNewWindowAsync()
        {
            await VS.Documents.OpenAsync(FilePath);
        }


        public string AsContextPromptText()
        {
            if (!File.Exists(FilePath))
            {
                return $"`File {FilePath} does not found`";
            }

            var fi = new FileInfo(FilePath);

            return
                Environment.NewLine
                + $"Source code of the file `{FilePath}`:"
                + Environment.NewLine
                + Environment.NewLine
                + "```"
                + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                + Environment.NewLine
                + System.IO.File.ReadAllText(FilePath)
                + Environment.NewLine
                + "```"
                + Environment.NewLine;
        }

        public void ReplaceWithText(string body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var lineEnding = LineEndingHelper.Actual.OpenDocumentAndGetLineEnding(
                FilePath
                );


            File.WriteAllText(
                FilePath,
                body.WithLineEnding(lineEnding)
                );
        }
    }

}
