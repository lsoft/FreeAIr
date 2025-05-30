using Agent.BLogic;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Server.Github
{
    public static class GithubInstaller
    {
        public const string AssetsUrl = "https://api.github.com/repos/github/github-mcp-server/releases/latest";
        public const string WinX64AssetFileName = "github-mcp-server_Windows_x86_64.zip";

        public const string ExeFolderName = "github-mcp-server";
        public const string ExeFileName = "github-mcp-server.exe";

        public static bool IsInstalled(string mcpServerFolderPath)
        {
            if (!Directory.Exists(mcpServerFolderPath))
            {
                return false;
            }

            if (!File.Exists(GetMCPServerFilePath(mcpServerFolderPath)))
            {
                return false;
            }

            return true;
        }

        public static async Task<bool> InstallAsync(
            string mcpServerFolderPath
            )
        {
            try
            {
                if (IsInstalled(mcpServerFolderPath))
                {
                    return true;
                }

                var exeFilePath = GetMCPServerFilePath(mcpServerFolderPath);

                if (Directory.Exists(mcpServerFolderPath))
                {
                    if (!File.Exists(exeFilePath))
                    {
                        Directory.Delete(mcpServerFolderPath, true);
                    }
                }

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("C# App");

                var release = await httpClient.GetFromJsonAsync<Release>(
                    AssetsUrl
                    );
                if (release == null)
                {
                    return false;
                }

                var winx64 = release.assets.FirstOrDefault(a => a.name == WinX64AssetFileName);
                if (winx64 == null)
                {
                    return false;
                }

                //var url = new Uri(winx64.browser_download_url);
                //var fileName = url.Segments.Last();

                var bins = await httpClient.GetByteArrayAsync(
                    winx64.browser_download_url
                    );
                if (bins == null || bins.Length == 0)
                {
                    return false;
                }

                var zipFilePath = Path.Combine(
                    Path.GetTempPath(),
                    Guid.NewGuid().ToString()
                    );
                try
                {
                    File.WriteAllBytes(zipFilePath, bins);

                    using var zip = ZipFile.OpenRead(zipFilePath);
                    zip.ExtractToDirectory(mcpServerFolderPath);

                    if (IsInstalled(mcpServerFolderPath))
                    {
                        return true;
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

            return false;
        }


        public static string GetMCPServerFilePath(string mcpServerFolderPath)
        {
            return Path.Combine(
                mcpServerFolderPath,
                ExeFileName
                );
        }
    }
}
