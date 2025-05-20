using FreeAIr.UI.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.MCP.Github
{
    public static class Installer
    {
        public const string AssetsUrl = "https://api.github.com/repos/github/github-mcp-server/releases/latest";
        public const string WinX64AssetFileName = "github-mcp-server_Windows_arm64.zip";

        public const string ExeFolderName = "github-mcp-server";
        public const string ExeFileName = "github-mcp-server.exe";

        public static bool IsInstalled()
        {
            if (!Directory.Exists(GetMCPServerFolderPath()))
            {
                return false;
            }

            if (!File.Exists(GetMCPServerFilePath()))
            {
                return false;
            }

            return true;
        }

        public static async Task<bool> InstallAsync()
        {
            var backgroundTask = new GithubMCPInstallBackgroundTask();
            var w = new WaitForTaskWindow(
                backgroundTask
                );
            await w.ShowDialogAsync();

            var result = backgroundTask.SuccessfullyInstalled;
            return result;
        }


        public static string GetMCPServerFilePath()
        {
            return Path.Combine(
                GetMCPServerFolderPath(),
                ExeFileName
                );
        }
        public static string GetMCPServerFolderPath()
        {
            return Path.Combine(
                FreeAIrPackage.WorkingFolder,
                ExeFolderName
                );
        }

        private sealed class GithubMCPInstallBackgroundTask : BackgroundTask
        {
            public override string TaskDescription => "Installing GitHub.com MCP server...";

            public bool SuccessfullyInstalled
            {
                get;
                private set;
            }

            protected override async Task RunWorkingTaskAsync()
            {
                try
                {
                    if (IsInstalled())
                    {
                        SuccessfullyInstalled = true;
                        return;
                    }

                    SuccessfullyInstalled = false;

                    var exeFolderPath = GetMCPServerFolderPath();
                    var exeFilePath = GetMCPServerFilePath();

                    if (Directory.Exists(exeFolderPath))
                    {
                        if (!File.Exists(exeFilePath))
                        {
                            Directory.Delete(exeFolderPath, true);
                        }
                    }

                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("C# App");

                    var release = await httpClient.GetFromJsonAsync<Release>(
                        AssetsUrl
                        );
                    if (release == null)
                    {
                        return;
                    }

                    var winx64 = release.assets.FirstOrDefault(a => a.name == WinX64AssetFileName);
                    if (winx64 == null)
                    {
                        return;
                    }

                    //var url = new Uri(winx64.browser_download_url);
                    //var fileName = url.Segments.Last();

                    var bins = await httpClient.GetByteArrayAsync(
                        winx64.browser_download_url
                        );
                    if (bins == null || bins.Length == 0)
                    {
                        return;
                    }

                    var zipFilePath = Path.Combine(
                        Path.GetTempPath(),
                        Guid.NewGuid().ToString()
                        );
                    try
                    {
                        File.WriteAllBytes(zipFilePath, bins);

                        using var zip = ZipFile.OpenRead(zipFilePath);
                        zip.ExtractToDirectory(exeFolderPath);

                        if (IsInstalled())
                        {
                            SuccessfullyInstalled = true;
                            return;
                        }
                    }
                    finally
                    {
                        File.Delete(zipFilePath);
                    }
                }
                catch (Exception excp)
                {
                    //todo log
                }

                return;
            }

        }
    }
}
