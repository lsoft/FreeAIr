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
                using (var process = new Process())
                {
                    try
                    {
                        var error = new StringBuilder();

                        process.StartInfo.WorkingDirectory = _folderPath;
                        process.StartInfo.FileName = System.IO.Path.Combine(_folderPath, _fileName);
                        process.StartInfo.Arguments = _arguments;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.StartInfo.CreateNoWindow = true;

                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                        {
                            error.Append(e.Data);
                        });

                        ActivityLogHelper.ActivityLogInformation($"Запускаем процесс: {_fileName} {_arguments}");
                        var startResult = process.Start();

                        process.BeginErrorReadLine();
                        var output = await process.StandardOutput.ReadToEndAsync();

                        _ = await process.WaitForExitAsync(cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

                        var msg = new StringBuilder();
                        msg.AppendLine("Процесс завершён нештатно. Перезапуск...");
                        msg.AppendLine(new string('-', 80));
                        msg.AppendLine("Output:");
                        msg.AppendLine(output);
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
                        process.SafelyKill();
                        break;
                    }
                    catch (Exception excp)
                    {
                        excp.ActivityLogException();

                        process.SafelyKill();
                        break;
                    }

                }
            }

            ActivityLogHelper.ActivityLogInformation("Мониторинг завершён.");
        }
    }
}
