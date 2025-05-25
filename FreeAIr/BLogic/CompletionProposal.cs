using FreeAIr.Helper;
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

#nullable enable
namespace FreeAIr.BLogic
{
    [Export(typeof(ProposalSource))]
    [Name("FreeAIrProposalSource")]
    [Order(Before = "InlineCSharpProposalSourceProvider")]
    [Order(Before = "Highest Priority")]
    [ContentType("any")]
    public sealed class ProposalSource : ProposalSourceBase
    {
        private const int MinDelayBeforeRequestsMsec = 500;

        private readonly ITextView _textView;

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

        public async Task<ProposalCollectionBase?> CreateProposalSourceAsync(
            int caretPosition
            )
        {
            try
            {
                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var point = new VirtualSnapshotPoint(_textView.TextSnapshot, caretPosition);
                var snap = new SnapshotSpan(point.Position, 0);

                var documentFilePath = _textView.TextBuffer.GetFileName();
                var documentText = _textView.TextSnapshot.GetText();

                var userPrompt = UserPrompt.CreateSuggestWholeLine(
                    documentFilePath,
                    documentText,
                    caretPosition
                    );

                var lineEnding = LineEndingHelper.Actual.GetOpenedDocumentLineEnding(documentFilePath);

                var chatContainer = componentModel.GetService<ChatContainer>();

                var chat = await chatContainer.StartChatAsync(
                    new ChatDescription(
                        ChatKindEnum.SuggestWholeLine,
                        null
                        ),
                    userPrompt
                    );
                if (chat is null)
                {
                    return null;
                }

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
                //todo log
                int g = 0;
            }

            return ProposalFactory.CreateEmptyCollection();
        }
    }

    [Export(typeof(ProposalSourceProvider))]
    [Export(typeof(ProposalSourceProviderBase))]
    [Name("FreeAIrProposalSourceProvider")]
    [Order(Before = "InlineCSharpProposalSourceProvider")]
    [Order(Before = "IntelliCodeCSharpProposalSource")]
    [Order(Before = "Highest Priority")]
    [ContentType("any")]
    public sealed class ProposalSourceProvider : ProposalSourceProviderBase, IDisposable
    {
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly IAsyncServiceProvider _serviceProvider;

        internal ProposalSourceProvider()
        {
        }

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


        private void OnProposalDisplayed(object sender, EventArgs e)
        {
        }

        private void OnProposalRejected(object sender, EventArgs e)
        {
        }

        private void OnSuggestionAccepted(object sender, EventArgs e)
        {
        }

        private void OnSuggestionDismissed(object sender, EventArgs e)
        {
        }


        public void Dispose()
        {
        }

        public override async Task<ProposalSourceBase?> GetProposalSourceAsync(
            ITextView view,
            CancellationToken cancel
            )
        {
            if (!ResponsePage.Instance.IsImplicitWholeLineCompletionEnabled)
            {
                return null;
            }

            return new ProposalSource(view);
        }
    }
}