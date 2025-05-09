﻿using System.Text.RegularExpressions;

namespace FreeAIr.Helper
{
    public static class AnswerHelper
    {
        private static readonly Regex _removeCodeBlockRegex = new Regex(
            @"```\S*"
            );

        private const string ThinkStart = "<think>";
        private const string ThinkEnd = "</think>";

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
