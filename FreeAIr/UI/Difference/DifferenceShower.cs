using Community.VisualStudio.Toolkit;
using FreeAIr.Helper;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

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

        public static async Task ShowAsync(
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
                    return;
                }
            }

            var frame = differenceService.OpenComparisonWindow2(
                leftFileMoniker: parameters.LeftFile.FilePath,
                rightFileMoniker: parameters.RightFile.FilePath,
                caption: parameters.Caption,
                Tooltip: parameters.Tooltip,
                leftLabel: parameters.LeftLabel,
                rightLabel: parameters.RightLabel,
                inlineLabel: parameters.Caption,
                roles: null,
                grfDiffOptions: (uint)__VSDIFFSERVICEOPTIONS.VSDIFFOPT_DoNotShow
                );

            lock (_locker)
            {
                _descriptors.Add(parameters.Caption, new DiffFrameDescriptor(parameters, frame));
            }

            frame.Show();
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

                if (d.Parameters.ChangedTextAdded)
                {
                    return;
                }

                d.Parameters.ChangedTextAdded = true;
            }

            ApplyModifiedBodyAsync(windowFrame, d)
                .FileAndForget(nameof(ApplyModifiedBodyAsync));
        }

        private static async Task ApplyModifiedBodyAsync(
            WindowFrame windowFrame,
            DiffFrameDescriptor descriptor
            )
        {
            try
            {
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
                    //"q" + Environment.NewLine + d.Parameters.RightFile.ReadAllText()
                    );

                edit.Apply();

            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
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

            d.FrameClosedAsync()
                .FileAndForget(nameof(DiffFrameDescriptor.FrameClosedAsync));
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

            public DiffFrameDescriptor(
                DifferenceShowerParameters parameters,
                IVsWindowFrame frame
                )
            {
                if (frame is null)
                {
                    throw new ArgumentNullException(nameof(frame));
                }

                Parameters = parameters;
                Frame = frame;
            }

            public async Task FrameClosedAsync()
            {
                try
                {
                    await Parameters.FrameClosedAction?.Invoke(Parameters);
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();
                }
                finally
                {
                    Dispose();
                }
            }

            public void Dispose()
            {
                Parameters.Dispose();
            }
        }

    }

    public sealed class DifferenceShowerParameters : IDisposable
    {
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
        
        public Func<DifferenceShowerParameters, Task>? FrameClosedAction
        {
            get;
        }

        public TempFile LeftFile
        {
            get;
        }

        public TempFile RightFile
        {
            get;
        }

        public bool ChangedTextAdded
        {
            get;
            set;
        }


        public DifferenceShowerParameters(
            string fileName,
            string originalFileBody,
            string modifiedFileBody,
            string caption,
            string tooltip,
            string leftLabel,
            string rightLabel,
            Func<DifferenceShowerParameters, Task>? frameClosedAction
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

            ModifiedFileBody = modifiedFileBody;
            Caption = caption;
            Tooltip = tooltip;
            LeftLabel = leftLabel;
            RightLabel = rightLabel;
            FrameClosedAction = frameClosedAction;

            LeftFile = TempFile.CreateBasedOfExistingName(
                fileName,
                "current"
                );
            LeftFile.WriteAllText(originalFileBody);

            RightFile = TempFile.CreateBasedOfExistingName(
                fileName,
                "modified"
                );
            RightFile.WriteAllText(originalFileBody);

            ChangedTextAdded = false;
        }

        public void Dispose()
        {
            LeftFile.Dispose();
            RightFile.Dispose();
        }
    }
}
