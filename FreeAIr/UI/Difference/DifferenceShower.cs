using FreeAIr.Helper;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Threading.Tasks;
using FreeAIr.BLogic;

namespace FreeAIr.UI.Difference
{
    public static class DifferenceShower
    {
        private static object _locker = new();
        private static Dictionary<string, DiffFrameDescriptor> _descriptors = new();

        static DifferenceShower()
        {
            VS.Events.WindowEvents.ActiveFrameChanged += WindowEvents_ActiveFrameChanged;
            VS.Events.WindowEvents.Destroyed += WindowEvents_Destroyed;
        }

        public static async Task<string?> ShowAsync(
            DifferenceShowerParameters parameters
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var differenceService = (IVsDifferenceService)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SVsDifferenceService));

            lock (_locker)
            {
                if (_descriptors.TryGetValue(parameters.Caption, out var d))
                {
                    d.Frame.Show();
                    return null;
                }
            }

            DiffFrameDescriptor? descriptor = null;
            try
            {
                var leftFile = TempFile.CreateBasedOfExistingName(
                    parameters.FileName,
                    "current"
                    );
                leftFile.WriteAllText(parameters.OriginalFileBody);

                var rightFile = TempFile.CreateBasedOfExistingName(
                    parameters.FileName,
                    "modified"
                    );
                rightFile.WriteAllText(parameters.OriginalFileBody);

                var frame = differenceService.OpenComparisonWindow2(
                    leftFileMoniker: leftFile.FilePath,
                    rightFileMoniker: rightFile.FilePath,
                    caption: parameters.Caption,
                    Tooltip: parameters.Tooltip,
                    leftLabel: parameters.LeftLabel,
                    rightLabel: parameters.RightLabel,
                    inlineLabel: parameters.Caption,
                    roles: null,
                    grfDiffOptions: (uint)__VSDIFFSERVICEOPTIONS.VSDIFFOPT_DoNotShow
                    );

                descriptor = new DiffFrameDescriptor(
                    parameters,
                    leftFile,
                    rightFile,
                    frame
                    );
                lock (_locker)
                {
                    _descriptors.Add(parameters.Caption, descriptor);
                }

                frame.Show();

                await descriptor.CloseSignal.WaitAsync();

                var result = descriptor.TwiceModifiedItemBody;

                return result;
            }
            finally
            {
                descriptor?.Dispose();
            }
        }


        private static async void WindowEvents_ActiveFrameChanged(ActiveFrameChangeEventArgs frameChanged)
        {
            var windowFrame = frameChanged.NewFrame;

            var caption = windowFrame.Caption;

            DiffFrameDescriptor? d = null;
            lock (_locker)
            {
                if (!_descriptors.TryGetValue(caption, out d))
                {
                    return;
                }

                if (d.ChangedTextAdded)
                {
                    return;
                }

                d.ChangedTextAdded = true;
            }

            ApplyModifiedBodyAsync(windowFrame, d)
                .FileAndForget(nameof(ApplyModifiedBodyAsync));
        }

        private static void WindowEvents_Destroyed(WindowFrame windowFrame)
        {
            var caption = windowFrame.Caption;

            DiffFrameDescriptor? d = null;
            lock (_locker)
            {
                if (!_descriptors.TryGetValue(caption, out d))
                {
                    return;
                }

                _descriptors.Remove(caption);
            }

            d.FrameClosed();
        }


        private static async Task ApplyModifiedBodyAsync(
            WindowFrame windowFrame,
            DiffFrameDescriptor descriptor
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var documentView = await windowFrame.GetDocumentViewAsync();
                if (documentView is null)
                {
                    return;
                }

                var currentText = documentView.TextBuffer.CurrentSnapshot.GetText();

                using var edit = documentView.TextBuffer.CreateEdit();
                edit.Replace(
                    0,
                    currentText.Length,
                    descriptor.Parameters.ModifiedFileBody
                    );

                edit.Apply();

            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        private sealed class DiffFrameDescriptor : IDisposable
        {
            public DifferenceShowerParameters Parameters
            {
                get;
            }

            public IVsWindowFrame Frame
            {
                get;
            }

            public bool ChangedTextAdded
            {
                get;
                set;
            }

            public TempFile LeftFile
            {
                get;
            }

            public TempFile RightFile
            {
                get;
            }

            public NonDisposableSemaphoreSlim CloseSignal
            {
                get;
            }

            public string TwiceModifiedItemBody
            {
                get;
                private set;
            }

            public DiffFrameDescriptor(
                DifferenceShowerParameters parameters,
                TempFile leftFile,
                TempFile rightFile,
                IVsWindowFrame frame
                )
            {
                if (leftFile is null)
                {
                    throw new ArgumentNullException(nameof(leftFile));
                }

                if (rightFile is null)
                {
                    throw new ArgumentNullException(nameof(rightFile));
                }

                if (frame is null)
                {
                    throw new ArgumentNullException(nameof(frame));
                }

                Parameters = parameters;
                LeftFile = leftFile;
                RightFile = rightFile;
                Frame = frame;

                TwiceModifiedItemBody = parameters.OriginalFileBody;

                CloseSignal = new(0, 1);
            }

            public void FrameClosed()
            {
                TwiceModifiedItemBody = RightFile.ReadAllText();

                CloseSignal.Release();
            }

            public void Dispose()
            {
                LeftFile.Dispose();
                RightFile.Dispose();
            }
        }

    }

    public sealed class DifferenceShowerParameters
    {
        public string FileName
        {
            get;
        }

        public string OriginalFileBody
        {
            get;
        }

        public string ModifiedFileBody
        {
            get;
        }

        public string Caption
        {
            get;
        }

        public string Tooltip
        {
            get;
        }
        
        public string LeftLabel
        {
            get;
        }
        
        public string RightLabel
        {
            get;
        }

        public DifferenceShowerParameters(
            string fileName,
            string originalFileBody,
            string modifiedFileBody,
            string caption,
            string tooltip,
            string leftLabel,
            string rightLabel
            )
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (originalFileBody is null)
            {
                throw new ArgumentNullException(nameof(originalFileBody));
            }

            if (modifiedFileBody is null)
            {
                throw new ArgumentNullException(nameof(modifiedFileBody));
            }

            if (string.IsNullOrEmpty(caption))
            {
                throw new ArgumentException($"'{nameof(caption)}' cannot be null or empty.", nameof(caption));
            }

            if (string.IsNullOrEmpty(tooltip))
            {
                throw new ArgumentException($"'{nameof(tooltip)}' cannot be null or empty.", nameof(tooltip));
            }

            if (string.IsNullOrEmpty(leftLabel))
            {
                throw new ArgumentException($"'{nameof(leftLabel)}' cannot be null or empty.", nameof(leftLabel));
            }

            if (string.IsNullOrEmpty(rightLabel))
            {
                throw new ArgumentException($"'{nameof(rightLabel)}' cannot be null or empty.", nameof(rightLabel));
            }

            FileName = fileName;
            OriginalFileBody = originalFileBody;
            ModifiedFileBody = modifiedFileBody;
            Caption = caption;
            Tooltip = tooltip;
            LeftLabel = leftLabel;
            RightLabel = rightLabel;
        }
    }
}
