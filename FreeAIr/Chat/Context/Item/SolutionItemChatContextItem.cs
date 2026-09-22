using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.Llm;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FreeAIr.Chat.Context.Composer;

namespace FreeAIr.Chat.Context.Item
{
    /// <summary>
    /// A file of the solution, or a selected fragment of one, attached to a chat as context.
    ///
    /// This is the context item the user creates most: everything dragged into the chat window,
    /// picked through the `#` autocomplete, or taken from the current editor selection ends up
    /// here. Unlike a snapshot, it reads the file every time the prompt is built, so a chat which
    /// has been open for a while still sends what is on disk right now.
    ///
    /// It is also the only context item that can write back — see <see cref="ReplaceWithText"/>,
    /// which is how the "apply the answer to the file" commands land their result.
    /// </summary>
    [DebuggerDisplay("{SelectedIdentifier.FilePath}")]
    public sealed class SolutionItemChatContextItem : IChatContextItem
    {
        /// <summary>Whether, and how, line numbers are prefixed onto the body sent to the model.</summary>
        private readonly AddLineNumbersMode _addLineNumberMode;

        /// <summary>Which file, and which part of it — the whole file when the selection is null.</summary>
        public SelectedIdentifier SelectedIdentifier
        {
            get;
        }

        /// <summary>The label shown on the context chip in the chat window.</summary>
        public string ContextUIDescription => SelectedIdentifier.ContextUIDescription;

        /// <summary>
        /// True when the item was pulled in by the C# context composer following references rather
        /// than chosen by the user. The chat window shows those differently, so that an unexpected
        /// file in the context can be told from one the user attached on purpose.
        /// </summary>
        public bool IsAutoFound
        {
            get;
        }

        /// <summary>How line numbers are prefixed onto this file when it is sent to the model. Kept so a restored chat sends the same numbering it did before the restart.</summary>
        public AddLineNumbersMode LineNumberMode => _addLineNumberMode;

        /// <summary>Creates a context item for a solution file or fragment, with the given line-numbering mode.</summary>
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

        /// <summary>
        /// Whether two context items point at the same file and the same fragment of it. Used to
        /// keep the context free of duplicates when the same file arrives twice — from the user and
        /// from the reference walker, for instance.
        /// </summary>
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

        /// <summary>Opens the underlying file (and selection, if any) in a Visual Studio editor tab.</summary>
        public async Task OpenInNewWindowAsync()
        {
            await SelectedIdentifier.OpenInNewWindowAsync();
        }


        /// <summary>
        /// The file as it goes into the prompt: a fenced markdown code block, tagged with the
        /// language guessed from the extension so the model reads it as code.
        ///
        /// A whole file is introduced by a line naming its path — the model needs the name to talk
        /// about the file and to write a patch against it. A selection is sent bare, because it is
        /// always the subject of the prompt right next to it.
        ///
        /// A missing file is reported inside the prompt instead of throwing: the user may have
        /// deleted it since attaching, and losing the whole request over it would be worse.
        /// </summary>
        public async Task<string> AsContextPromptTextAsync()
        {
            if (!File.Exists(SelectedIdentifier.FilePath))
            {
                return $"`File {SelectedIdentifier.FilePath} does not found`";
            }

            var fi = new FileInfo(SelectedIdentifier.FilePath);

            if (SelectedIdentifier.Selection is not null)
            {
                //a selection is taken from the editor buffer, not from disk: it may well be the
                //unsaved text the user is looking at, and that is what they mean by "this"
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

        /// <summary>Applies <see cref="_addLineNumberMode"/> to the given body text before it is sent to the model.</summary>
        private string AddLineNumbers(string body, string lineEnding)
        {
            return _addLineNumberMode.AddLineNumbers(body, lineEnding);
        }

        /// <summary>
        /// Writes the model's text back over the file, or over just the selected fragment when
        /// there is one. This is what the apply, fix and rewrite commands call once the user has
        /// accepted an answer.
        ///
        /// The line endings of the incoming text are converted to whatever the document already
        /// uses: a model answers in whatever it likes, and writing that verbatim turns the whole
        /// file into one line of the diff.
        /// </summary>
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

        /// <summary>
        /// The other files this one refers to, found by walking the C# type references. Offered to
        /// the user as context worth adding, because a question about a class is usually a question
        /// about the types it works with as well.
        /// </summary>
        public async Task<IReadOnlyList<SolutionItemChatContextItem>> SearchRelatedContextItemsAsync()
        {
            var contextItems = (await CSharpContextComposer.ComposeFromFilePathAsync(
                SelectedIdentifier.FilePath
                )).ConvertToChatContextItem();
            return contextItems;
        }

        /// <summary>This item as one user message, ready to be put in front of the actual prompt.</summary>
        public async Task<LlmMessage> CreateChatMessageAsync()
        {
            return LlmMessage.CreateUserMessage(
                await AsContextPromptTextAsync()
                );
        }

    }

}
