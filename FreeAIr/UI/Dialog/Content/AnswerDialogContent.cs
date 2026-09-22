using MarkdownParser.Antlr.Answer;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using WpfHelpers;
using FreeAIr.Chat.Content;

namespace FreeAIr.UI.Dialog.Content
{
    /// <summary>
    /// Dialog content that renders one LLM answer bubble in the chat window, converting the answer's
    /// markdown body into a WPF <see cref="FlowDocument"/> and refreshing it as streamed text arrives.
    /// </summary>
    public sealed class AnswerDialogContent : DialogContent<AnswerChatContent>
    {
        /// <summary>Parses the answer's markdown body into renderable parts (code blocks, links, tool calls, etc).</summary>
        private readonly IMarkdownParser _answerParser;
        /// <summary>Commands (e.g. apply-to-file) that markdown parts such as code blocks can attach themselves to.</summary>
        private readonly AdditionalCommandContainer _additionalCommandContainer;
        /// <summary>
        /// The chat this answer sits in, as far as rewinding and forking are concerned. Null when
        /// the answer is rendered outside a chat window, and then neither link is offered.
        /// </summary>
        private readonly IChatTimeline? _timeline;

        /// <summary>Answer bubbles are always left-aligned in the chat, unlike the user's own prompts.</summary>
        public HorizontalAlignment HorizontalAlignment => HorizontalAlignment.Left;

        /// <summary>The links under the answer only exist when there is a chat they could act on.</summary>
        public Visibility TimelineVisibility => _timeline is null ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// Drops everything the chat holds after this answer, once the user has confirmed it. The
        /// link is disabled while a turn is in flight, because the transcript is being written into
        /// just then; <see cref="IChatTimeline.CanRewindAfter"/> is the rule, and WPF re-asks it on
        /// its own.
        /// </summary>
        public ICommand RewindCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_timeline is null)
                            {
                                return;
                            }

                            await _timeline.RewindAfterAsync(TypedContent);
                        },
                        a => _timeline is not null && _timeline.CanRewindAfter(TypedContent)
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// Starts a new chat holding the dialogue up to and including this answer and switches to
        /// it, leaving this chat untouched. Nothing is destroyed, so unlike Rewind it asks nothing.
        /// </summary>
        public ICommand ForkCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            if (_timeline is null)
                            {
                                return;
                            }

                            await _timeline.ForkAtAsync(TypedContent);
                        },
                        a => _timeline is not null && _timeline.CanForkAt(TypedContent)
                        );
                }

                return field;
            }
        }

        /// <summary>Padding around the answer bubble's border.</summary>
        public Thickness BorderThickness
        {
            get;
        } = new Thickness(10, 1, 1, 1);

        /// <summary>The rendered flow document shown in the chat bubble, rebuilt each time the answer text changes.</summary>
        public FlowDocument Document
        {
            get;
        } = new FlowDocument();

        /// <summary>Wires up the markdown parser and command container, then renders the initial flow document.</summary>
        private AnswerDialogContent(
            IMarkdownParser answerParser,
            AdditionalCommandContainer? additionalCommandContainer,
            IChatTimeline? timeline,
            AnswerChatContent answer,
            bool isInProgress
            ) : base(answer, answer)
        {
            if (answerParser is null)
            {
                throw new ArgumentNullException(nameof(answerParser));
            }

            _answerParser = answerParser;
            _additionalCommandContainer = additionalCommandContainer;
            _timeline = timeline;

            UpdateDocument(isInProgress);
            answer.AnswerChangedEvent.Event += AnswerChangedRaisedAsync;
        }

        /// <summary>Handles the answer's streamed-text-changed event by refreshing the rendered flow document.</summary>
        private async Task AnswerChangedRaisedAsync(object sender, AnswerChangedEventArgs args)
        {
            await UpdateDocumentAsync(true);
        }

        /// <summary>Rebuilds the flow document on the UI thread and notifies bindings that it changed.</summary>
        private async Task UpdateDocumentAsync(bool isInProgress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            UpdateDocument(isInProgress);

            OnPropertyChanged();
        }

        /// <summary>Reparses the answer's current markdown body and rewrites <see cref="Document"/> from it.</summary>
        private void UpdateDocument(bool isInProgress)
        {
            var parsedAnswer = _answerParser.Parse(TypedContent.AnswerBody);

            parsedAnswer.UpdateFlowDocument(
                Document,
                _additionalCommandContainer,
                isInProgress
                );
        }

        /// <summary>Builds an <see cref="AnswerDialogContent"/> for the given answer, resolving the markdown parser via the VS component model.</summary>
        public static AnswerDialogContent Create(
            AnswerChatContent answer,
            AdditionalCommandContainer? additionalCommandContainer,
            IChatTimeline? timeline,
            bool isInProgress
            )
        {
            var componentModel = FreeAIrPackage.Instance.GetService<SComponentModel, IComponentModel>();
            var answerParser = componentModel.GetService<IMarkdownParser>();

            return new AnswerDialogContent(
                answerParser,
                additionalCommandContainer,
                timeline,
                answer,
                isInProgress
                );
        }
    }
}