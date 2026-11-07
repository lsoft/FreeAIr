using System.Collections.Generic;

namespace FreeAIr.Llm.Budget
{
    /// <summary>
    /// Fits a chat into the model's context window by leaving out its oldest turns. Nothing is
    /// summarized or shortened: the system prompt, the tools and the turn being answered are
    /// always sent, and of the dialogue before it as many of the most recent turns as still fit.
    ///
    /// The unit is a whole turn - a prompt with everything that answered it - because a cut
    /// inside one leaves a tool result without its call or an answer without its question, and
    /// both protocols reject the first of these outright.
    /// </summary>
    public static class ContextWindowFitter
    {
        /// <summary>
        /// Chooses what to send. <paramref name="historyTurns"/> are the earlier turns, oldest
        /// first; <paramref name="currentTurn"/> is the turn being answered, with the context
        /// documents in front of its prompt. When even the mandatory part does not fit, it is
        /// sent anyway with no history - the server's refusal says more than silence would.
        /// </summary>
        public static ContextWindowFit Fit(
            string? systemPrompt,
            IReadOnlyList<LlmToolDefinition> tools,
            IReadOnlyList<IReadOnlyList<LlmMessage>> historyTurns,
            IReadOnlyList<LlmMessage> currentTurn,
            int inputBudget,
            TokenCounter counter
            )
        {
            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }
            if (historyTurns is null)
            {
                throw new ArgumentNullException(nameof(historyTurns));
            }
            if (currentTurn is null)
            {
                throw new ArgumentNullException(nameof(currentTurn));
            }
            if (counter is null)
            {
                throw new ArgumentNullException(nameof(counter));
            }

            //the part which goes out whatever the budget says
            var rawEstimate =
                TokenEstimator.EstimatePreamble(systemPrompt, tools)
                + TokenEstimator.EstimateMessages(currentTurn)
                ;
            var isOverBudget = counter.Scale(rawEstimate) > inputBudget;

            //then the turns before it, newest first, until the next one would not fit; an older
            //turn is never sent in place of a newer one, so the history stays contiguous
            var firstKeptTurn = historyTurns.Count;
            if (!isOverBudget)
            {
                for (var turnIndex = historyTurns.Count - 1; turnIndex >= 0; turnIndex--)
                {
                    var turnEstimate = TokenEstimator.EstimateMessages(historyTurns[turnIndex]);
                    if (counter.Scale(rawEstimate + turnEstimate) > inputBudget)
                    {
                        break;
                    }

                    rawEstimate += turnEstimate;
                    firstKeptTurn = turnIndex;
                }
            }

            var messages = new List<LlmMessage>();
            for (var turnIndex = firstKeptTurn; turnIndex < historyTurns.Count; turnIndex++)
            {
                messages.AddRange(historyTurns[turnIndex]);
            }
            messages.AddRange(currentTurn);

            return new ContextWindowFit(
                messages,
                historyTurns.Count - firstKeptTurn,
                firstKeptTurn,
                rawEstimate,
                counter.Scale(rawEstimate),
                isOverBudget
                );
        }
    }

    /// <summary>What <see cref="ContextWindowFitter"/> decided to send, and what it left out.</summary>
    public sealed class ContextWindowFit
    {
        /// <summary>The transcript to send: the kept turns, then the turn being answered.</summary>
        public IReadOnlyList<LlmMessage> Messages
        {
            get;
        }

        /// <summary>How many of the earlier turns are sent.</summary>
        public int KeptTurnCount
        {
            get;
        }

        /// <summary>How many of the oldest turns were left out to make the request fit.</summary>
        public int DroppedTurnCount
        {
            get;
        }

        /// <summary>
        /// The unscaled <see cref="TokenEstimator"/> guess for everything sent, preamble included.
        /// This is what the server's count is later divided by to calibrate the counter.
        /// </summary>
        public int RawEstimate
        {
            get;
        }

        /// <summary>The guess in the model's tokens, as calibrated so far.</summary>
        public int EstimatedTokens
        {
            get;
        }

        /// <summary>
        /// Whether the system prompt, the tools and the current turn alone are already over the
        /// budget, so that the request is likely to be refused however much history is cut.
        /// </summary>
        public bool IsOverBudget
        {
            get;
        }

        public ContextWindowFit(
            IReadOnlyList<LlmMessage> messages,
            int keptTurnCount,
            int droppedTurnCount,
            int rawEstimate,
            int estimatedTokens,
            bool isOverBudget
            )
        {
            Messages = messages ?? throw new ArgumentNullException(nameof(messages));
            KeptTurnCount = keptTurnCount;
            DroppedTurnCount = droppedTurnCount;
            RawEstimate = rawEstimate;
            EstimatedTokens = estimatedTokens;
            IsOverBudget = isOverBudget;
        }
    }
}
