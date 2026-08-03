using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeAIr.Grep
{
    /// <summary>
    /// The knobs of a text search. A plain object rather than the settings file or the arguments of
    /// a tool call, so that the matching can be exercised without either.
    /// </summary>
    public sealed class GrepOptions
    {
        /// <summary>What to look for: a literal text, or a .NET regular expression when
        /// <see cref="UseRegularExpression"/> is set.</summary>
        public string Pattern
        {
            get;
            set;
        } = string.Empty;

        public bool UseRegularExpression
        {
            get;
            set;
        }

        public bool CaseSensitive
        {
            get;
            set;
        }

        /// <summary>Wraps the pattern in word boundaries, i.e. `Chat` stops matching `ChatWindow`.</summary>
        public bool WholeWord
        {
            get;
            set;
        }

        /// <summary>
        /// Reports the lines which do <b>not</b> match, the way `grep -v` does. The rest of the
        /// search is unchanged: the same files are read and the same budget applies, so this is
        /// worth pointing at a file or a mask rather than at a whole solution - nearly every line
        /// of it fails to match nearly every pattern.
        /// </summary>
        public bool InvertMatch
        {
            get;
            set;
        }

        /// <summary>How many lines around a match are reported with it. Zero is the default.</summary>
        public int ContextLineCount
        {
            get;
            set;
        }

        /// <summary>
        /// The most matching lines the search reports. This is a budget of tokens rather than of
        /// time: the answer goes to the model, and an unbounded one both costs money and pushes
        /// everything else out of the context.
        /// </summary>
        public int MaxMatchCount
        {
            get;
            set;
        } = 100;

        /// <summary>
        /// Longer lines are reported cut down to a window around the match. Minified javascript and
        /// generated code are single lines of a hundred kilobytes, and one of them is enough to
        /// blow up an answer.
        /// </summary>
        public int MaxLineLength
        {
            get;
            set;
        } = 400;

        /// <summary>
        /// How long the regular expression is allowed to chew on a single line. The pattern comes
        /// from a model, so catastrophic backtracking is not a hypothetical - and this runs inside
        /// Visual Studio, where a hung thread is the user's whole IDE.
        /// </summary>
        public TimeSpan RegexTimeout
        {
            get;
            set;
        } = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// One matching line, with the lines around it when they were asked for.
    /// </summary>
    public sealed class GrepMatch
    {
        /// <summary>Path of the file relative to the root of the search.</summary>
        public string RelativePath
        {
            get;
        }

        /// <summary>One based, as every editor counts them.</summary>
        public int LineNumber
        {
            get;
        }

        /// <summary>The matching line, possibly cut down to a window around the match.</summary>
        public string Line
        {
            get;
        }

        public IReadOnlyList<string> ContextBefore
        {
            get;
        }

        public IReadOnlyList<string> ContextAfter
        {
            get;
        }

        public GrepMatch(
            string relativePath,
            int lineNumber,
            string line,
            IReadOnlyList<string> contextBefore,
            IReadOnlyList<string> contextAfter
            )
        {
            RelativePath = relativePath;
            LineNumber = lineNumber;
            Line = line;
            ContextBefore = contextBefore;
            ContextAfter = contextAfter;
        }
    }

    /// <summary>
    /// What a search has found and what it had to give up on. The counters are part of the answer:
    /// a model which is told that the limit was reached asks a narrower question, while a model
    /// which is given a silently truncated list concludes that there is nothing else.
    /// </summary>
    public sealed class GrepSearchResult
    {
        private readonly List<GrepMatch> _matches;
        private readonly int _maxMatchCount;

        public IReadOnlyList<GrepMatch> Matches => _matches;

        public int FilesScanned
        {
            get;
            private set;
        }

        public int FilesWithMatches
        {
            get;
            private set;
        }

        /// <summary>
        /// Files whose regular expression ran out of time. Reported rather than thrown: one
        /// pathological file must not lose the matches found in the others.
        /// </summary>
        public int TimedOutFileCount
        {
            get;
            private set;
        }

        /// <summary>The search stopped because <see cref="GrepOptions.MaxMatchCount"/> was hit.</summary>
        public bool LimitReached
        {
            get;
            private set;
        }

        public GrepSearchResult(
            int maxMatchCount
            )
        {
            _matches = new List<GrepMatch>();
            _maxMatchCount = Math.Max(1, maxMatchCount);
        }

        public void NoteFileScanned()
        {
            FilesScanned++;
        }

        public void NoteFileMatched()
        {
            FilesWithMatches++;
        }

        public void NoteFileTimedOut()
        {
            TimedOutFileCount++;
        }

        /// <summary>
        /// Takes the match in unless the budget is spent. Returns false once it is, which is the
        /// signal for the caller to stop walking files.
        /// </summary>
        public bool TryAddMatch(
            GrepMatch match
            )
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            if (_matches.Count >= _maxMatchCount)
            {
                LimitReached = true;
                return false;
            }

            _matches.Add(match);

            if (_matches.Count >= _maxMatchCount)
            {
                LimitReached = true;
            }

            return true;
        }
    }

    /// <summary>
    /// The matching itself: text goes in, matching lines come out. It knows nothing about files,
    /// solutions or Visual Studio - the caller reads the text however it can (from the disk, or
    /// from an editor buffer which has not been saved yet) and feeds it here.
    ///
    /// A literal search is the same code path as a regular expression, only escaped: one path is
    /// one set of bugs.
    /// </summary>
    public sealed class GrepEngine
    {
        private readonly GrepOptions _options;
        private readonly Regex _regex;
        private readonly int _contextLineCount;
        private readonly int _maxLineLength;

        public GrepEngine(
            GrepOptions options
            )
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.Pattern))
            {
                throw new ArgumentException($"'{nameof(options.Pattern)}' cannot be null or empty.", nameof(options));
            }

            _options = options;
            _contextLineCount = Math.Max(0, options.ContextLineCount);
            _maxLineLength = Math.Max(40, options.MaxLineLength);

            var body = options.UseRegularExpression
                ? options.Pattern
                : Regex.Escape(options.Pattern)
                ;

            if (options.WholeWord)
            {
                body = "\\b(?:" + body + ")\\b";
            }

            var regexOptions = RegexOptions.CultureInvariant;
            if (!options.CaseSensitive)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            //a broken pattern throws ArgumentException from here, which the caller turns into a
            //failed tool call: the model wrote it and the model can fix it
            _regex = new Regex(body, regexOptions, options.RegexTimeout);
        }

        public GrepSearchResult CreateResult()
        {
            return new GrepSearchResult(_options.MaxMatchCount);
        }

        /// <summary>
        /// Looks through one file's text. Returns false when the result is full and there is no
        /// point in reading the next file.
        /// </summary>
        public bool SearchIn(
            string relativePath,
            string text,
            GrepSearchResult result
            )
        {
            if (relativePath is null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.LimitReached)
            {
                return false;
            }

            result.NoteFileScanned();

            var lines = TextFile.SplitLines(text);
            var matched = false;

            try
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    var match = _regex.Match(lines[i]);

                    //an inverted search wants exactly the lines the ordinary one throws away
                    if (match.Success == _options.InvertMatch)
                    {
                        continue;
                    }

                    matched = true;

                    var added = result.TryAddMatch(
                        new GrepMatch(
                            relativePath,
                            i + 1,
                            //there is no match to keep in view when the line is here for not
                            //having one, so a long one is cut from its head
                            Shorten(lines[i], _options.InvertMatch ? 0 : match.Index),
                            TakeContext(lines, i - _contextLineCount, i),
                            TakeContext(lines, i + 1, i + 1 + _contextLineCount)
                            )
                        );

                    if (!added)
                    {
                        break;
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                //one file which makes the pattern misbehave must not lose what the others found
                result.NoteFileTimedOut();
            }

            if (matched)
            {
                result.NoteFileMatched();
            }

            return !result.LimitReached;
        }

        private IReadOnlyList<string> TakeContext(
            List<string> lines,
            int fromInclusive,
            int toExclusive
            )
        {
            if (_contextLineCount == 0)
            {
                return Array.Empty<string>();
            }

            var from = Math.Max(0, fromInclusive);
            var to = Math.Min(lines.Count, toExclusive);
            if (from >= to)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>(to - from);
            for (var i = from; i < to; i++)
            {
                result.Add(Shorten(lines[i], 0));
            }

            return result;
        }

        /// <summary>
        /// Cuts a long line down to a window around the match rather than to its head: on a
        /// generated single line file the head is never the interesting part.
        /// </summary>
        private string Shorten(
            string line,
            int matchIndex
            )
        {
            line = line.TrimEnd();

            if (line.Length <= _maxLineLength)
            {
                return line;
            }

            var start = Math.Max(0, matchIndex - (_maxLineLength / 3));
            var length = Math.Min(_maxLineLength, line.Length - start);

            var window = line.Substring(start, length);

            if (start > 0)
            {
                window = "..." + window;
            }

            if (start + length < line.Length)
            {
                window += "...";
            }

            return window;
        }
    }
}
