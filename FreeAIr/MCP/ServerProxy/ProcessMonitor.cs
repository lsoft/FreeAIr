using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace FreeAIr.McpServerProxy
{
    /// <summary>
    /// Keeps a child process alive: starts it, waits for it, and restarts it if it died on its own.
    /// A failed start is retried too, with a pause in between; the loop gives up only after
    /// <see cref="MaxConsecutiveFailures"/> failures in a row, which means the process is not going
    /// to run at all. Cancellation (Visual Studio shutting down) ends the loop as well, and in
    /// every case the process is killed on the way out.
    ///
    /// Standard input and output are redirected, because that is the JSON-RPC channel to
    /// `Proxy.exe`; standard error is drained separately and ends up in the activity log,
    /// which is the only way to see why the proxy refused to run.
    /// </summary>
    public sealed class ProcessMonitor
    {
        /// <summary>
        /// How many times in a row the process may fail to start before the monitoring gives up.
        /// A process which has managed to run resets the counter.
        /// </summary>
        private const int MaxConsecutiveFailures = 5;

        /// <summary>Pause before retrying the process after a failed start, so a persistently broken process does not hammer the OS with restart attempts.</summary>
        private static readonly TimeSpan RestartDelayAfterFailure = TimeSpan.FromSeconds(5);

        /// <summary>Working directory the process is started from.</summary>
        private readonly string _folderPath;
        /// <summary>Executable file name, resolved relative to <see cref="_folderPath"/>.</summary>
        private readonly string _fileName;
        /// <summary>Command-line arguments passed to the process, if any.</summary>
        private readonly string? _arguments;

        /// <summary>The currently running (or most recently started) child process; replaced with a new instance on every restart.</summary>
        public Process Process
        {
            get;
            private set;
        }

        /// <summary>
        /// Raised right after the process has been started, every time — including each restart.
        /// The subscriber gets the brand new <see cref="System.Diagnostics.Process"/>: the streams
        /// of the previous one are dead by then and have to be re-bound.
        /// </summary>
        public event Action<Process>? ProcessStarted;

        /// <summary>Configures the monitor with the child process's location and arguments; the process is not started until <see cref="StartMonitoringAsync"/> runs.</summary>
        public ProcessMonitor(
            string folderPath,
            string fileName,
            string? arguments = null
            )
        {
            if (folderPath is null)
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            _folderPath = folderPath;
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _arguments = arguments;
        }

        /// <summary>Runs the start/wait/restart loop described on the class, until cancelled or <see cref="MaxConsecutiveFailures"/> consecutive start failures occur.</summary>
        public async Task StartMonitoringAsync(
            CancellationToken cancellationToken = default
            )
        {
            var consecutiveFailures = 0;

            while (true)
            {
                Process = new Process();
                using (Process)
                {
                    try
                    {
                        var error = new StringBuilder();

                        Process.StartInfo.WorkingDirectory = _folderPath;
                        Process.StartInfo.FileName = System.IO.Path.Combine(_folderPath, _fileName);
                        Process.StartInfo.Arguments = _arguments;
                        Process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        Process.StartInfo.CreateNoWindow = true;

                        Process.StartInfo.UseShellExecute = false;
                        Process.StartInfo.RedirectStandardInput = true;
                        Process.StartInfo.RedirectStandardOutput = true;
                        Process.StartInfo.RedirectStandardError = true;
                        Process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                        {
                            error.Append(e.Data);
                        });

                        ActivityLogHelper.ActivityLogInformation($"Запускаем процесс: {_fileName} {_arguments}");
                        var startResult = Process.Start();

                        Process.BeginErrorReadLine();
                        //var output = await Process.StandardOutput.ReadToEndAsync();

                        ProcessStarted?.Invoke(Process);

                        _ = await Process.WaitForExitAsync(cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

                        //процесс худо-бедно, но отработал, счётчик неудачных запусков более не актуален
                        consecutiveFailures = 0;

                        var msg = new StringBuilder();
                        msg.AppendLine("Процесс завершён нештатно. Перезапуск...");
                        msg.AppendLine(new string('-', 80));
                        //msg.AppendLine("Output:");
                        //msg.AppendLine(output);
                        msg.AppendLine(new string('-', 80));
                        msg.AppendLine("Error:");
                        msg.AppendLine(error.ToString());
                        ActivityLogHelper.ActivityLogWarning(msg.ToString());

                        //чтобы не сразу перезапускать процесс и не ДДОСить ОС
                        await Task.Delay(1_000, cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        //that's ok
                        //kill the app
                        Process.SafelyKill();
                        break;
                    }
                    catch (Exception excp)
                    {
                        excp.ActivityLogException();

                        Process.SafelyKill();

                        consecutiveFailures++;
                        if (consecutiveFailures >= MaxConsecutiveFailures)
                        {
                            //проблема, судя по всему, не рассосётся сама: нет смысла дёргать ОС дальше
                            ActivityLog.LogError(
                                "FreeAIr",
                                $"Процесс {_fileName} не удалось запустить {consecutiveFailures} раз подряд. Мониторинг прекращён."
                                );
                            break;
                        }

                        try
                        {
                            //чтобы не ДДОСить ОС запусками, которые почти наверняка снова упадут
                            await Task.Delay(RestartDelayAfterFailure, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }

                }
            }

            ActivityLogHelper.ActivityLogInformation("Мониторинг завершён.");
        }
    }
}
