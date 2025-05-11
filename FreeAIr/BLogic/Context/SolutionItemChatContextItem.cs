using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.IO;
using System.IO.Packaging;
using System.Threading.Tasks;

namespace FreeAIr.BLogic.Context
{
    public sealed class SolutionItemChatContextItem : IChatContextItem
    {
        public SelectedIdentifier SelectedIdentifier
        {
            get;
        }

        public string ContextUIDescription => SelectedIdentifier.FilePath + SelectedIdentifier.Selection?.ToString();

        public bool IsAutoFound
        {
            get;
        }

        public SolutionItemChatContextItem(
            SelectedIdentifier selectedIdentifier,
            bool isAutoFound
            )
        {
            if (selectedIdentifier is null)
            {
                throw new ArgumentNullException(nameof(selectedIdentifier));
            }

            SelectedIdentifier = selectedIdentifier;
            IsAutoFound = isAutoFound;
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
                SelectedIdentifier.FilePath,
                otherDisk.SelectedIdentifier.FilePath
                ) == 0;

            return result;
        }

        public async Task OpenInNewWindowAsync()
        {
            await SelectedIdentifier.OpenInNewWindowAsync();
        }


        public async Task<string> AsContextPromptTextAsync()
        {
            if (!File.Exists(SelectedIdentifier.FilePath))
            {
                return $"`File {SelectedIdentifier.FilePath} does not found`";
            }

            var fi = new FileInfo(SelectedIdentifier.FilePath);

            if (SelectedIdentifier.Selection is not null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var documentView = await VS.Documents.GetDocumentViewAsync(SelectedIdentifier.FilePath);
                var body = documentView.TextView.TextSnapshot.GetText(
                    SelectedIdentifier.Selection.GetVisualStudioSpan()
                    );

                return
                    Environment.NewLine
                    + "```"
                    + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                    + Environment.NewLine
                    + body
                    + Environment.NewLine
                    + "```"
                    + Environment.NewLine
                    ;
            }
            else
            {
                return
                    Environment.NewLine
                    + $"Source code of the file `{SelectedIdentifier.FilePath}`:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "```"
                    + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                    + Environment.NewLine
                    + System.IO.File.ReadAllText(SelectedIdentifier.FilePath)
                    + Environment.NewLine
                    + "```"
                    + Environment.NewLine
                    ;
            }
        }

        public void ReplaceWithText(string body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var lineEnding = LineEndingHelper.Actual.OpenDocumentAndGetLineEnding(
                SelectedIdentifier.FilePath
                );


            File.WriteAllText(
                SelectedIdentifier.FilePath,
                body.WithLineEnding(lineEnding)
                );
        }
    }

}
