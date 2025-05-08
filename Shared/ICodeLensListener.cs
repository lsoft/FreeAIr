using FreeAIr.Shared.Dto;
using System;
using System.Threading.Tasks;

namespace FreeAIr.Shared
{
    public interface ICodeLensListener
    {
        Task<bool> IsEnabled(
            );

        Task<CodeLensUnitInfo> GetUnitInformationAsync(
            CodeLensTarget target
            );

        int GetVisualStudioPid();
    }
}
