using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
