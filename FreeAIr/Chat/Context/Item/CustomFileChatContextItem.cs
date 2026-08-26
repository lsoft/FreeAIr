using FreeAIr.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FreeAIr.Chat.Context.Item
{
    /// <summary>
    /// A file attached to a chat by its path alone, without being part of the solution.
    ///
    /// This is how a model gets to see something Visual Studio knows nothing about — a log, a
    /// config outside the repository, a file an MCP tool has just written. Because there is no
    /// solution item behind it, there is no selection to narrow it down and no reference graph to
    /// walk: the whole file goes in, and nothing related comes with it.
    ///
    /// For files that are part of the solution use <see cref="SolutionItemChatContextItem"/>, which
    /// can do both of those things.
    /// </summary>
    public sealed class CustomFileChatContextItem : IChatContextItem
    {
        /// <summary>The full path of the attached file, outside of any solution.</summary>
        private readonly string _filePath;

        /// <summary>The full path of the attached file. Same value the chip shows, and what a restored chat uses to rebuild this item.</summary>
        public string FilePath => _filePath;

        /// <summary>The label shown on the context chip in the chat window: the file's own path.</summary>
        public string ContextUIDescription => _filePath;

        /// <summary>True when this file was attached automatically rather than by the user.</summary>
        public bool IsAutoFound
        {
            get;
        }

        /// <summary>Attaches the file at <paramref name="filePath"/>, trimming stray whitespace from the path.</summary>
        public CustomFileChatContextItem(
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

        /// <summary>
        /// The whole file as a fenced markdown block, introduced by its path so the model can refer
        /// to it. Read from disk on every request, so a file changing under an open chat is picked
        /// up.
        ///
        /// A missing file becomes a note inside the prompt rather than an exception: the path comes
        /// from outside the solution and may well have gone away, and that should not cost the user
        /// the request.
        /// </summary>
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

        /// <summary>
        /// Two items are the same when they name the same path, compared case insensitively as
        /// Windows would. Keeps a file from being attached twice.
        /// </summary>
        public bool IsSame(IChatContextItem other)
        {
            if (other is not CustomFileChatContextItem other2)
            {
                return false;
            }

            return StringComparer.CurrentCultureIgnoreCase.Compare(_filePath, other2._filePath) == 0;
        }

        /// <summary>
        /// Opens the file in an editor tab, which is what clicking its chip in the chat window
        /// does. Failure is shown to the user and swallowed: an unopenable path is a nuisance, not
        /// a reason to break the chat.
        /// </summary>
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

                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Overwrites the file with the model's text, converted to the line endings the document
        /// already uses. Always the whole file — there is no selection here to replace a part of.
        /// </summary>
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

        /// <summary>
        /// Always empty. Finding related files needs a Roslyn document, and a file outside the
        /// solution has none.
        /// </summary>
        public async Task<IReadOnlyList<SolutionItemChatContextItem>> SearchRelatedContextItemsAsync()
        {
            return [];
        }

        /// <summary>Wraps the file's rendered prompt text into a chat message ready for the request.</summary>
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
