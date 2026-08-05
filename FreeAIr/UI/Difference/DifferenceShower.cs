using FreeAIr.Helper;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Threading.Tasks;
using FreeAIr.BLogic;

namespace FreeAIr.UI.Difference
{
    /// <summary>
    /// Opens Visual Studio's built-in diff/difference window to show the LLM's proposed change
    /// to a file side by side with the current content, and captures whatever the user edits
    /// in that window once it is closed.
    /// </summary>
    public static class DifferenceShower
    {
        /// <summary>Guards access to <see cref="_descriptors"/> across the UI thread and VS window events.</summary>
        private static object _locker = new();
        /// <summary>Open diff windows keyed by their caption, so a second request for the same caption reuses the existing frame.</summary>
        private static Dictionary<string, DiffFrameDescriptor> _descriptors = new();

        static DifferenceShower()
        {
            VS.Events.WindowEvents.ActiveFrameChanged += WindowEvents_ActiveFrameChanged;
            VS.Events.WindowEvents.Destroyed += WindowEvents_Destroyed;
        }

        /// <summary>
        /// Opens (or focuses an existing) diff window comparing the original and modified file bodies,
        /// then waits until the user closes it and returns whatever the right-hand side ended up containing.
        /// </summary>
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


        /// <summary>
        /// When a tracked diff window becomes the active frame, injects the modified file body into it once,
        /// so the comparison shows the proposed change rather than a duplicate of the original.
        /// </summary>
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

        /// <summary>
        /// Removes a diff window from tracking once Visual Studio destroys its frame and signals
        /// <see cref="ShowAsync"/> to resume with the user's final edit of the right-hand file.
        /// </summary>
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


        /// <summary>
        /// Replaces the text buffer shown in the right-hand pane of the diff window with the
        /// LLM-modified file body, so the user sees the proposed change instead of the original text.
        /// </summary>
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

        /// <summary>
        /// Tracks one open diff window: the temp files backing each side, the VS frame itself,
        /// and the signal used to hand the user's final edit back to the awaiting <see cref="ShowAsync"/> call.
        /// </summary>
        private sealed class DiffFrameDescriptor : IDisposable
        {
            /// <summary>The captions, labels and file bodies the diff window was opened with.</summary>
            public DifferenceShowerParameters Parameters
            {
                get;
            }

            /// <summary>The Visual Studio window frame hosting this diff comparison.</summary>
            public IVsWindowFrame Frame
            {
                get;
            }

            /// <summary>True once the modified file body has been injected into the frame, so it is only applied once.</summary>
            public bool ChangedTextAdded
            {
                get;
                set;
            }

            /// <summary>Temp file backing the left ("current") side of the comparison.</summary>
            public TempFile LeftFile
            {
                get;
            }

            /// <summary>Temp file backing the right ("modified") side of the comparison.</summary>
            public TempFile RightFile
            {
                get;
            }

            /// <summary>Released when the diff window is closed, unblocking whoever awaited <see cref="ShowAsync"/>.</summary>
            public NonDisposableSemaphoreSlim CloseSignal
            {
                get;
            }

            /// <summary>The right-hand file body as it stood when the window closed, i.e. after the user's own edits.</summary>
            public string TwiceModifiedItemBody
            {
                get;
                private set;
            }

            /// <summary>Creates the descriptor for a newly opened diff window, wiring together its temp files, frame and parameters.</summary>
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

            /// <summary>Captures the right-hand file's final text and releases <see cref="CloseSignal"/> when the window is destroyed.</summary>
            public void FrameClosed()
            {
                TwiceModifiedItemBody = RightFile.ReadAllText();

                CloseSignal.Release();
            }

            /// <summary>Deletes the left and right temp files backing this diff window.</summary>
            public void Dispose()
            {
                LeftFile.Dispose();
                RightFile.Dispose();
            }
        }

    }

    /// <summary>
    /// Everything needed to open a diff window comparing a file's original and LLM-modified bodies:
    /// the file name, both bodies, and the captions/labels shown in the Visual Studio UI.
    /// </summary>
    public sealed class DifferenceShowerParameters
    {
        /// <summary>Name of the file being compared, used to give the temp files a recognizable extension.</summary>
        public string FileName
        {
            get;
        }

        /// <summary>The file's content before the proposed change.</summary>
        public string OriginalFileBody
        {
            get;
        }

        /// <summary>The file's content after the proposed change, shown on the right-hand side of the diff.</summary>
        public string ModifiedFileBody
        {
            get;
        }

        /// <summary>Title of the diff window; also used as the key to detect and reuse an already-open comparison.</summary>
        public string Caption
        {
            get;
        }

        /// <summary>Tooltip text shown for the diff window's tab.</summary>
        public string Tooltip
        {
            get;
        }

        /// <summary>Label shown above the left ("current") pane of the diff.</summary>
        public string LeftLabel
        {
            get;
        }

        /// <summary>Label shown above the right ("modified") pane of the diff.</summary>
        public string RightLabel
        {
            get;
        }

        /// <summary>Validates and stores the file bodies and display labels for a new diff window.</summary>
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
