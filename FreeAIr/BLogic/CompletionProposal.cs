using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static FreeAIr.Helper.SuggestionHijackHelper;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Item;

#nullable enable
namespace FreeAIr.BLogic
{
    /// <summary>
    /// Whole line completion: the grey inline suggestion Visual Studio shows ahead of the caret,
    /// produced here by asking an LLM instead of by IntelliCode.
    ///
    /// Visual Studio asks this source for a proposal on practically every keystroke, and every
    /// proposal is a full request to a model. That is why the ordering attributes put this source
    /// ahead of the built-in ones, why the class waits before doing anything at all, and why the
    /// feature is off unless the user turns it on in the settings.
    ///
    /// One source instance serves one editor view; the views are handed out by
    /// <see cref="ProposalSourceProvider"/>.
    /// </summary>
    [Export(typeof(ProposalSource))]
    [Name("FreeAIrProposalSource")]
    [Order(Before = "InlineCSharpProposalSourceProvider")]
    [Order(Before = "Highest Priority")]
    [ContentType("any")]
    public sealed class ProposalSource : ProposalSourceBase
    {
        /// <summary>
        /// How long the typing has to stop before a request goes out. Anything shorter turns a
        /// sentence being typed into a request per character, all but the last one cancelled and
        /// all of them paid for.
        /// </summary>
        private const int MinDelayBeforeRequestsMsec = 500;

        /// <summary>The editor view this suggestion source produces inline completions for.</summary>
        private readonly ITextView _textView;

        /// <summary>Binds this source to the editor view it will serve proposals for.</summary>
        public ProposalSource(
            ITextView textView
            )
        {
            if (textView is null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            _textView = textView;
        }

        /// <summary>
        /// Called by the editor whenever it would like something to suggest at the caret.
        ///
        /// The whole body of the method is the debounce: it sleeps first, and the editor cancels
        /// the token the moment the user types again, so only the pause at the end of a burst of
        /// typing ever reaches the model. A cancellation is the normal outcome here, not a failure.
        /// </summary>
        public override async Task<ProposalCollectionBase?> RequestProposalsAsync(
            VirtualSnapshotPoint caret,
            CompletionState? completionState,
            ProposalScenario scenario,
            char triggeringCharacter,
            CancellationToken cancellationToken
            )
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(MinDelayBeforeRequestsMsec),
                    cancellationToken
                    );

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                var caretPosition = caret.Position.Position;

                return await CreateProposalSourceAsync(
                    caretPosition
                    );
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// Builds one suggestion for the caret position: finds the configured action and agent,
        /// sends the document with an anchor marking where the code should go, and turns the answer
        /// into a proposal the editor can display.
        ///
        /// The anchor is the trick that makes this work at all. Rather than describing the position
        /// in words, the document is sent with a marker string inserted at the caret, and the
        /// prompt asks for the code that belongs where the marker is. The document itself is never
        /// modified — only the copy that goes into the request.
        ///
        /// Everything is swallowed and logged: this runs on the typing path, and a dialog or an
        /// exception here would interrupt the user rather than help them.
        /// </summary>
        public async Task<ProposalCollectionBase?> CreateProposalSourceAsync(
            int caretPosition
            )
        {
            try
            {
                var options = await FreeAIrOptions.DeserializeAsync();

                //the feature needs an action wired to a named agent; without one there is nothing
                //to ask, and the complaint is worth showing only if the user believes it is enabled
                var support = await FreeAIrOptions.DeserializeSupportCollectionAsync();
                var action = support.Actions.FirstOrDefault(
                    a =>
                        a.Scopes.Contains(SupportScopeEnum.WholeLineCompletion)
                        && !string.IsNullOrEmpty(a.AgentName)
                        );
                if (action is null)
                {
                    if (!options.Unsorted.IsImplicitWholeLineCompletionEnabled)
                    {
                        await VS.MessageBox.ShowErrorAsync(
                            $"No support action for {SupportScopeEnum.WholeLineCompletion} and with non empty agent name was defined."
                            );
                    }
                    return null;
                }
                var chosenAgent = options.AgentCollection.Agents.FirstOrDefault(a => a.Name == action.AgentName);
                if (chosenAgent is null)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        $"No agent with name {action.AgentName} was found. Check the whole line completion action."
                        );
                    return null;
                }

                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var point = new VirtualSnapshotPoint(_textView.TextSnapshot, caretPosition);
                var snap = new SnapshotSpan(point.Position, 0);

                var documentFilePath = _textView.TextBuffer.GetFileName();
                var documentFileInfo = new System.IO.FileInfo(documentFilePath);
                var documentText = _textView.TextSnapshot.GetText();

                var supportContext = await SupportContext.WithWholeLineDataAsync(
                    documentFilePath
                    );

                var promptText = supportContext.ApplyVariablesToPrompt(
                    action.Prompt
                    );
                var userPrompt = UserPrompt.CreateTextBasedPrompt(
                    promptText
                    );

                var lineEnding = LineEndingHelper.Actual.GetOpenedDocumentLineEnding(documentFilePath);

                var chatContainer = componentModel.GetService<ChatContainer>();

                var chat = await chatContainer.StartChatAsync(
                    new ChatDescription(
                        null
                        ),
                    userPrompt,
                    await ChatOptions.NoToolAutoProcessedTextResponseAsync(chosenAgent)
                    );
                if (chat is null)
                {
                    return null;
                }

                //this is the whole point of the request: the model is shown the file with a marker
                //where the caret is and asked what belongs there. Only the copy sent to the model
                //carries the anchor; the buffer the user is typing in is untouched.
                documentText = documentText.Insert(
                    caretPosition,
                    options.Unsorted.WholeLineCompletionAnchorName
                    );

                var promptBody =
                    Environment.NewLine
                    + $"Content of the file `{documentFilePath}`:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "```"
                    + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(documentFileInfo.Extension)
                    + Environment.NewLine
                    + documentText
                    + Environment.NewLine
                    + "```"
                    ;

                chat.ChatContext.AddItems(
                    [
                        new SimpleTextChatContextItem(
                            documentFilePath,
                            promptBody,
                            false
                            )
                    ]
                    );

                var cleanAnswer = await chat.WaitForPromptCleanAnswerAsync(
                    lineEnding
                    );
                if (!string.IsNullOrEmpty(cleanAnswer))
                {
                    var proposalCollection = ProposalFactory.CreateCollectionFromText(
                        cleanAnswer,
                        _textView,
                        caretPosition
                        );

                    return proposalCollection;
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return ProposalFactory.CreateEmptyCollection();
        }
    }

    /// <summary>
    /// The MEF entry point Visual Studio finds: it hands out a <see cref="ProposalSource"/> per
    /// editor view, and refuses to hand out any while whole line completion is switched off in the
    /// settings.
    ///
    /// The constructor also subscribes to four internal events of the suggestion service through
    /// reflection. They are not part of any public API — see <see cref="SuggestionHijackHelper"/> —
    /// and the handlers are deliberately empty: the subscription exists to keep the hooks in place
    /// for the telemetry that would tell whether a suggestion was shown, accepted or thrown away.
    /// </summary>
    [Export(typeof(ProposalSourceProvider))]
    [Export(typeof(ProposalSourceProviderBase))]
    [Name("FreeAIrProposalSourceProvider")]
    [Order(Before = "InlineCSharpProposalSourceProvider")]
    [Order(Before = "IntelliCodeCSharpProposalSource")]
    [Order(Before = "Highest Priority")]
    [ContentType("any")]
    public sealed class ProposalSourceProvider : ProposalSourceProviderBase, IDisposable
    {
        /// <summary>MEF-imported service used to resolve the text document behind an editor view.</summary>
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        /// <summary>MEF-imported async service provider, used to reach package-level services.</summary>
        private readonly IAsyncServiceProvider _serviceProvider;

        /// <summary>Parameterless constructor required by MEF for provider discovery.</summary>
        internal ProposalSourceProvider()
        {
        }

        /// <summary>
        /// MEF import constructor: also hooks the suggestion service's internal display/accept/
        /// reject/dismiss events through reflection, purely to keep the hooks alive for telemetry —
        /// see <see cref="SuggestionHijackHelper"/> for why reflection is needed here.
        /// </summary>
        [ImportingConstructor]
        internal ProposalSourceProvider(
          ITextDocumentFactoryService textDocumentFactoryService,
          SuggestionServiceBase suggestionServiceBase,
          [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider,
          IAsyncCompletionBroker asyncCompletionBroker,
          ICompletionBroker completionBroker)
        {
            _textDocumentFactoryService = textDocumentFactoryService;
            _serviceProvider = serviceProvider;

            suggestionServiceBase.GetType().GetEvent("ProposalDisplayedInternal", BindingFlags.Instance | BindingFlags.Public)?.AddEventHandler(suggestionServiceBase, new EventHandler<EventArgs>(OnProposalDisplayed));
            suggestionServiceBase.GetType().GetEvent("ProposalRejectedInternal", BindingFlags.Instance | BindingFlags.Public)?.AddEventHandler(suggestionServiceBase, new EventHandler<EventArgs>(OnProposalRejected));
            suggestionServiceBase.GetType().GetEvent("SuggestionAcceptedInternal", BindingFlags.Instance | BindingFlags.Public)?.AddEventHandler(suggestionServiceBase, new EventHandler<EventArgs>(OnSuggestionAccepted));
            suggestionServiceBase.GetType().GetEvent("SuggestionDismissedInternal", BindingFlags.Instance | BindingFlags.Public)?.AddEventHandler(suggestionServiceBase, new EventHandler<EventArgs>(OnSuggestionDismissed));
        }


        /// <summary>Empty handler kept only to hold the reflection-based subscription to the suggestion service's proposal-displayed event.</summary>
        private void OnProposalDisplayed(object sender, EventArgs e)
        {
        }

        /// <summary>Empty handler kept only to hold the reflection-based subscription to the suggestion service's proposal-rejected event.</summary>
        private void OnProposalRejected(object sender, EventArgs e)
        {
        }

        /// <summary>Empty handler kept only to hold the reflection-based subscription to the suggestion service's suggestion-accepted event.</summary>
        private void OnSuggestionAccepted(object sender, EventArgs e)
        {
        }

        /// <summary>Empty handler kept only to hold the reflection-based subscription to the suggestion service's suggestion-dismissed event.</summary>
        private void OnSuggestionDismissed(object sender, EventArgs e)
        {
        }


        /// <summary>No unmanaged or disposable state to release; present to satisfy <see cref="IDisposable"/>.</summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// A proposal source for this view, or null when the feature is off — returning null is how
        /// a provider opts out and leaves the editor to the other suggestion sources.
        ///
        /// The setting is read on every call rather than cached, so switching the feature off takes
        /// effect on the next keystroke instead of on the next restart.
        /// </summary>
        public override async Task<ProposalSourceBase?> GetProposalSourceAsync(
            ITextView view,
            CancellationToken cancel
            )
        {
            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            if (!unsorted.IsImplicitWholeLineCompletionEnabled)
            {
                return null;
            }

            return new ProposalSource(view);
        }
    }
}