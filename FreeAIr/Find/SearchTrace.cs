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
        private static readonly NonDisposableSemaphoreSlim _paneSemaphore = new(1, 1);
        private static readonly object _chainLocker = new();

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

        public static void Fail(
            string message,
            Exception excp
            )
        {
            Write("!!! " + message + ": " + excp.Message);

            excp.ActivityLogException(message);
        }

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
