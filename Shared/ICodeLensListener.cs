using FreeAIr.Shared.Dto;
using System.Threading.Tasks;

namespace FreeAIr.Shared
{
    /// <summary>
    /// Cross-process contract implemented by the Visual Studio package and called by the CodeLens
    /// data-point host over the named pipe from <see cref="CodeLensPipeName"/>, since CodeLens
    /// data points run out-of-process from the main IDE.
    /// </summary>
    public interface ICodeLensListener
    {
        /// <summary>Whether the FreeAIr CodeLens indicator is currently turned on in settings.</summary>
        Task<bool> IsEnabled(
            );

        /// <summary>Resolves a <see cref="CodeLensTarget"/> to the method info shown by the indicator.</summary>
        Task<CodeLensUnitInfo> GetUnitInformationAsync(
            CodeLensTarget target
            );

        /// <summary>PID of the Visual Studio process hosting this listener, used to build the scoped pipe name.</summary>
        int GetVisualStudioPid();
    }
}
