using Antlr4.Runtime;
using System.IO;

namespace MarkdownParser.Antlr
{
    public class ErrorListener<S> : ConsoleErrorListener<S>
    {
        public bool HadError
        {
            get;
            private set;
        }

        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            S offendingSymbol,
            int line,
            int col,
            string msg,
            RecognitionException e
            )
        {
            HadError = true;
            base.SyntaxError(output, recognizer, offendingSymbol, line, col, msg, e);
        }
    }
}
