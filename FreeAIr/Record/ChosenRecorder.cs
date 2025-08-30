using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.Record.Fake;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Windows;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record
{
    public static class ChosenRecorder
    {
        private static IRecorder? _currentRecorder;

        public static event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        public static async Task InitAsync()
        {
            await SetNewRecorderAsync();
        }

        public static bool IsReady() => _currentRecorder is not null;

        public static string? GetRecorderName()
        {
            return _currentRecorder?.Name;
        }

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
        }

        private static async Task SetNewRecorderAsync()
        {
            var recorderFactories = await RecorderContextMenu.ObtainRecorderFactoriesAsync();
            var recorderFactory = recorderFactories.FirstOrDefault(
                r => r.Name == RecordingPage.Instance.ChosenRecorderName
                ) ?? recorderFactories[0];

            var defaultRecorder = await recorderFactory.CreateRecorderAsync();
            await ReplaceRecorderWithAsync(defaultRecorder);
        }

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

        private static void FireRecorderStatusChangedSignal(
            IRecorder recorder,
            RecorderStatusEnum status
            )
        {
            RecorderStatusChangedSignal?.Invoke(recorder, status);
        }
    }

}
