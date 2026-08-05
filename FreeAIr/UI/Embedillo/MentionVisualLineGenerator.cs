using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace FreeAIr.UI.Embedillo
{
    /// <summary>
    /// Creates a <see cref="MentionVisualLineGenerator"/> for the AvalonEdit editor that renders it is
    /// attached to. Used by DI/MEF wiring so each editor instance gets its own generator with its own
    /// suggestion state, rather than sharing a single generator across editors.
    /// </summary>
    public interface IMentionVisualLineGeneratorFactory
    {
        /// <summary>
        /// Builds a new mention generator instance ready to be attached to an AvalonEdit text view.
        /// </summary>
        MentionVisualLineGenerator Create(
            );
    }

    /// <summary>
    /// Base AvalonEdit <see cref="VisualLineElementGenerator"/> that recognizes "@mention"/"#file"-style
    /// anchors (file references, code identifiers, etc.) typed in the chat editor and replaces the matched
    /// text with an inline suggestion control, while ignoring anchors that appear inside fenced or inline
    /// code blocks.
    /// </summary>
    public abstract class MentionVisualLineGenerator : VisualLineElementGenerator
    {
        /// <summary>
        /// Regex built from <see cref="AnchorSymbol"/> that matches an anchor character followed by the
        /// identifier characters (letters, digits, path separators, etc.) that make up a mention.
        /// </summary>
        private readonly Regex _targetRegex;

        /// <summary>
        /// Matches fenced code blocks (triple backtick) so their contents can be masked out before anchor
        /// detection runs, preventing mentions from being recognized inside pasted code.
        /// </summary>
        private static readonly Regex _threeSlashRegex = new Regex(
            @"\`\`\`[\s\S]*?\`\`\`",
            RegexOptions.Compiled
            );

        /// <summary>
        /// Matches inline code spans (single backtick) so their contents can be masked out before anchor
        /// detection runs, preventing mentions from being recognized inside inline code.
        /// </summary>
        private static readonly Regex _oneSlashRegex = new Regex(
            @"\`[\s\S]*?\`",
            RegexOptions.Compiled
            );

        /// <summary>
        /// The character that introduces a mention in the editor text, such as '@' or '#'.
        /// </summary>
        private readonly char _anchorSymbol;

        /// <summary>
        /// The character that introduces a mention recognized by this generator.
        /// </summary>
        public char AnchorSymbol => _anchorSymbol;

        /// <summary>
        /// Builds the anchor-matching regex for the given trigger character so the generator can find and
        /// render mentions that start with it.
        /// </summary>
        public MentionVisualLineGenerator(
            char anchorSymbol
            )
        {
            _anchorSymbol = anchorSymbol;

            _targetRegex = new Regex(
                @"(?<!\S)" + anchorSymbol + @"([\p{L}\p{M}0-9_:\\.@\-~\[\]]+)"
                );
        }

        /// <summary>
        /// Finds the offset of the next mention anchor at or after <paramref name="startOffset"/> in the
        /// document, so AvalonEdit knows where this generator needs to take over rendering.
        /// </summary>
        public override int GetFirstInterestedOffset(int startOffset)
        {
            var document = CurrentContext.TextView.Document;
            var text = document.Text;

            text = PrepareTextForSearchingAnchor(text);

            var targetMatch = _targetRegex.Match(text, startOffset);
            return targetMatch.Success ? targetMatch.Index : -1;
        }

        /// <summary>
        /// Masks out fenced and inline code blocks in <paramref name="text"/> with '*' filler so anchor
        /// detection never fires on mention-like text pasted inside code, while keeping the string length
        /// (and therefore all offsets) unchanged.
        /// </summary>
        public static string PrepareTextForSearchingAnchor(string text)
        {
            //не обрабатывать anchor внутри
            //```
            //тут
            //```
            foreach (Match match in _threeSlashRegex.Matches(text))
            {
                foreach (var capture in match.Captures.OrderBy(c => c.Index))
                {
                    text = text.Substring(0, capture.Index)
                        + new string('*', capture.Length)
                        + text.Substring(capture.Index + capture.Length)
                        ;
                }
            }

            //не обрабатывать anchor внутри `тут`
            foreach (Match match in _oneSlashRegex.Matches(text))
            {
                foreach (var capture in match.Captures.OrderBy(c => c.Index))
                {
                    text = text.Substring(0, capture.Index)
                        + new string('*', capture.Length)
                        + text.Substring(capture.Index + capture.Length)
                        ;
                }
            }

            return text;
        }

        /// <summary>
        /// Builds the inline visual element that replaces a recognized mention at <paramref name="offset"/>
        /// with the suggestion control returned by <see cref="CreateControl(string)"/>, or returns null when
        /// the offset does not sit exactly at the start of a mention match.
        /// </summary>
        public override VisualLineElement? ConstructElement(int offset)
        {
            var document = CurrentContext.TextView.Document;
            var text = document.Text;

            var remainingText = text.Substring(offset);
            var match = _targetRegex.Match(remainingText);

            if (!match.Success || match.Index != 0)
            {
                return null;
            }

            var mentionText = _anchorSymbol + match.Groups[1].Value;

            var control = CreateControl(mentionText);

            var element = new FixedInlineObjectElement(
                mentionText.Length,
                control
                );

            return element;
        }

        /// <summary>
        /// Creates the WPF control shown in place of the matched mention text, e.g. a chip displaying the
        /// referenced file or symbol name.
        /// </summary>
        protected abstract UIElement CreateControl(string mentionText);

        /// <summary>
        /// Retrieves the list of candidates (files, symbols, etc.) that can be inserted after this
        /// generator's anchor symbol, used to populate the mention autocomplete popup.
        /// </summary>
        public abstract System.Threading.Tasks.Task<List<ISuggestion>> GetSuggestionsAsync();

        /// <summary>
        /// Parses the raw text following the anchor symbol into a strongly typed mention part that can be
        /// stored in the chat message and later resolved back into a suggestion.
        /// </summary>
        public abstract IParsedPart? CreatePart(string partPayload);

        #region private classes

        /// <summary>
        /// Исправленная версия контрола. Теперь контрол правильно выравнивается по вертикали.
        /// </summary>
        public class FixedInlineObjectElement : InlineObjectElement
        {
            /// <summary>
            /// Wraps the given element as a fixed inline object spanning <paramref name="documentLength"/>
            /// characters of the mention text.
            /// </summary>
            public FixedInlineObjectElement(int documentLength, UIElement element)
                : base(documentLength, element)
            {
            }

            /// <summary>
            /// Produces the text run for this inline element, substituting a <see cref="FixedInlineObjectRun"/>
            /// so the baseline correction in <see cref="FixedInlineObjectRun.Format"/> applies.
            /// </summary>
            /// <inheritdoc/>
            public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
            {
                var run = base.CreateTextRun(startVisualColumn, context);

                return new FixedInlineObjectRun(
                    run.Length,
                    run.Properties,
                    this.Element
                    );
            }

        }

        /// <summary>
        /// Inline object run used by mention elements to correct the vertical alignment (baseline) of the
        /// embedded suggestion control so it lines up with surrounding text instead of sitting too high.
        /// </summary>
        private class FixedInlineObjectRun : InlineObjectRun
        {
            /// <summary>
            /// Creates the run wrapping the given inline element with the specified length and text
            /// formatting properties.
            /// </summary>
            public FixedInlineObjectRun(int length, TextRunProperties properties, UIElement element)
                : base(length, properties, element)
            {
            }

            /// <summary>
            /// Formats the embedded object and scales down its reported baseline so the control renders
            /// vertically centered instead of aligned to the default (too high) baseline.
            /// </summary>
            /// <inheritdoc/>
            public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
            {
                var result = base.Format(remainingParagraphWidth);
                return new TextEmbeddedObjectMetrics(
                    result.Width,
                    result.Height,
                    result.Baseline * 0.75
                    );
            }
        }

        #endregion
    }

    /// <summary>
    /// A single candidate offered in the mention autocomplete popup (a file, symbol, or other referenceable
    /// item), pairing the icon and text shown to the user with the underlying data stored in the message.
    /// </summary>
    public interface ISuggestion
    {
        /// <summary>
        /// Icon shown next to the suggestion in the autocomplete popup.
        /// </summary>
        ImageMoniker Image
        {
            get;
        }

        /// <summary>
        /// The complete data (e.g. full file path) that gets embedded into the mention when this suggestion
        /// is chosen.
        /// </summary>
        string FullData
        {
            get;
        }

        /// <summary>
        /// The shortened, human-readable text shown for this suggestion in the autocomplete popup.
        /// </summary>
        string PublicData
        {
            get;
        }
    }
}