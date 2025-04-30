using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace FreeAIr.Helper
{
    public class VisualStudioDocumentHelper
    {
        private readonly string _documentFullPath;

        private readonly IComponentModel _componentModel;
        private readonly IVsUIShellOpenDocument? _openDoc;
        private readonly IVsTextManager? _textManager;
        private readonly DTE2 _dte;
        private readonly ServiceProvider _serviceProvider;
        private readonly IVsInfoBarUIFactory _infoBarFactory;

        public VisualStudioDocumentHelper(
            string documentFullPath
            )
        {
            if (documentFullPath is null)
            {
                throw new ArgumentNullException(nameof(documentFullPath));
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            _documentFullPath = documentFullPath;

            _componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            _openDoc = Package.GetGlobalService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            _textManager = Package.GetGlobalService(typeof(VsTextManagerClass)) as IVsTextManager;

            _dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_dte);
            _infoBarFactory = (IVsInfoBarUIFactory)_serviceProvider.GetService(typeof(SVsInfoBarUIFactory));
        }

        public void Open(
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread(nameof(Open));

            var logicalView = VSConstants.LOGVIEWID_Code;

            OpenDocumentViewProject(logicalView, out _);
        }

        public async Task<bool> OpenNavigateViewRoslynInternalsAsync(
            DocumentId documentId,
            int position,
            int length
            )
        {
            try
            {
                var workspace = (Workspace)_componentModel.GetService<VisualStudioWorkspace>();

                var project = workspace.CurrentSolution.GetProject(documentId.ProjectId);
                if (project is null)
                {
                    return false;
                }

                var sgDocument = await project.GetDocumentByDocumentIdAsync(documentId);
                if (sgDocument is null)
                {
                    return false;
                }

                var extensions = _componentModel.GetExtensions<Microsoft.CodeAnalysis.Host.IWorkspaceService>().ToList();

                var navigationService = extensions.FirstOrDefault(s => s.GetType().Name == "VisualStudioDocumentNavigationService");
                if (navigationService is null)
                {
                    return false;
                }

                var getLocationForSpanAsyncMethod = navigationService.GetMethodFromInstance("GetLocationForSpanAsync");
                if (getLocationForSpanAsyncMethod is null)
                {
                    return false;
                }

                var navigableLocation = await navigationService.InvokeTaskMethodAsync<object>(
                    getLocationForSpanAsyncMethod,
                    new object[]
                    {
                        workspace,
                        sgDocument.Id,
                        new Microsoft.CodeAnalysis.Text.TextSpan(
                            position,
                            length
                            ),
                        true, //allowInvalidSpan
                        CancellationToken.None
                    });

                var navigableLocationType = navigableLocation.GetType();

                var navigateOptionsType = navigableLocationType.Assembly.GetType("Microsoft.CodeAnalysis.Navigation.NavigationOptions");
                if (navigateOptionsType is null)
                {
                    return false;
                }

                var navigateOptionsConstructor = navigateOptionsType.GetConstructor(new Type[0]);
                if (navigateOptionsConstructor is null)
                {
                    return false;
                }

                var navigateOptions = navigateOptionsConstructor.Invoke(new object[0]);
                if (navigateOptions is null)
                {
                    return false;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

                var navigateToAsyncMethod = navigableLocationType.GetMethod("NavigateToAsync");
                if (navigateToAsyncMethod is null)
                {
                    return false;
                }

                var navigateToAsyncMethodSuccess = await navigableLocation.InvokeTaskMethodAsync<bool>(
                    navigateToAsyncMethod,
                    new object[]
                    {
                        navigateOptions,
                        CancellationToken.None
                    });
                return navigateToAsyncMethodSuccess;
            }
            catch (Exception ex)
            {
                //todo Logging.LogVS(ex);
            }

            return false;
        }

        public void OpenNavigate(
            int position,
            int length
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread(nameof(OpenAndNavigate));

            var logicalView = VSConstants.LOGVIEWID_Code;

            if (!OpenDocumentViewProject(logicalView, out IVsWindowFrame? frame))
            {
                return;
            }

            frame!.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out object docData);
            if (!GetVsTextBuffer(docData, out VsTextBuffer? vsTextBuffer2))
            {
                return;
            }

            //go to target part of the code
            NavigateAndSelect(
                vsTextBuffer2!,
                position,
                length
                );
        }

        public void OpenAndNavigate(
            int startLine,
            int startColumn,
            int endLine,
            int endColumn
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread(nameof(OpenAndNavigate));

            var logicalView = VSConstants.LOGVIEWID_Code;

            if (!OpenDocumentViewProject(logicalView, out IVsWindowFrame? frame))
            {
                return;
            }

            frame!.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out object docData);

            if (!GetVsTextBuffer(docData, out VsTextBuffer? buffer))
            {
                return;
            }

            NavigateAndSelect(
                buffer!,
                startLine,
                startColumn,
                endLine,
                endColumn
                );
        }

        private void NavigateAndSelect(
            VsTextBuffer buffer,
            int position,
            int length
            )
        {
            if (_textManager == null)
            {
                return;
            }

            var docViewType = default(Guid);

            _textManager.NavigateToPosition(
                buffer,
                ref docViewType,
                position,
                length
                );
        }

        private void NavigateAndSelect(
            VsTextBuffer buffer,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn
            )
        {
            if (_textManager == null)
            {
                return;
            }

            var docViewType = default(Guid);

            _textManager.NavigateToLineAndColumn(
                buffer,
                ref docViewType,
                startLine,
                startColumn,
                endLine,
                endColumn
                );
        }

        private static bool GetVsTextBuffer(
            object docData,
            out VsTextBuffer? buffer
            )
        {
            // Get the VsTextBuffer  
            buffer = docData as VsTextBuffer;
            if (buffer == null)
            {
                if (docData is IVsTextBufferProvider bufferProvider)
                {
                    ErrorHandler.ThrowOnFailure(
                        bufferProvider.GetTextBuffer(out IVsTextLines lines)
                        );

                    buffer = lines as VsTextBuffer;

                    Debug.Assert(buffer != null, "IVsTextLines does not implement IVsTextBuffer");

                    if (buffer == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool OpenDocumentViewProject(
            Guid logicalView,
            out IVsWindowFrame? frame
            )
        {
            if (_openDoc is null)
            {
                frame = null;
                return false;
            }

            if (ErrorHandler.Failed(
                    _openDoc.OpenDocumentViaProject(
                        _documentFullPath,
                        ref logicalView,
                        out IServiceProvider _,
                        out IVsUIHierarchy hier,
                        out var itemId,
                        out frame)
                    ) ||
                frame == null)
            {
                return false;
            }

            return true;
        }
    }
}
