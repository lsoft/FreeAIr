using RunProcessAsTask;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Helpers for launching and terminating external processes (such as MCP server proxies and
    /// CLI tools invoked by FreeAIr) without crashing the extension when the process misbehaves.
    /// </summary>
    public static class ProcessHelper
    {
        /// <summary>
        /// Kills the process and swallows any exception, so callers can clean up a possibly
        /// already-exited or inaccessible process without needing their own try/catch.
        /// </summary>
        public static void SafelyKill(
            this System.Diagnostics.Process process
            )
        {
            try
            {
                process.Kill();
            }
            catch (Exception excp)
            {
                //nothing to do
            }
        }


        /// <summary>
        /// Runs an executable with no visible console window, capturing its UTF-8 stdout/stderr.
        /// Used for invoking helper CLIs (such as git or MCP server processes) in the background.
        /// </summary>
        public static Task<ProcessResults> RunSilentlyAsync(
            string workingDirectory,
            string exeName,
            string arguments,
            CancellationToken cancellationToken
            )
        {
            return Run(
                    new ProcessStartInfo
                    {
                        WorkingDirectory = workingDirectory,
                        FileName = exeName,
                        Arguments = arguments,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    },
                    cancellationToken
                    );
        }

        /// <summary>
        /// Starts a process from the given <see cref="ProcessStartInfo"/> and awaits its exit as
        /// a <see cref="Task"/>, forwarding cancellation to terminate the process early.
        /// </summary>
        public static Task<ProcessResults> Run(
            ProcessStartInfo psi,
            CancellationToken cancellationToken
            )
        {
            return 
                ProcessEx.RunAsync(
                    psi,
                    cancellationToken
                    );
        }
    }
}
