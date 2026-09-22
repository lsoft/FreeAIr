namespace WpfHelpers
{
    /// <summary>
    /// Where the commands in this assembly report a failure they have already shown the user.
    ///
    /// A message box tells whoever is sitting in front of Visual Studio and nobody else. When a
    /// build is handed to a tester, "I pressed the button and got an error" is the whole report -
    /// the stack trace was in a dialog that is now closed - so the same exception goes to the log
    /// as well.
    ///
    /// A hook rather than a call to the activity log, because this assembly is netstandard2.0 and
    /// knows nothing about the IDE; <c>FreeAIrPackage</c> attaches <c>ActivityLogHelper</c> to it at
    /// startup. Unattached, reporting is a no-op.
    /// </summary>
    public static class CommandDiagnostics
    {
        /// <summary>The sink, or null when nobody is listening. Assigned once, at package startup.</summary>
        public static Action<Exception>? Sink
        {
            get;
            set;
        }

        /// <summary>
        /// Reports a command failure. Never throws: this runs inside the catch which is keeping an
        /// unhandled exception out of WPF's dispatcher, and throwing here would defeat it.
        /// </summary>
        public static void Report(
            Exception excp
            )
        {
            var sink = Sink;
            if (sink is null || excp is null)
            {
                return;
            }

            try
            {
                sink(excp);
            }
            catch (Exception)
            {
                //a broken sink must not become the failure the user sees
            }
        }
    }
}
