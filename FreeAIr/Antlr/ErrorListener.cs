using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeAIr.Antlr
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
