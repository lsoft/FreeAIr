using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Agent
{
    //todo log here across all class body
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
                        Console.WriteLine($"Запускаем процесс: {_fileName} {_arguments}");
                        process.Start();

                        _ = await process.WaitForExitAsync(cancellationToken);

                        Console.WriteLine("Процесс завершён нештатно. Перезапуск...");

                        //чтобы не сразу перезапускать процесс и не ДДОСить ОС
                        await Task.Delay(1000);
                    }
                    catch (OperationCanceledException)
                    {
                        //that's ok
                        //kill the app
                        process.Kill();
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при выполнении процесса: {ex.Message}");

                        process.Kill();
                        break;
                    }

                }
            }

            Console.WriteLine("Мониторинг завершён.");
        }
    }
}
