using System.Diagnostics;

namespace Proxy
{
    public static class ParentProcessWatcher
    {
        public static void StartAsync()
        {
            var parentProcessId = GetParentProcessId();
            if (!parentProcessId.HasValue)
            {
                //todo log
                Environment.Exit(0);
            }

            Console.WriteLine($"Parent process id: {parentProcessId}");

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        var parentProcess = Process.GetProcessById(parentProcessId.Value);
                        await parentProcess.WaitForExitAsync();

                        //Console.WriteLine($"!!! EXIT !!!");
                    }
                    catch (Exception excp)
                    {
                        //todo log
                        Console.WriteLine($"!!! ERROR !!!");
                        Console.WriteLine(excp.Message);
                        Console.WriteLine(excp.StackTrace);
                    }

                    Environment.Exit(0);
                });
        }

        /// <summary>
        /// Получение ID родительского процесса через WMI
        /// </summary>
        private static int? GetParentProcessId()
        {
            using (var process = Process.GetCurrentProcess())
            {
                try
                {
                    string query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}";
                    using (var searcher = new System.Management.ManagementObjectSearcher(query))
                    {
                        foreach (var item in searcher.Get())
                        {
                            return Convert.ToInt32(item["ParentProcessId"]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //todo log
                    Console.WriteLine($"Ошибка при получении родительского PID: {ex.Message}");
                }
            }

            return null;
        }
    }
}
