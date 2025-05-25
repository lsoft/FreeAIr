using FreeAIr.BLogic.Context.Composer;
using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace FreeAIr.BLogic.Context.Item
{
    public sealed class SolutionItemChatContextItem : IChatContextItem
    {
        private readonly bool _addLineNumberInTheBody;

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
            bool isAutoFound,
            bool addLineNumberInTheBody = false
            )
        {
            if (selectedIdentifier is null)
            {
                throw new ArgumentNullException(nameof(selectedIdentifier));
            }

            SelectedIdentifier = selectedIdentifier;
            IsAutoFound = isAutoFound;
            _addLineNumberInTheBody = addLineNumberInTheBody;
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

            if (!SelectedIdentifier.Equals(
                    otherDisk.SelectedIdentifier
                    )
                )
            {
                return false;
            }

            return true;
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

                var lineEnding = LineEndingHelper.Actual.GetOpenedDocumentLineEnding(SelectedIdentifier.FilePath);
                var modifiedBody = ProcessLineNumbers(
                    body,
                    lineEnding
                    );

                return
                    Environment.NewLine
                    + "```"
                    + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                    + Environment.NewLine
                    + modifiedBody
                    + Environment.NewLine
                    + "```"
                    + Environment.NewLine
                    ;
            }
            else
            {
                var lineEnding = LineEndingHelper.Actual.GetDocumentLineEnding(SelectedIdentifier.FilePath);
                var modifiedBody = ProcessLineNumbers(
                    System.IO.File.ReadAllText(SelectedIdentifier.FilePath),
                    lineEnding
                    );

                return
                    Environment.NewLine
                    + $"Text of the file `{SelectedIdentifier.FilePath}`:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "```"
                    + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                    + Environment.NewLine
                    + modifiedBody
                    + Environment.NewLine
                    + "```"
                    + Environment.NewLine
                    ;
            }
        }

        private string ProcessLineNumbers(string body, string lineEnding)
        {
            if (!_addLineNumberInTheBody)
            {
                return body;
            }

            var lines = body.Split(new[] { lineEnding }, StringSplitOptions.None);

            var digitCount = Math.Ceiling(Math.Log10(lines.Length));
            var toStringMode = "D" + digitCount.ToString();

            var result = new StringBuilder();
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                if (_addLineNumberInTheBody)
                {
                    result.Append(lineIndex.ToString(toStringMode));
                    result.Append(": ");
                }

                result.AppendLine(line);
            }

            return result.ToString();
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

        public async Task<IReadOnlyList<IChatContextItem>> SearchRelatedContextItemsAsync()
        {
            var contextItems = (await ContextComposer.ComposeFromFilePathAsync(
                SelectedIdentifier.FilePath
                )).ConvertToChatContextItem();
            return contextItems;
        }
    }

}
