using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FreeAIr.Chat.Context.Composer;

namespace FreeAIr.Chat.Context.Item
{
    [DebuggerDisplay("{SelectedIdentifier.FilePath}")]
    public sealed class SolutionItemChatContextItem : IChatContextItem
    {
        private readonly AddLineNumbersMode _addLineNumberMode;

        public SelectedIdentifier SelectedIdentifier
        {
            get;
        }

        public string ContextUIDescription => SelectedIdentifier.ContextUIDescription;

        public bool IsAutoFound
        {
            get;
        }

        public SolutionItemChatContextItem(
            SelectedIdentifier selectedIdentifier,
            bool isAutoFound,
            AddLineNumbersMode addLineNumberBody
            )
        {
            if (selectedIdentifier is null)
            {
                throw new ArgumentNullException(nameof(selectedIdentifier));
            }

            if (addLineNumberBody is null)
            {
                throw new ArgumentNullException(nameof(addLineNumberBody));
            }

            SelectedIdentifier = selectedIdentifier;
            IsAutoFound = isAutoFound;
            _addLineNumberMode = addLineNumberBody;
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
                var modifiedBody = AddLineNumbers(
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
                var modifiedBody = AddLineNumbers(
                    System.IO.File.ReadAllText(SelectedIdentifier.FilePath),
                    lineEnding
                    );

                return
                    Environment.NewLine
                    + $"Content of the file `{SelectedIdentifier.FilePath}`:"
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

        private string AddLineNumbers(string body, string lineEnding)
        {
            return _addLineNumberMode.AddLineNumbers(body, lineEnding);
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

            if (SelectedIdentifier.Selection is null)
            {
                File.WriteAllText(
                    SelectedIdentifier.FilePath,
                    body.WithLineEnding(lineEnding)
                    );
            }
            else
            {
                var text = File.ReadAllText(SelectedIdentifier.FilePath);
                var mtext = text.Substring(
                    0,
                    SelectedIdentifier.Selection.StartPosition
                    )
                    + body.WithLineEnding(lineEnding)
                    + text.Substring(
                        SelectedIdentifier.Selection.StartPosition + SelectedIdentifier.Selection.Length
                        );
                File.WriteAllText(
                    SelectedIdentifier.FilePath,
                    mtext
                    );
            }
        }

        public async Task<IReadOnlyList<SolutionItemChatContextItem>> SearchRelatedContextItemsAsync()
        {
            var contextItems = (await CSharpContextComposer.ComposeFromFilePathAsync(
                SelectedIdentifier.FilePath
                )).ConvertToChatContextItem();
            return contextItems;
        }

        public async Task<UserChatMessage> CreateChatMessageAsync()
        {
            return new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    await AsContextPromptTextAsync()
                    )
                );
        }

    }

}
