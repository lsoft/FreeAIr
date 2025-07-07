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
    public class CodeLensListener : ICodeLensCallbackListener, ICodeLensListener
    {
        private readonly IComponentModel _componentModel;

        [ImportingConstructor]
        public CodeLensListener(
            )
        {
            _componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
        }

        public Task<bool> IsEnabled(
            )
        {
            return Task.FromResult(true);
        }

        public int GetVisualStudioPid() => Process.GetCurrentProcess().Id;

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
