using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace FreeAIr.McpServerProxy
{
    public sealed class ProcessMonitor
    {
        private readonly string _folderPath;
        private readonly string _fileName;
        private readonly string? _arguments;

        public Process Process
        {
            get;
            private set;
        }

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

        public async Task StartMonitoringAsync(
            CancellationToken cancellationToken = default
            )
        {
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

                        _ = await Process.WaitForExitAsync(cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

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
                        break;
                    }

                }
            }

            ActivityLogHelper.ActivityLogInformation("Мониторинг завершён.");
        }
    }
}
