using Dto;
using EnvDTE;
using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.McpServerProxy;
using FreeAIr.Options2;
using StreamJsonRpc;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy
{
    /// <summary>
    /// Owns `Proxy.exe` — the child process which hosts every out-of-process MCP server.
    ///
    /// MCP servers cannot be hosted inside devenv: the extension targets .NET Framework 4.8, while
    /// the MCP SDK needs a modern .NET. So the proxy is a .NET 9 executable shipped inside the VSIX
    /// as `Proxy.zip`, unpacked on the first use and talked to over JSON-RPC on its stdin/stdout.
    ///
    /// Everything here happens in the static constructor, which means that merely touching this
    /// class starts the child process.
    ///
    /// The proxy is restartable: <see cref="ProcessMonitor"/> brings it back if it dies, and the
    /// JSON-RPC channel is re-attached to the streams of the new process every time, see
    /// <see cref="ProxyProcessStarted"/>. A restarted proxy hosts no MCP servers, so the current
    /// configuration is pushed into it again.
    /// </summary>
    public static class McpServerProxyApplication
    {
        /// <summary>The name `Proxy.csproj`'s `PostBuild` target zips its output to and embeds into the VSIX; unpacked on first use.</summary>
        public const string ProxyApplicationZipFileName = "Proxy.zip";
        /// <summary>The executable launched inside <see cref="ProxyUnpackedFolderPath"/> once the zip has been extracted.</summary>
        public const string ProxyApplicationExeFileName = "Proxy.exe";

        /// <summary>
        /// Written into the unpacked folder after the last entry of the archive has been extracted.
        /// Its presence — and only it — means the folder can be trusted.
        /// </summary>
        private const string UnpackedMarkerFileName = "unpacked.marker";

        /// <summary>The folder `Proxy.zip` is extracted into, derived from the extension's own install folder so an upgraded VSIX never merges into a previous version's copy.</summary>
        public static readonly string ProxyUnpackedFolderPath;
        /// <summary>The folder holding the embedded `Proxy.zip` archive before it is unpacked.</summary>
        private static readonly string _proxyZipFolderPath;

        /// <summary>Cancelled on Visual Studio shutdown to stop <see cref="_processMonitor"/>'s monitoring loop.</summary>
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        /// <summary>Keeps `Proxy.exe` running, restarting it if it dies; see <see cref="ProcessMonitor"/>.</summary>
        private static readonly ProcessMonitor _processMonitor;
        /// <summary>The long-running task backing <see cref="_processMonitor"/>'s monitoring loop.</summary>
        private static readonly Task _processTask;

        /// <summary>Subscribed to <c>OnBeginShutdown</c> so the proxy process is torn down when Visual Studio closes.</summary>
        private static readonly DTEEvents _dteEvents;

        //private static readonly HttpClient _httpClient;

        /// <summary>
        /// The rpc channel to the process which is running right now, or null while there is none.
        /// Replaced on every restart of the proxy.
        /// </summary>
        private static JsonRpc? _rpc;
        private static IMcpProxyInterface? _proxyInterface;

        /// <summary>
        /// True while the rpc channel to the proxy is alive. It goes false between a crash of the
        /// proxy and its restart, so it has to be re-checked before every call — which is exactly
        /// what every <see cref="IMcpServerProxy"/> hosted by the proxy does.
        /// </summary>
        public static bool Started => _proxyInterface is not null;

        /// <summary>
        /// Null until the proxy has been started for the first time, and between a crash of the
        /// proxy and its restart.
        /// </summary>
        public static IMcpProxyInterface? ProxyInterface => _proxyInterface;

        //public static HttpClient HttpClient =>
        //    Started
        //        ? _httpClient
        //        : throw new InvalidOperationException("Proxy not started");

        static McpServerProxyApplication()
        {
            ProxyUnpackedFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Proxy\Unpacked");
            _proxyZipFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Proxy\Archive");

            UnpackProxy();

            //derive a per-devenv identifier so that several Visual Studio instances
            //running side by side do not fight over the same proxy
            var visualStudioProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var proxyProcessId = 30000 + (visualStudioProcessId % 10000);

            //_httpClient = new();
            //_httpClient.BaseAddress = new Uri($"http://localhost:{proxyProcessId}");

            _processMonitor = new ProcessMonitor(
                ProxyUnpackedFolderPath,
                ProxyApplicationExeFileName,
                $"{proxyProcessId} {visualStudioProcessId}"
                );

            //must be subscribed before the monitoring starts: the first process is started
            //synchronously, so the very first event is raised from inside this constructor
            _processMonitor.ProcessStarted += ProxyProcessStarted;

            _processTask = _processMonitor.StartMonitoringAsync(
                _cancellationTokenSource.Token
                );

            //a static constructor which throws poisons the type for the rest of the session, and
            //failing to subscribe to the shutdown is not a reason to lose the whole MCP subsystem
            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte is null)
            {
                ActivityLogHelper.ActivityLogWarning(
                    "Cannot obtain DTE service, MCP proxy will not be stopped on the shutdown of Visual Studio."
                    );
                return;
            }

            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
        }

        /// <summary>
        /// Re-binds the rpc channel to the process which has just started. Invoked on the first
        /// start and on every restart of the proxy.
        ///
        /// Never throws: it runs inside the monitoring loop, and a failure here must not take the
        /// monitoring down with it.
        /// </summary>
        private static void ProxyProcessStarted(
            System.Diagnostics.Process process
            )
        {
            try
            {
                var isRestart = _rpc is not null;

                //the previous channel is bound to the streams of the process which has already died
                var oldRpc = _rpc;
                _rpc = null;
                _proxyInterface = null;
                if (oldRpc is not null)
                {
                    try
                    {
                        oldRpc.Dispose();
                    }
                    catch (Exception excp)
                    {
                        excp.ActivityLogException();
                    }
                }

                var rpc = JsonRpc.Attach(
                    process.StandardInput.BaseStream,
                    process.StandardOutput.BaseStream
                    );
                _proxyInterface = rpc.Attach<IMcpProxyInterface>();
                _rpc = rpc;

                if (isRestart)
                {
                    //a freshly started proxy hosts no MCP servers at all,
                    //so the current configuration has to be pushed into it again
                    ActivityLogHelper.ActivityLogInformation(
                        "MCP proxy has been restarted, reapplying the MCP servers configuration."
                        );

                    UpdateExternalServersAsync()
                        .FileAndForget(nameof(UpdateExternalServersAsync));
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Validates a set of configured MCP servers by actually starting them. Returns true
        /// Returns true only when every configured server came up; otherwise the user is told
        /// which ones failed and the caller is expected to abandon the save.
        /// </summary>
        public static async Task<bool> ApplyServerNodeAsync(
            McpServers servers
            )
        {
            if (servers is null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            try
            {
                var setupResult = await UpdateExternalServersAsync(
                    servers
                    );
                if (setupResult is null)
                {
                    await Community.VisualStudio.Toolkit.VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Invalid MCP servers json subnode. Fix json and try again."
                        );
                    return false;
                }

                var failedServerNames = new List<string>();
                foreach (var mcpServer in servers.Servers)
                {
                    if (setupResult.SuccessStartedMcpServers.All(a => a.Name != mcpServer.Key))
                    {
                        //этот сервер не был инициализирован по какой-то причине
                        failedServerNames.Add(mcpServer.Key);
                    }
                }
                if (failedServerNames.Count > 0)
                {
                    await Community.VisualStudio.Toolkit.VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Some MCP servers failed to start: {string.Join(",", failedServerNames)}. Changes did not saved."
                        );
                    return false;
                }

                return true;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                await Community.VisualStudio.Toolkit.VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    excp.Message
                    + Environment.NewLine
                    + excp.StackTrace
                    );
            }

            return false;
        }

        /// <summary>
        /// Pushes the configured MCP servers into the proxy, then reconciles the local tool
        /// catalogue with what actually started.
        ///
        /// Servers the proxy refused to start are simply missing from the result, which is how the
        /// caller learns about the failure — see
        /// <see cref="McpServersSetupConfigurationResult.SuccessStartedMcpServers"/>.
        /// </summary>
        /// <param name="mcpServers">
        /// The servers to run. When null they are taken from the current FreeAIr settings.
        /// </param>
        /// <returns>
        /// Null when the settings could not even be read, or when the proxy is not running right
        /// now. In the latter case the configuration is pushed again by
        /// <see cref="ProxyProcessStarted"/> as soon as the proxy comes back.
        /// </returns>
        public static async Task<McpServersSetupConfigurationResult?> UpdateExternalServersAsync(
            McpServers? mcpServers = null
            )
        {
            var proxyInterface = _proxyInterface;
            if (proxyInterface is null)
            {
                ActivityLogHelper.ActivityLogWarning(
                    "MCP proxy is not running, MCP servers configuration is not applied."
                    );

                return null;
            }

            if (mcpServers is null)
            {
                try
                {
                    mcpServers = await FreeAIrOptions.DeserializeMcpServersAsync();
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();

                    return null;
                }
            }

            var reply = await proxyInterface.UpdateExternalServersAsync(
                new UpdateExternalServersRequest(
                    mcpServers
                    )
                );
            if (!string.IsNullOrEmpty(reply.ErrorMessage))
            {
                ActivityLogHelper.ActivityLogWarning(reply.ErrorMessage);
            }

            var approvedExternalMcpServers = reply.McpServers ?? new McpServers();

            var toolContainer = await AvailableToolContainer.ReadSystemAsync();

            var setupResult = await McpServerProxyCollection.SetupConfigurationAsync(
                toolContainer,
                approvedExternalMcpServers
                );

            return setupResult;
        }

        /// <summary>Cancels the proxy monitoring loop when Visual Studio starts shutting down, so `Proxy.exe` is not left running as an orphan.</summary>
        private static void DTEEvents_OnBeginShutdown()
        {
            _cancellationTokenSource.Cancel();

            ////а зачем вызывать WaitForStopAsync и НЕ ждать его?
            //WaitForStopAsync()
            //    .FileAndForget(nameof(WaitForStopAsync));
        }

        //public async Task WaitForStopAsync()
        //{
        //    if (_processTask is null)
        //    {
        //        return;
        //    }
        //    if (_processTask.IsCompleted || _processTask.IsCanceled || _processTask.IsFaulted)
        //    {
        //        return;
        //    }

        //    await _processTask;
        //}

        /// <summary>
        /// Extracts `Proxy.zip` into the extension folder. Since the folder path is derived from
        /// the folder the extension itself was loaded from, an upgraded VSIX unpacks into a new
        /// folder rather than merging into the old one.
        ///
        /// "Already unpacked" is decided by the marker file, not by the presence of the folder:
        /// the folder appears before the first entry is written, so a run interrupted halfway
        /// (devenv killed, antivirus, no disk space) would otherwise leave a truncated folder which
        /// is never repaired. Without the marker the extraction simply runs again, overwriting
        /// whatever has been written before.
        ///
        /// Never throws: a proxy which could not be unpacked simply fails to start, and
        /// <see cref="ProcessMonitor"/> reports that in the activity log in a much more telling way
        /// than a type initialization error would.
        /// </summary>
        private static void UnpackProxy()
        {
            try
            {
                var markerFilePath = Path.Combine(
                    ProxyUnpackedFolderPath,
                    UnpackedMarkerFileName
                    );
                if (File.Exists(markerFilePath))
                {
                    return;
                }

                if (!Directory.Exists(ProxyUnpackedFolderPath))
                {
                    Directory.CreateDirectory(ProxyUnpackedFolderPath);
                }

                var zipFilePath = Path.Combine(
                    _proxyZipFolderPath,
                    ProxyApplicationZipFileName
                    );

                using (var zip = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (var entry in zip.Entries)
                    {
                        var entryFilePath = Path.Combine(ProxyUnpackedFolderPath, entry.FullName);

                        var entryFolderPath = Path.GetDirectoryName(entryFilePath);
                        if (!string.IsNullOrEmpty(entryFolderPath) && !Directory.Exists(entryFolderPath))
                        {
                            Directory.CreateDirectory(entryFolderPath);
                        }

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            //каталог, а не файл
                            continue;
                        }

                        entry.ExtractToFile(entryFilePath, overwrite: true);
                    }
                }

                //маркер пишется последним: пока его нет, распаковка считается незавершённой
                File.WriteAllText(markerFilePath, ProxyApplicationZipFileName);
            }
            catch (Exception excp)
            {
                excp.ActivityLogException(
                    $"Cannot unpack {ProxyApplicationZipFileName} into {ProxyUnpackedFolderPath}"
                    );
            }
        }

    }
}
