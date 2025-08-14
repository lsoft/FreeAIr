using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using MarkdownParser.Antlr.Answer.Blocks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer
{
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

        public override void EnterBlockquote([NotNull] AnswerMarkdownParser.BlockquoteContext context)
        {
            _answer.AddBlockquoteBlock();

            base.EnterBlockquote(context);
        }

        public override void EnterTable_row([NotNull] AnswerMarkdownParser.Table_rowContext context)
        {
            var text = context.GetText();
            _answer.AddTableRow(text);
        }

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

        public override void EnterParagraph([NotNull] AnswerMarkdownParser.ParagraphContext context)
        {
            _answer.AddParagraphBlock();

            base.EnterParagraph(context);
        }

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

        public override void EnterPunctuation([NotNull] AnswerMarkdownParser.PunctuationContext context)
        {
            var text = context.GetText();

            _answer.AddText(
                text
                );
        }

        public override void EnterWord([NotNull] AnswerMarkdownParser.WordContext context)
        {
            var text = context.GetText();

            _answer.AddText(
                text
                );
        }

        public override void EnterWhitespace([NotNull] AnswerMarkdownParser.WhitespaceContext context)
        {
            var text = context.GetText();

            _answer.AddText(
                text
                );
        }

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

        public override void EnterH1([NotNull] AnswerMarkdownParser.H1Context context)
        {
            var text = context.GetText();

            _answer.AddHeader(
                1,
                text
                );
        }
        public override void EnterH2([NotNull] AnswerMarkdownParser.H2Context context)
        {
            _answer.AddHeader(
                2,
                context.GetText()
                );
        }
        public override void EnterH3([NotNull] AnswerMarkdownParser.H3Context context)
        {
            _answer.AddHeader(
                3,
                context.GetText()
                );
        }
        public override void EnterH4([NotNull] AnswerMarkdownParser.H4Context context)
        {
            _answer.AddHeader(
                4,
                context.GetText()
                );
        }
        public override void EnterH5([NotNull] AnswerMarkdownParser.H5Context context)
        {
            _answer.AddHeader(
                5,
                context.GetText()
                );
        }
        public override void EnterH6([NotNull] AnswerMarkdownParser.H6Context context)
        {
            _answer.AddHeader(
                6,
                context.GetText()
                );
        }

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

        public override void EnterCode_line([NotNull] AnswerMarkdownParser.Code_lineContext context)
        {
            _answer.AddCodeLine(
                context.GetText()
                );
        }

    }

}
