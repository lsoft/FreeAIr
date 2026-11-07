using System.Collections.Generic;

namespace FreeAIr.Llm.Budget
{
    /// <summary>
    /// Fills one portion of files for a batch request - the natural language outlines, the
    /// natural language search - up to a token budget, counting each file once however many
    /// times it is asked for.
    ///
    /// A file comes with the files it refers to, and those go into the context with it. In a
    /// portion which already holds something, a file goes in only together with all of them; if
    /// that does not fit, the portion is full and the file starts the next one. In an empty
    /// portion the file itself always goes in, even over the budget - there is nothing smaller to
    /// send - and of its related files as many as still fit.
    /// </summary>
    public sealed class PortionBudget
    {
        /// <summary>The keys of the parts already in the portion, so a shared reference is paid for once.</summary>
        private readonly HashSet<string> _keys = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>How many tokens the parts of one portion may take together.</summary>
        public int Budget
        {
            get;
        }

        /// <summary>How many tokens the parts in the portion take so far.</summary>
        public long Used
        {
            get;
            private set;
        }

        /// <summary>Whether nothing has been put into the portion yet.</summary>
        public bool IsEmpty => _keys.Count == 0;

        public PortionBudget(
            int budget
            )
        {
            Budget = budget;
        }

        /// <summary>
        /// Tries to put <paramref name="main"/> into the portion with <paramref name="related"/>.
        /// Returns false, changing nothing, when the portion is not empty and the lot does not
        /// fit. On success <paramref name="acceptedRelated"/> lists the related parts which were
        /// added - the ones not in the portion already and, in an empty portion, the ones which fit.
        /// </summary>
        public bool TryAdd(
            PortionPart main,
            IReadOnlyList<PortionPart> related,
            out IReadOnlyList<PortionPart> acceptedRelated
            )
        {
            if (main is null)
            {
                throw new ArgumentNullException(nameof(main));
            }
            if (related is null)
            {
                throw new ArgumentNullException(nameof(related));
            }

            var newRelated = new List<PortionPart>();
            var newKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in related)
            {
                if (!_keys.Contains(part.Key) && !string.Equals(part.Key, main.Key, StringComparison.OrdinalIgnoreCase) && newKeys.Add(part.Key))
                {
                    newRelated.Add(part);
                }
            }

            var mainCost = _keys.Contains(main.Key) ? 0 : main.Tokens;

            if (!IsEmpty)
            {
                //all or nothing: a file without its references is the half answer the portion
                //split exists to avoid
                long cost = mainCost;
                foreach (var part in newRelated)
                {
                    cost += part.Tokens;
                }

                if (Used + cost > Budget)
                {
                    acceptedRelated = Array.Empty<PortionPart>();
                    return false;
                }

                Add(main);
                newRelated.ForEach(Add);
                acceptedRelated = newRelated;
                return true;
            }

            //an empty portion: the file goes in regardless, its references while they fit
            Add(main);

            var accepted = new List<PortionPart>();
            foreach (var part in newRelated)
            {
                if (Used + part.Tokens > Budget)
                {
                    continue;
                }

                Add(part);
                accepted.Add(part);
            }

            acceptedRelated = accepted;
            return true;
        }

        /// <summary>Empties the portion for the next batch.</summary>
        public void Clear()
        {
            _keys.Clear();
            Used = 0;
        }

        private void Add(
            PortionPart part
            )
        {
            if (_keys.Add(part.Key))
            {
                Used += part.Tokens;
            }
        }
    }

    /// <summary>
    /// One file of a portion as <see cref="PortionBudget"/> sees it: a key telling it apart - the
    /// path - and what it costs in tokens as it will be sent.
    /// </summary>
    public sealed class PortionPart
    {
        /// <summary>What tells two parts apart; the same file asked for twice has the same key.</summary>
        public string Key
        {
            get;
        }

        /// <summary>The tokens the part takes in the request.</summary>
        public int Tokens
        {
            get;
        }

        public PortionPart(
            string key,
            int tokens
            )
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Tokens = tokens;
        }
    }
}
