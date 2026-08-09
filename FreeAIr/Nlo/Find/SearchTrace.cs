using FreeAIr.BLogic;
using FreeAIr.Helper;
//`Task<T>` needs this: FreeAIrPackage.cs declares a global `Task` alias which shadows the generic one
using System.Threading.Tasks;

namespace FreeAIr.Find
{
    /// <summary>
    /// The natural language search walks through three pickers, a tool window, a chat and,
    /// optionally, the embedding index, and every one of those steps is entitled to decide that
    /// there is nothing to do. Each of those decisions used to be a bare `return`: the button did
    /// nothing, said nothing, and left nothing behind to look at afterwards.
    ///
    /// Every step now writes itself into an output pane the user can keep open while searching, so
    /// that "the button does not work" is answered by reading one line. The steps which end the
    /// search early also say why, and repeat it into the activity log — that is the line a bug
    /// report needs.
    /// </summary>
    public static class SearchTrace
    {
        /// <summary>Serializes creation and lookup of the output pane so concurrent callers do not create it twice.</summary>
        private static readonly NonDisposableSemaphoreSlim _paneSemaphore = new(1, 1);
        /// <summary>Guards <see cref="_tail"/> so lines queued from different threads chain in a single order.</summary>
        private static readonly object _chainLocker = new();

        /// <summary>The lazily created output window pane the trace writes into.</summary>
        private static OutputWindowPane? _pane;

        /// <summary>
        /// Every line is appended after the previous one. The search reports itself from the UI
        /// thread and from two background ones, and lines which arrive in the wrong order are worse
        /// than no lines at all.
        /// </summary>
        private static Task _tail = Task.CompletedTask;

        /// <summary>
        /// A search has been asked for. Written apart so that the pane can be read as a list of
        /// runs rather than one endless stream.
        /// </summary>
        public static void Begin(
            string message
            )
        {
            Write("=== " + message);
        }

        /// <summary>
        /// One intermediate step of a running search, e.g. which scope or agent was chosen.
        /// </summary>
        public static void Step(
            string message
            )
        {
            Write("    " + message);
        }

        /// <summary>
        /// The search is over and the user has not got what was asked for.
        /// </summary>
        public static void Stop(
            string reason
            )
        {
            Write("!!! " + reason);

            ActivityLogHelper.ActivityLogWarning(
                "Natural language search stopped: " + reason
                );
        }

        /// <summary>
        /// The search has thrown. Reports both to the pane and to the activity log, since an
        /// unexpected exception is exactly the kind of failure a bug report needs the stack trace
        /// for.
        /// </summary>
        public static void Fail(
            string message,
            Exception excp
            )
        {
            Write("!!! " + message + ": " + excp.Message);

            excp.ActivityLogException(message);
        }

        /// <summary>Timestamps a line and queues it onto the tail chain so writes reach the pane in order.</summary>
        private static void Write(
            string message
            )
        {
            var line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + message;

            lock (_chainLocker)
            {
                _tail = AppendAfterAsync(_tail, line);
            }
        }

        /// <summary>Waits for the previous queued line, then writes this one; a failed prior write is swallowed so it cannot block later lines.</summary>
        private static async Task AppendAfterAsync(
            Task previous,
            string line
            )
        {
            try
            {
                await previous;
            }
            catch
            {
                //a line which failed to reach the pane must not take the next ones with it
            }

            try
            {
                var pane = await CreateOrGetPaneAsync();
                if (pane is null)
                {
                    return;
                }

                await pane.WriteLineAsync(line);
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>Returns the shared output pane, creating it under the "FreeAIr natural language search" name on first use.</summary>
        private static async Task<OutputWindowPane?> CreateOrGetPaneAsync(
            )
        {
            try
            {
                await _paneSemaphore.WaitAsync();

                if (_pane is null)
                {
                    _pane = await VS.Windows.CreateOutputWindowPaneAsync(
                        FreeAIr.Resources.Resources.FreeAIr_natural_language_search
                        );
                }
            }
            finally
            {
                _paneSemaphore.Release();
            }

            return _pane;
        }
    }
}
