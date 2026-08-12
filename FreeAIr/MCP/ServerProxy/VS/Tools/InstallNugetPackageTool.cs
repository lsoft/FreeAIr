using EnvDTE;
using EnvDTE80;
using FreeAIr.Helper;
using NuGet.VisualStudio;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>
    /// MCP tool that lets the chat model install a NuGet package into a chosen project of the
    /// currently open solution, using Visual Studio's own <see cref="IVsPackageInstaller2"/> service
    /// so the result is identical to using the NuGet Package Manager UI.
    /// </summary>
    public sealed class InstallNugetPackageTool : VisualStudioMcpServerTool
    {
        /// <summary>
        /// The single shared instance of this tool, registered by <see cref="VisualStudioMcpServerProxy"/>.
        /// </summary>
        public static readonly InstallNugetPackageTool Instance = new();

        /// <summary>
        /// The tool name advertised to the chat model for the NuGet install operation.
        /// </summary>
        public const string VisualStudioToolName = "InstallNugetPackage";

        /// <summary>
        /// JSON schema key for the NuGet package id to install.
        /// </summary>
        private const string NugetPackageNameParameterName = "nuget_package_name";

        /// <summary>
        /// JSON schema key for the package version to install; omitted or "latest" installs the
        /// newest available version.
        /// </summary>
        private const string NugetPackageVersionParameterName = "nuget_package_version";

        /// <summary>
        /// JSON schema key for the name of the solution project the package should be installed into.
        /// </summary>
        private const string TargetProjectNameParameterName = "target_project_name";

        /// <summary>
        /// Declares the tool's name and JSON schema describing the package name, optional version
        /// and target project parameters expected in a tool call.
        /// </summary>
        public InstallNugetPackageTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Install a nuget package in specified project of current solution.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{NugetPackageNameParameterName}}}": {
                            "type": "string",
                            "description": "Header of nuget package you want to install"
                            },
                        "{{{NugetPackageVersionParameterName}}}": {
                            "type": "string",
                            "description": "A version of nuget package you want to install. Leave this empty if you want to install latest version of nuget package."
                            },
                        "{{{TargetProjectNameParameterName}}}": {
                            "type": "string",
                            "description": "A solution project name in which you want to install a nuget package"
                            }
                        },
                    "required": ["{{{NugetPackageNameParameterName}}}", "{{{TargetProjectNameParameterName}}}"]
                }
                """
                )
        {
        }

        /// <summary>
        /// Resolves the target project by name, then installs the requested NuGet package (latest
        /// or a specific version) into it via the Visual Studio package installer service.
        /// </summary>
        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!arguments.TryGetValue(NugetPackageNameParameterName, out var nugetPackageNameItem))
            {
                return McpServerProxyToolCallResult.CreateFailed($"Parameter {NugetPackageNameParameterName} does not found.");
            }
            var nugetPackageName = nugetPackageNameItem as string;

            string? nugetPackageVersion = null;
            if (arguments.TryGetValue(NugetPackageVersionParameterName, out var nugetPackageVersionItem))
            {
                nugetPackageVersion = nugetPackageVersionItem as string;
            }

            if (!arguments.TryGetValue(TargetProjectNameParameterName, out var targetProjectNameItem))
            {
                return McpServerProxyToolCallResult.CreateFailed($"Parameter {TargetProjectNameParameterName} does not found.");
            }
            var targetProjectName = targetProjectNameItem as string;



            var dte = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte == null)
            {
                //todo log

                return McpServerProxyToolCallResult.CreateFailed("Visual Studio internal error.");
            }

            var targetProject = dte.TryFindProject(targetProjectName);
            if (targetProject is null)
            {
                return McpServerProxyToolCallResult.CreateFailed($"Cannot find project `{targetProjectName}` in current solution.");
            }

            var componentModel = await MefHelper.GetComponentModelAsync();

            var packageInstaller = componentModel.GetService<IVsPackageInstaller2>();

            if (string.IsNullOrEmpty(nugetPackageVersion) || StringComparer.InvariantCultureIgnoreCase.Equals(nugetPackageVersion, "latest"))
            {
                packageInstaller.InstallLatestPackage(
                    null,
                    targetProject,
                    nugetPackageName,
                    true,
                    false
                    );
            }
            else
            {
                packageInstaller.InstallPackage(
                    null,
                    targetProject,
                    nugetPackageName,
                    nugetPackageVersion,
                    false
                    );
            }


            return McpServerProxyToolCallResult.CreateSuccess($"Nuget package `{nugetPackageName}` version {nugetPackageVersion} installed into project `{targetProjectName}` SUCCESSFULLY.");
        }
    }

}
