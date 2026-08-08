using System.Text.RegularExpressions;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Cleans up raw chat model output before it is shown or used: normalizes line endings and
    /// strips the quoting, `&lt;think&gt;` blocks and code fences a model tends to wrap a short
    /// answer in (e.g. a generated commit message).
    /// </summary>
    public static class AnswerHelper
    {
        /// <summary>
        /// Matches a markdown code fence opener, e.g. ` ```csharp `, so it can be stripped from an
        /// answer.
        /// </summary>
        private static readonly Regex _removeCodeBlockRegex = new Regex(
            @"```\S*"
            );

        /// <summary>
        /// The opening tag of a reasoning model's `&lt;think&gt;` block.
        /// </summary>
        private const string ThinkStart = "<think>";
        /// <summary>
        /// The closing tag of a reasoning model's `&lt;think&gt;` block.
        /// </summary>
        private const string ThinkEnd = "</think>";

        /// <summary>
        /// Rewrites every line break in the answer to the given line ending, regardless of what mix
        /// of CR, LF or CRLF the model produced.
        /// </summary>
        public static string WithLineEnding(
            this string answer,
            string lineEnding
            )
        {
            var lines = answer
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                ;
            var result = string.Join(lineEnding, lines);
            return result;
        }

        /// <summary>
        /// Strips a leading `&lt;think&gt;...&lt;/think&gt;` reasoning block, then repeatedly trims
        /// surrounding quotes, stray line breaks and code fences until nothing more can be removed.
        /// Used to turn a chat model's raw answer into the bare text it was actually asked for, e.g.
        /// a commit message.
        /// </summary>
        public static string CleanupFromQuotesAndThinks(
            this string answer,
            string lineEnding
            )
        {
            var tsi = answer.IndexOf(ThinkStart);
            var tei = answer.IndexOf(ThinkEnd);
            if (tsi >= 0 && tei >= 0 && tei > tsi)
            {
                answer = answer.Remove(
                    tsi,
                    tei - tsi + ThinkEnd.Length
                    );
            }

            answer = string.Join(
                Environment.NewLine,
                answer.Split('\r', '\n')
                );

            while (!string.IsNullOrEmpty(answer))
            {
                if (answer.StartsWith("\""))
                {
                    answer = answer.Substring(1);
                }
                else if (answer.StartsWith("'"))
                {
                    answer = answer.Substring(1);
                }
                else if (answer.StartsWith("\r"))
                {
                    answer = answer.Substring(1);
                }
                else if (answer.StartsWith("\n"))
                {
                    answer = answer.Substring(1);
                }

                else if (answer.EndsWith("\""))
                {
                    answer = answer.Substring(0, answer.Length - 1);
                }
                else if (answer.EndsWith("'"))
                {
                    answer = answer.Substring(0, answer.Length - 1);
                }
                else if (answer.EndsWith("\r"))
                {
                    answer = answer.Substring(0, answer.Length - 1);
                }
                else if (answer.EndsWith("\n"))
                {
                    answer = answer.Substring(0, answer.Length - 1);
                }

                else if (answer.Contains("`"))
                {
                    answer = _removeCodeBlockRegex.Replace(
                        answer,
                        string.Empty
                        );
                    answer = answer.Replace("`", "");
                }
                else
                {
                    break;
                }
            }

            return answer.WithLineEnding(lineEnding);
        }
    }
}
