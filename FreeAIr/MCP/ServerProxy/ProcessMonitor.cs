using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using System.Diagnostics;
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
                        process.StartInfo.WorkingDirectory = _folderPath;
                        process.StartInfo.FileName = _fileName;
                        process.StartInfo.Arguments = _arguments;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.StartInfo.CreateNoWindow = true;

                        //todo replace Console.WriteLine with logging in the whole class
                        ActivityLogHelper.ActivityLogInformation($"Запускаем процесс: {_fileName} {_arguments}");
                        process.Start();

                        _ = await process.WaitForExitAsync(cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

                        ActivityLogHelper.ActivityLogWarning("Процесс завершён нештатно. Перезапуск...");

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
