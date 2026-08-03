using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeAIr.Grep
{
    /// <summary>
    /// The `*.cs;*.xaml` kind of filter, i.e. the only kind a model ever writes on its own.
    ///
    /// A mask without a directory separator is matched against the file name alone, so that the
    /// obvious `*.cs` means what it looks like; a mask which carries one is matched against the
    /// path relative to the solution. `*` and `?` are the only metacharacters, and `*` crosses
    /// directories - `Search\*.cs` finds `Search\Find\RagShortlist.cs`.
    ///
    /// A mask which starts with `!` subtracts instead of adding: `*.cs;!*.Designer.cs` is the whole
    /// point of this being more than a list. Generated code is what a search over a solution drowns
    /// in, and a budget of a hundred matches is spent on it long before the answer is reached.
    /// </summary>
    public sealed class FileMask
    {
        /// <summary>The filter which lets every file through, i.e. an unspecified mask.</summary>
        public static readonly FileMask MatchEverything = new FileMask(
            MaskGroup.Empty,
            MaskGroup.Empty
            );

        private readonly MaskGroup _included;
        private readonly MaskGroup _excluded;

        public bool MatchesEverything => _included.IsEmpty && _excluded.IsEmpty;

        private FileMask(
            MaskGroup included,
            MaskGroup excluded
            )
        {
            _included = included;
            _excluded = excluded;
        }

        /// <summary>
        /// Reads a `;` or `,` separated list of masks. An empty text is not an error: it is the
        /// filter which matches everything.
        /// </summary>
        public static FileMask Parse(
            string? masks
            )
        {
            if (string.IsNullOrWhiteSpace(masks))
            {
                return MatchEverything;
            }

            var included = new MaskGroupBuilder();
            var excluded = new MaskGroupBuilder();

            foreach (var part in masks!.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var mask = part.Trim();

                var isExclusion = mask.StartsWith("!", StringComparison.Ordinal);
                if (isExclusion)
                {
                    mask = mask.Substring(1).Trim();
                }

                if (mask.Length == 0)
                {
                    continue;
                }

                (isExclusion ? excluded : included).Add(mask);
            }

            if (included.IsEmpty && excluded.IsEmpty)
            {
                return MatchEverything;
            }

            return new FileMask(included.Build(), excluded.Build());
        }

        /// <summary>
        /// Whether the file passes the filter. <paramref name="relativePath"/> is the path against
        /// the root of the search; both separators are understood.
        ///
        /// An exclusion always wins, and a filter made of exclusions alone means everything else.
        /// </summary>
        public bool IsMatch(
            string relativePath
            )
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return false;
            }

            if (MatchesEverything)
            {
                return true;
            }

            var path = relativePath.Replace('/', '\\');

            var separator = path.LastIndexOf('\\');
            var fileName = separator >= 0
                ? path.Substring(separator + 1)
                : path
                ;

            if (_excluded.IsMatch(path, fileName))
            {
                return false;
            }

            if (_included.IsEmpty)
            {
                return true;
            }

            return _included.IsMatch(path, fileName);
        }

        /// <summary>
        /// One side of the filter: the masks about the whole path and the masks about the file name
        /// are kept apart, because they are matched against different texts.
        /// </summary>
        private sealed class MaskGroup
        {
            public static readonly MaskGroup Empty = new MaskGroup(
                Array.Empty<Regex>(),
                Array.Empty<Regex>()
                );

            private readonly Regex[] _nameMasks;
            private readonly Regex[] _pathMasks;

            public bool IsEmpty => _nameMasks.Length == 0 && _pathMasks.Length == 0;

            public MaskGroup(
                Regex[] nameMasks,
                Regex[] pathMasks
                )
            {
                _nameMasks = nameMasks;
                _pathMasks = pathMasks;
            }

            public bool IsMatch(
                string path,
                string fileName
                )
            {
                foreach (var pathMask in _pathMasks)
                {
                    if (pathMask.IsMatch(path))
                    {
                        return true;
                    }
                }

                foreach (var nameMask in _nameMasks)
                {
                    if (nameMask.IsMatch(fileName))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class MaskGroupBuilder
        {
            private readonly List<Regex> _nameMasks = new List<Regex>();
            private readonly List<Regex> _pathMasks = new List<Regex>();

            public bool IsEmpty => _nameMasks.Count == 0 && _pathMasks.Count == 0;

            public void Add(
                string mask
                )
            {
                var isPathMask = mask.IndexOf('\\') >= 0 || mask.IndexOf('/') >= 0;

                var regex = new Regex(
                    ConvertToRegexBody(mask, isPathMask),
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                    );

                if (isPathMask)
                {
                    _pathMasks.Add(regex);
                }
                else
                {
                    _nameMasks.Add(regex);
                }
            }

            public MaskGroup Build()
            {
                return new MaskGroup(
                    _nameMasks.ToArray(),
                    _pathMasks.ToArray()
                    );
            }
        }

        private static string ConvertToRegexBody(
            string mask,
            bool isPathMask
            )
        {
            var body = new StringBuilder(mask.Length * 2);
            body.Append('^');

            foreach (var c in mask)
            {
                switch (c)
                {
                    case '*':
                        body.Append(".*");
                        break;
                    case '?':
                        body.Append('.');
                        break;
                    case '/':
                    case '\\':
                        //the caller has normalized the path it matches against, so both spellings
                        //of a separator have to end up as the same one here
                        body.Append("\\\\");
                        break;
                    default:
                        body.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }

            body.Append('$');

            if (isPathMask)
            {
                //a path mask which does not start at the root is still meant to match: `src\*.cs`
                //is how a model spells "somewhere under a src folder"
                body.Insert(1, "(?:.*\\\\)?");
            }

            return body.ToString();
        }
    }
}
