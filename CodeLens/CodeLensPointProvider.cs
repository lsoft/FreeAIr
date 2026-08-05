using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using FreeAIr.Shared;
using FreeAIr.Shared.Helper;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;


namespace FreeAIr.CodeLens
{
    [Export(typeof(IAsyncCodeLensDataPointProvider))]
    [Name(Id)]
    [ContentType("CSharp")]
    [LocalizedName(typeof(Resources), "FreeAIrCodeLensProvider")]
    [Priority(300)]
    /// <summary>MEF-exported CodeLens provider for C# files: decides which code elements get the FreeAIr indicator and creates the <see cref="CodeLensDataPoint"/> for each.</summary>
    public class CodeLensPointProvider : IAsyncCodeLensDataPointProvider
    {
        internal const string Id = "FreeAIrCodeLensProviderName";

        private readonly Lazy<ICodeLensCallbackService> _callbackService;


        [ImportingConstructor]
        public CodeLensPointProvider(
            Lazy<ICodeLensCallbackService> callbackService
            )
        {
            if (callbackService is null)
            {
                throw new ArgumentNullException(nameof(callbackService));
            }

            _callbackService = callbackService;
        }

        /// <summary>True when the indicator is enabled in settings and the element is a real code member (not a namespace/container/package).</summary>
        public async Task<bool> CanCreateDataPointAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext context, CancellationToken token)
        {
            if (!await IsEnabled())
            {
                return false;
            }

            var result = false;

            if (descriptor.Kind.NotIn(
                    CodeElementKinds.Unspecified,
                    CodeElementKinds.Invalid,
                    CodeElementKinds.Namespace,
                    CodeElementKinds.Container,
                    CodeElementKinds.Package
                    )
                )
            {
                result = true;
            }

            return result;
        }

        /// <summary>Asks the Visual Studio side, via <see cref="ICodeLensListener.IsEnabled"/>, whether the FreeAIr CodeLens indicator is turned on.</summary>
        public async Task<bool> IsEnabled()
        {
            try
            {
                return await _callbackService
                    .Value
                    .InvokeAsync<bool>(this, nameof(ICodeLensListener.IsEnabled))
                    .ConfigureAwait(false)
                    ;
            }
            catch (Exception ex)
            {
                //todo log
                throw;
            }

        }

        /// <summary>Creates a <see cref="CodeLensDataPoint"/> for the descriptor and connects it to the owning Visual Studio process.</summary>
        public async Task<IAsyncCodeLensDataPoint> CreateDataPointAsync(CodeLensDescriptor descriptor, CodeLensDescriptorContext context, CancellationToken token)
        {
            try
            {
                var dp = new CodeLensDataPoint(
                    _callbackService.Value, 
                    descriptor
                    );

                var vspid = await _callbackService.Value
                    .InvokeAsync<int>(this, nameof(ICodeLensListener.GetVisualStudioPid))
                    .ConfigureAwait(false)
                    ;

                await dp.ConnectToVisualStudioAsync(vspid).ConfigureAwait(false);

                return dp;
            }
            catch (Exception ex)
            {
                //todo log
                throw;
            }

        }
    }
}
