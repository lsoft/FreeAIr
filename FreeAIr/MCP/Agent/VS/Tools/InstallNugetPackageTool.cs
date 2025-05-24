using EnvDTE;
using EnvDTE80;
using FreeAIr.BuildErrors;
using FreeAIr.Helper;
using FreeAIr.MCP.Agent.Github.BLO;
using Microsoft.VisualStudio.ComponentModelHost;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.VS.Tools
{
    public sealed class InstallNugetPackageTool : VisualStudioAgentTool
    {
        public static readonly InstallNugetPackageTool Instance = new();

        public const string VisualStudioToolName = "InstallNugetPackage";

        private const string NugetPackageNameParameterName = "nuget_package_name";
        private const string NugetPackageVersionParameterName = "nuget_package_version";
        private const string TargetProjectNameParameterName = "target_project_name";

        public InstallNugetPackageTool(
            ) : base(
                VisualStudioAgent.VisualStudioAgentName,
                VisualStudioToolName,
                "Install a nuget package in specified project of current solution.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{NugetPackageNameParameterName}}}": {
                            "type": "string",
                            "description": "Name of nuget package you want to install"
                            },
                        "{{{NugetPackageVersionParameterName}}}": {
                            "type": "string",
                            "description": "A version of nuget package you want ot install. Leave this empty if you want to install latest version of nuget package."
                            },
                        "{{{TargetProjectNameParameterName}}}": {
                            "type": "string",
                            "description": "A solution project name in which you want ot install a nuget package"
                            }
                        },
                    "required": ["{{{NugetPackageNameParameterName}}}", "{{{TargetProjectNameParameterName}}}"]
                }
                """
                )
        {
        }

        public override async Task<AgentToolCallResult> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!arguments.TryGetValue(NugetPackageNameParameterName, out var nugetPackageNameItem))
            {
                return new AgentToolCallResult($"Parameter {NugetPackageNameParameterName} does not found.");
            }
            var nugetPackageName = nugetPackageNameItem as string;

            string? nugetPackageVersion = null;
            if (arguments.TryGetValue(NugetPackageVersionParameterName, out var nugetPackageVersionItem))
            {
                nugetPackageVersion = nugetPackageVersionItem as string;
            }

            if (!arguments.TryGetValue(TargetProjectNameParameterName, out var targetProjectNameItem))
            {
                return new AgentToolCallResult($"Parameter {TargetProjectNameParameterName} does not found.");
            }
            var targetProjectName = targetProjectNameItem as string;



            var dte = await FreeAIrPackage.Instance.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte == null)
            {
                //todo log
                return new AgentToolCallResult("Visual Studio internal error.");
            }

            var targetProject = dte.TryFindProject(targetProjectName);
            if (targetProject is null)
            {
                return new AgentToolCallResult($"Cannot find project `{targetProjectName}` in current solution.");
            }

            var componentModel = await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;

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


            return new AgentToolCallResult($"Nuget package `{nugetPackageName}` version {nugetPackageVersion} installed into project `{targetProjectName}` SUCCESSFULLY.");
        }
    }

}
