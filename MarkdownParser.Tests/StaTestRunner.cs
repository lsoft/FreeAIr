namespace MarkdownParser.Tests
{
    /// <summary>
    /// Runs a test body on a single-threaded-apartment thread. xunit runs on the thread pool, which
    /// is MTA, and a few of the WPF types the answer renderer builds - buttons, image decoders -
    /// want an STA thread the way the Visual Studio UI thread is one. Rather than take a dependency
    /// on an STA-aware xunit attribute, the body is handed to a thread of our own and its exception
    /// is rethrown on the caller.
    /// </summary>
    internal static class StaTestRunner
    {
        /// <summary>Runs <paramref name="body"/> on a fresh STA thread and rethrows whatever it threw.</summary>
        public static void Run(
            Action body
            )
        {
            Exception? failure = null;

            var thread = new Thread(
                () =>
                {
                    try
                    {
                        body();
                    }
                    catch (Exception excp)
                    {
                        failure = excp;
                    }
                });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            thread.Join();

            if (failure is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo
                    .Capture(failure)
                    .Throw();
            }
        }
    }
}
