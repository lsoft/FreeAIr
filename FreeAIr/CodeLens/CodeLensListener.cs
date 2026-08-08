using FreeAIr.Shared;
using FreeAIr.Shared.Dto;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FreeAIr.Extension
{
    [Export(typeof(ICodeLensCallbackListener))]
    [ContentType("CSharp")]
    /// <summary>
    /// MEF-exported <see cref="ICodeLensCallbackListener"/> living in the main Visual Studio process:
    /// the counterpart the out-of-process <see cref="Microsoft.VisualStudio.Language.CodeLens.Remoting.ICodeLensCallbackService"/>
    /// calls into to answer <see cref="CodeLensDataPoint"/> requests via <see cref="ICodeLensListener"/>.
    /// </summary>
    public class CodeLensListener : ICodeLensCallbackListener, ICodeLensListener
    {
        /// <summary>The Visual Studio MEF component model, used to resolve services needed to answer CodeLens callbacks.</summary>
        private readonly IComponentModel _componentModel;

        /// <summary>Creates the listener and grabs the global <see cref="IComponentModel"/> service from the Visual Studio shell.</summary>
        [ImportingConstructor]
        public CodeLensListener(
            )
        {
            _componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
        }

        /// <summary>Whether the FreeAIr CodeLens indicator is enabled; currently always on.</summary>
        public Task<bool> IsEnabled(
            )
        {
            return Task.FromResult(true);
        }

        /// <summary>PID of this Visual Studio process, used by the CodeLens data point to build the scoped pipe name.</summary>
        public int GetVisualStudioPid() => Process.GetCurrentProcess().Id;

        /// <summary>Resolves a <see cref="CodeLensTarget"/> to the method info shown by the indicator.</summary>
        public async Task<CodeLensUnitInfo> GetUnitInformationAsync(
            CodeLensTarget target
            )
        {
            return new CodeLensUnitInfo
            {
                UnitInfo = new UnitInfo(
                    projectGuid: target.ProjectGuid,
                    documentGuid: target.RoslynDocumentIdGuid,
                    filePath: target.FilePath,
                    name: target.Name,
                    body: "body",
                    spanStart: target.SpanStart,
                    spanLength: target.SpanLength
                    )
            };
        }
    }
}
