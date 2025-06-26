using FreeAIr.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FreeAIr.BLogic.Context.Item
{
    public sealed class CustomFileContextItem : IChatContextItem
    {
        private readonly string _filePath;

        public string ContextUIDescription => _filePath;

        public bool IsAutoFound
        {
            get;
        }

        public CustomFileContextItem(
            string filePath,
            bool isAutoFound
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _filePath = filePath.Trim();
            IsAutoFound = isAutoFound;
        }

        public async Task<string> AsContextPromptTextAsync()
        {
            if (!File.Exists(_filePath))
            {
                return $"`File {_filePath} does not found`";
            }

            var fi = new FileInfo(_filePath);
            return
                Environment.NewLine
                + $"Content of the file `{_filePath}`:"
                + Environment.NewLine
                + Environment.NewLine
                + "```"
                + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                + Environment.NewLine
                + System.IO.File.ReadAllText(_filePath)
                + Environment.NewLine
                + "```"
                + Environment.NewLine
                ;
        }

        public bool IsSame(IChatContextItem other)
        {
            if (other is not CustomFileContextItem other2)
            {
                return false;
            }

            return StringComparer.CurrentCultureIgnoreCase.Compare(_filePath, other2._filePath) == 0;
        }

        public async Task OpenInNewWindowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                var documentView = await VS.Documents.OpenAsync(_filePath);
                if (documentView is null)
                {
                    return;
                }
            }
            catch (Exception excp)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    "Error: "
                    + Environment.NewLine
                    + excp.Message
                    + Environment.NewLine
                    + Environment.NewLine
                    + excp.StackTrace
                    );

                //todo log
            }
        }

        public void ReplaceWithText(string body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var lineEnding = LineEndingHelper.Actual.OpenDocumentAndGetLineEnding(
                _filePath
                );


            File.WriteAllText(
                _filePath,
                body.WithLineEnding(lineEnding)
                );
        }

        public async Task<IReadOnlyList<SolutionItemChatContextItem>> SearchRelatedContextItemsAsync()
        {
            return [];
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
