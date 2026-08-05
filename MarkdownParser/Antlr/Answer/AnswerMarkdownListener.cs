using Antlr4.Runtime.Misc;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer
{
    /// <summary>
    /// Walks the ANTLR parse tree produced by <c>AnswerMarkdownParser</c> and feeds each recognized
    /// construct (headers, blockquotes, tables, code blocks, links, images, inline XML) into a
    /// <see cref="ParsedMarkdown"/> as WPF-ready blocks and parts.
    /// </summary>
    public class AnswerMarkdownListener : AnswerMarkdownBaseListener
    {
        private static readonly Regex UrlParseRegex = new Regex(
            @"\[([^\]]+)\]\(([^\)\s]+)(?:\s+\""([^\""]+)\"")?\)",
            RegexOptions.Compiled
            );
        private static readonly Regex XmlParseRegex = new Regex(
            @"\<([^>]+)\>([^\<]*)\<\/([^>]+)\>",
            RegexOptions.Compiled | RegexOptions.Multiline
            );
        private static readonly Regex ImgParseRegex = new Regex(
            @"\[([^\]]+)\]\(([^\)\s]+)(?:\s+\""([^\""]+)\"")?\)",
            RegexOptions.Compiled
            );

        private readonly ParsedMarkdown _answer;

        public AnswerMarkdownListener(
            ParsedMarkdown answer
            )
        {
            if (answer is null)
            {
                throw new ArgumentNullException(nameof(answer));
            }

            _answer = answer;
        }

        /// <summary>Starts a new blockquote block.</summary>
        public override void EnterBlockquote([NotNull] AnswerMarkdownParser.BlockquoteContext context)
        {
            _answer.AddBlockquoteBlock();

            base.EnterBlockquote(context);
        }

        /// <summary>Adds one raw markdown table row to the current table block.</summary>
        public override void EnterTable_row([NotNull] AnswerMarkdownParser.Table_rowContext context)
        {
            var text = context.GetText();
            _answer.AddTableRow(text);
        }

        /// <summary>Renders a `---` horizontal rule as a bordered WPF element and adds it as a block.</summary>
        public override void EnterHorizontal_rule([NotNull] AnswerMarkdownParser.Horizontal_ruleContext context)
        {
            var border = new Border
            {
                Margin = new Thickness(5, 5, 5, 5),
                BorderThickness = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                BorderBrush = System.Windows.Media.Brushes.Green,
            };

            var bc = new BlockUIContainer(border);

            _answer.AddHorizontalRuleBlock(
                bc
                );
        }

        /// <summary>Starts a new paragraph block.</summary>
        public override void EnterParagraph([NotNull] AnswerMarkdownParser.ParagraphContext context)
        {
            _answer.AddParagraphBlock();

            base.EnterParagraph(context);
        }

        /// <summary>Parses a `![desc](link "title")` image tag and adds it as an image part.</summary>
        public override void EnterImage([NotNull] AnswerMarkdownParser.ImageContext context)
        {
            var text = context.GetText();

            MatchCollection matches = ImgParseRegex.Matches(text);

            foreach (Match match in matches)
            {
                string description = match.Groups[1].Value;
                string link = match.Groups[2].Value;
                string title = match.Groups.Count > 2
                    ? match.Groups[3].Value
                    : string.Empty;

                _answer.AddImage(
                    text,
                    description,
                    link,
                    title
                    );
            }
        }

        /// <summary>Parses a bare `&lt;url-or-email&gt;` autolink, turning an `@`-containing body into a `mailto:` link.</summary>
        public override void EnterQuick_link([NotNull] AnswerMarkdownParser.Quick_linkContext context)
        {
            var text = context.GetText();
            var filtered = text.Trim('<', '>');

            string link;
            if (filtered.Contains("@"))
            {
                link = "mailto:" + filtered;
            }
            else
            {
                link = filtered;
            }

            _answer.AddUrl(
                text,
                filtered,
                link,
                filtered
                );
        }

        /// <summary>Appends a punctuation token as plain text.</summary>
        public override void EnterPunctuation([NotNull] AnswerMarkdownParser.PunctuationContext context)
        {
            var text = context.GetText();

            _answer.AddText(
                text
                );
        }

        /// <summary>Appends a plain word token as text.</summary>
        public override void EnterWord([NotNull] AnswerMarkdownParser.WordContext context)
        {
            var text = context.GetText();

            _answer.AddText(
                text
                );
        }

        /// <summary>Appends whitespace as text, preserving spacing between tokens.</summary>
        public override void EnterWhitespace([NotNull] AnswerMarkdownParser.WhitespaceContext context)
        {
            var text = context.GetText();

            _answer.AddText(
                text
                );
        }

        /// <summary>Parses an inline `&lt;tag&gt;body&lt;/tag&gt;` fragment; adds it as an XML node part when the tags match, otherwise falls back to plain text.</summary>
        public override void EnterXml_block([NotNull] AnswerMarkdownParser.Xml_blockContext context)
        {
            var text = context.GetText();

            MatchCollection matches = XmlParseRegex.Matches(text);

            foreach (Match match in matches)
            {
                string leftNodeName = match.Groups[1].Value;
                string body = match.Groups[2].Value;
                string rightNodeName = match.Groups[3].Value;

                if (leftNodeName == rightNodeName)
                {
                    _answer.AddXmlNode(
                        text,
                        leftNodeName,
                        body
                        );
                }
                else
                {
                    _answer.AddText(
                        text
                        );
                }
            }
        }

        /// <summary>Parses a `[desc](link "title")` markdown link and adds it as a URL part.</summary>
        public override void EnterUrl([NotNull] AnswerMarkdownParser.UrlContext context)
        {
            var urlBody = context.GetText();

            MatchCollection matches = UrlParseRegex.Matches(urlBody);

            foreach (Match match in matches)
            {
                string description = match.Groups[1].Value;
                string link = match.Groups[2].Value;
                string title = match.Groups.Count > 2
                    ? match.Groups[3].Value
                    : string.Empty;

                _answer.AddUrl(
                    urlBody,
                    description,
                    link,
                    title
                    );
            }
        }

        /// <summary>Adds a level-1 (`#`) header.</summary>
        public override void EnterH1([NotNull] AnswerMarkdownParser.H1Context context)
        {
            var text = context.GetText();

            _answer.AddHeader(
                1,
                text
                );
        }
        /// <summary>Adds a level-2 (`##`) header.</summary>
        public override void EnterH2([NotNull] AnswerMarkdownParser.H2Context context)
        {
            _answer.AddHeader(
                2,
                context.GetText()
                );
        }
        /// <summary>Adds a level-3 (`###`) header.</summary>
        public override void EnterH3([NotNull] AnswerMarkdownParser.H3Context context)
        {
            _answer.AddHeader(
                3,
                context.GetText()
                );
        }
        /// <summary>Adds a level-4 (`####`) header.</summary>
        public override void EnterH4([NotNull] AnswerMarkdownParser.H4Context context)
        {
            _answer.AddHeader(
                4,
                context.GetText()
                );
        }
        /// <summary>Adds a level-5 (`#####`) header.</summary>
        public override void EnterH5([NotNull] AnswerMarkdownParser.H5Context context)
        {
            _answer.AddHeader(
                5,
                context.GetText()
                );
        }
        /// <summary>Adds a level-6 (`######`) header.</summary>
        public override void EnterH6([NotNull] AnswerMarkdownParser.H6Context context)
        {
            _answer.AddHeader(
                6,
                context.GetText()
                );
        }

        /// <summary>Strips the fence lines from a fenced ` ```code``` ` block and adds the remaining body as a code block.</summary>
        public override void EnterCode_block([NotNull] AnswerMarkdownParser.Code_blockContext context)
        {
            var text = context.GetText();
            var lines = text.Split([Environment.NewLine, "\r", "\n"], StringSplitOptions.None);
            var code = string.Join(Environment.NewLine, lines.Skip(1).Take(lines.Length - 2));

            if (string.IsNullOrEmpty(code))
            {
                return;
            }

            _answer.AddCodeBlock(
                text,
                code
                );
        }

        /// <summary>Adds one line of code text to the current code block.</summary>
        public override void EnterCode_line([NotNull] AnswerMarkdownParser.Code_lineContext context)
        {
            _answer.AddCodeLine(
                context.GetText()
                );
        }

    }

}
