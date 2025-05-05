using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class AnswerHelper
    {
        private static readonly Regex _removeCodeBlockRegex = new Regex(
            @"```\S*"
            );

        public static string CleanupFromQuotes(
            this string answer
            )
        {
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

            return answer;
        }
    }
}
