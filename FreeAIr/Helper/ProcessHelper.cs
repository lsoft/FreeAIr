using RunProcessAsTask;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class ProcessHelper
    {
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
                        WindowStyle = ProcessWindowStyle.Hidden
                    },
                    cancellationToken
                    );
        }

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
