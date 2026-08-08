using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.Record.Fake;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record
{
    /// <summary>
    /// The currently active speech-to-text backend.
    ///
    /// FreeAIr ships several recorders (Microsoft Speech API, WinRT, local Whisper.Net, Whisper
    /// over an OpenAI-compatible endpoint) and the user picks one at run time; the choice is kept
    /// in <see cref="RecordingPage"/>. This class hides the swap from the rest of the code: it
    /// always hands out something, falling back to <see cref="FakeRecorder"/> when the chosen
    /// backend cannot be created (no model file, no microphone, no token, ...).
    ///
    /// Only the recording itself lives here; the record → transcribe → post-process pipeline is
    /// <see cref="FreeAIr.BLogic.RecorderTranscriberPostProcessor"/>.
    /// </summary>
    public static class ChosenRecorder
    {
        private static IRecorder? _currentRecorder;

        /// <summary>Fires whenever the active recorder's idle/recording/transcribing state changes, so the UI can update its icon.</summary>
        public static event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        /// <summary>Creates the recorder named in <see cref="RecordingPage"/>, falling back to <see cref="FakeRecorder"/> if that fails. Called once during package startup.</summary>
        public static async Task InitAsync()
        {
            try
            {
                await SetNewRecorderAsync();
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                await ReplaceRecorderWithAsync(FakeRecorder.Instance);
            }
        }

        /// <summary>Whether a recorder has already been created, without triggering creation as a side effect.</summary>
        public static bool IsReady() => _currentRecorder is not null;

        /// <summary>The active recorder's display name, or null before one has been created.</summary>
        public static string? GetRecorderName()
        {
            return _currentRecorder?.Name;
        }

        /// <summary>The active recorder, creating the default one first if none exists yet. Falls back to <see cref="FakeRecorder"/> on any failure so callers never get null.</summary>
        public static async Task<IRecorder> GetRecorderAsync()
        {
            try
            {
                if (_currentRecorder is null)
                {
                    await SetNewRecorderAsync();
                }

                return _currentRecorder;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return FakeRecorder.Instance;
        }

        /// <summary>
        /// Shows the recorder menu and applies whatever the user picked there: another backend
        /// (possibly with its own configuration window), another post-process support action, or
        /// one of the service commands.
        /// </summary>
        public static async Task ChooseRecorderAsync(
            )
        {
            var chosenMenuItem = await RecorderContextMenu.OpenRecorderAndPostProcessMenuAsync(
                RecordingPage.Instance.ChosenRecorderName,
                RecordingPage.Instance.ChosenPostProcessActionName
                );
            if (chosenMenuItem is null)
            {
                return;
            }

            if (chosenMenuItem is IRecorderFactory chosenRecorderFactory)
            {
                await ProcessNewRecorderAsync(
                    chosenRecorderFactory
                    );

                RecordingPage.Instance.ChosenRecorderName = chosenRecorderFactory.Name;
                await RecordingPage.Instance.SaveAsync();
            }
            else if (chosenMenuItem is SupportActionJson chosenAction)
            {
                RecordingPage.Instance.ChosenPostProcessActionName = chosenAction.Name;
                await RecordingPage.Instance.SaveAsync();
            }
            else if (chosenMenuItem is RecordingOtherActionEnum otherAction)
            {
                switch (otherAction)
                {
                    case RecordingOtherActionEnum.EnableDisable:
                        RecordingPage.Instance.Enabled = !RecordingPage.Instance.Enabled;
                        await RecordingPage.Instance.SaveAsync();
                        break;
                    case RecordingOtherActionEnum.ShowHelp:
                        await VS.MessageBox.ShowAsync(
                            FreeAIr.Resources.Resources.You_can_dictate_prompts,
                            buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                            );
                        break;
                }
            }
        }

        /// <summary>Builds the recorder named in <see cref="RecordingPage"/>, or the first known factory's recorder when the saved name no longer matches any.</summary>
        private static async Task SetNewRecorderAsync()
        {
            var recorderFactories = await RecorderContextMenu.ObtainRecorderFactoriesAsync();
            var recorderFactory = recorderFactories.FirstOrDefault(
                r => r.Name == RecordingPage.Instance.ChosenRecorderName
                ) ?? recorderFactories[0];

            var defaultRecorder = await recorderFactory.CreateRecorderAsync();
            await ReplaceRecorderWithAsync(defaultRecorder);
        }

        /// <summary>Shows the chosen backend's configuration window (if it has one) and switches to a recorder built from the answers.</summary>
        private static async Task ProcessNewRecorderAsync(
            IRecorderFactory chosenRecorderFactory
            )
        {
            var recorderConfigurationControl = chosenRecorderFactory.CreateConfigurationControl();
            if (recorderConfigurationControl is not null)
            {
                await RecorderSetupWindow.ShowAsync(
                    recorderConfigurationControl,
                    new System.Windows.Point(
                        System.Windows.Forms.Cursor.Position.X,
                        System.Windows.Forms.Cursor.Position.Y
                        )
                    );
            }

            var recorder = await chosenRecorderFactory.CreateRecorderAsync();

            await ReplaceRecorderWithAsync(recorder);
        }

        /// <summary>
        /// Installs a new recorder and disposes the previous one. The swap is atomic so that a
        /// status signal arriving in the middle cannot be routed to a half-replaced recorder.
        /// </summary>
        private static async Task ReplaceRecorderWithAsync(
            IRecorder newRecorder
            )
        {
            newRecorder.RecorderStatusChangedSignal += FireRecorderStatusChangedSignal;

            var oldRecorder = Interlocked.Exchange(ref _currentRecorder, newRecorder);
            if (oldRecorder is not null)
            {
                oldRecorder.RecorderStatusChangedSignal -= FireRecorderStatusChangedSignal;
                await oldRecorder.DisposeAsync();
            }
        }

        /// <summary>Relays a recorder's own status event out as <see cref="RecorderStatusChangedSignal"/>.</summary>
        private static void FireRecorderStatusChangedSignal(
            IRecorder recorder,
            RecorderStatusEnum status
            )
        {
            RecorderStatusChangedSignal?.Invoke(recorder, status);
        }
    }

}
