using Proxy.BLogic;
using System.IO.Compression;
using System.Net.Http.Json;

namespace Proxy.Server.Github
{
    /// <summary>
    /// Downloads and unpacks the official `github-mcp-server` executable from its GitHub Releases
    /// page the first time the GitHub MCP server is used, so the user never has to install it by
    /// hand.
    /// </summary>
    public static class GithubInstaller
    {
        /// <summary>GitHub Releases API URL for the latest `github-mcp-server` release.</summary>
        public const string AssetsUrl = "https://api.github.com/repos/github/github-mcp-server/releases/latest";
        /// <summary>File name of the Windows x64 release asset to download.</summary>
        public const string WinX64AssetFileName = "github-mcp-server_Windows_x86_64.zip";

        /// <summary>Name of the folder the executable is unpacked into.</summary>
        public const string ExeFolderName = "github-mcp-server";
        /// <summary>File name of the unpacked `github-mcp-server` executable.</summary>
        public const string ExeFileName = "github-mcp-server.exe";

        /// <summary>Whether the executable is already unpacked at <paramref name="mcpServerFolderPath"/>.</summary>
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

        /// <summary>Fetches the latest release from <see cref="AssetsUrl"/>, downloads the Windows x64 asset and extracts it to <paramref name="mcpServerFolderPath"/>; a no-op if it is already installed.</summary>
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


        /// <summary>The path to the executable inside the server's install folder.</summary>
        public static string GetMCPServerFilePath(string mcpServerFolderPath)
        {
            return Path.Combine(
                mcpServerFolderPath,
                ExeFileName
                );
        }
    }
}
