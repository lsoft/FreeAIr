using FreeAIr.BLogic.Context.Composer;
using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.BLogic.Context.Item
{
    public sealed class SolutionItemChatContextItem : IChatContextItem
    {
        private readonly AddLineNumbersMode _addLineNumberMode;

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
            return _addLineNumberMode.ProcessLineNumbers(body, lineEnding);
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

        public async Task<UserChatMessage> CreateChatMessageAsync()
        {
            return new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    await AsContextPromptTextAsync()
                    )
                );
        }

    }

    public sealed class AddLineNumbersMode
    {
        private readonly AddLineNumbersModeEnum _mode;
        private List<(int StartLine, int LineCount)> _scopes;

        public static readonly AddLineNumbersMode NotRequired = new AddLineNumbersMode(AddLineNumbersModeEnum.Disabled);

        public static readonly AddLineNumbersMode RequiredAllInScope = new AddLineNumbersMode(AddLineNumbersModeEnum.AllInScope);

        public bool Enabled => _mode != AddLineNumbersModeEnum.Disabled;

        private AddLineNumbersMode(
            AddLineNumbersModeEnum mode
            )
        {
            _mode = mode;
            _scopes = new();
        }

        private AddLineNumbersMode(
            List<(int StartLine, int LineCount)> scopes
            )
        {
            _mode = AddLineNumbersModeEnum.SpecificScopes;
            _scopes = scopes;
        }

        public static AddLineNumbersMode RequiredForScopes(
            List<(int StartLine, int LineCount)> scopes
            )
        {
            return new AddLineNumbersMode(scopes);
        }

        public enum AddLineNumbersModeEnum
        {
            Disabled,
            AllInScope,
            SpecificScopes
        }

        public string ProcessLineNumbers(
            string body,
            string lineEnding
            )
        {
            if (!Enabled)
            {
                return body;
            }

            var lines = body.Split(new[] { lineEnding }, StringSplitOptions.None);

            var digitCount = Math.Ceiling(Math.Log10(lines.Length));
            var stringFormat = "D" + digitCount.ToString();

            var result = new StringBuilder();
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];

                switch (_mode)
                {
                    case AddLineNumbersModeEnum.AllInScope:
                        {
                            result.Append(lineIndex.ToString(stringFormat));
                            result.Append(": ");
                        }
                        break;
                    case AddLineNumbersModeEnum.SpecificScopes:
                        {
                            if (_scopes.Any(s => s.StartLine <= lineIndex && (s.StartLine + s.LineCount) > lineIndex))
                            {
                                result.Append(lineIndex.ToString(stringFormat));
                                result.Append(": ");
                            }
                        }
                        break;
                }

                result.AppendLine(line);
            }

            return result.ToString();
        }
    }

}
