namespace FreeAIr.Llm
{
    /// <summary>
    /// Where this assembly reports the things it recovered from rather than failed on: a tool
    /// schema which had to be repaired, a call whose arguments would not parse, a fragment of the
    /// stream which was not what the protocol promises.
    ///
    /// None of these are worth failing a turn over - the request still goes out and the answer
    /// still arrives - and every one of them is a plausible reason for a tester's report of
    /// "the tool ran with nothing in it". Without a line in the log they are invisible.
    ///
    /// A hook rather than a call to Visual Studio's activity log, because this assembly is
    /// netstandard2.0 and knows nothing about the IDE; <c>FreeAIrPackage</c> attaches
    /// <c>ActivityLogHelper</c> to it at startup. Unattached - in the tests, say - reporting is a
    /// no-op.
    /// </summary>
    public static class LlmDiagnostics
    {
        /// <summary>
        /// The sink, or null when nobody is listening. Assigned once, at package startup; nothing
        /// here reads it more than once per report, so a late attach loses only what came before it.
        /// </summary>
        public static Action<string>? Sink
        {
            get;
            set;
        }

        /// <summary>
        /// Reports one recovered problem. Never throws: a failure to log must not become the
        /// failure the user sees, and this is called from the middle of a streaming answer.
        /// </summary>
        public static void Report(
            string message
            )
        {
            var sink = Sink;
            if (sink is null)
            {
                return;
            }

            try
            {
                sink(message);
            }
            catch (Exception)
            {
                //a broken sink is not worth taking a turn down for
            }
        }

        /// <summary>Reports a recovered problem together with the exception which revealed it.</summary>
        public static void Report(
            string message,
            Exception excp
            )
        {
            Report(message + " " + $"({excp.GetType().Name}) {excp.Message}");
        }
    }
}
