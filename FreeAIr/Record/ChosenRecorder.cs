using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.Record.Fake;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record
{
    public static class ChosenRecorder
    {
        private static readonly NonDisposableSemaphoreSlim _semaphore = new (1);
        private static IRecorder? _currentRecorder;

        public static event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        static ChosenRecorder()
        {
            GetRecorderAsync()
                .FileAndForget(nameof(GetRecorderAsync));
        }

        //public static RecorderStatusEnum? GetRecorderStatus()
        //{
        //    var taken = false;
        //    try
        //    {
        //        taken = _semaphore.Wait(TimeSpan.FromMilliseconds(10));

        //        return _currentRecorder?.Status;
        //    }
        //    finally
        //    {
        //        if (taken)
        //        {
        //            _semaphore.Release();
        //        }
        //    }
        //}

        public static string? GetRecorderName()
        {
            var taken = false;
            try
            {
                taken = _semaphore.Wait(TimeSpan.FromMilliseconds(10));

                return _currentRecorder?.Name;
            }
            finally
            {
                if (taken)
                {
                    _semaphore.Release();
                }
            }
        }

        public static async Task<IRecorder> GetRecorderAsync()
        {
            try
            {
                await _semaphore.WaitAsync();

                if (_currentRecorder is null)
                {
                    var recorderFactories = await RecorderContextMenu.ObtainRecorderFactoriesAsync();
                    var recorderFactory = recorderFactories[0];
                    _currentRecorder = await recorderFactory.CreateRecorderAsync();
                }

                return _currentRecorder;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
            finally
            {
                _semaphore.Release();
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

        private static async Task ProcessNewRecorderAsync(
            IRecorderFactory chosenRecorderFactory
            )
        {
            var recorderConfigurationControl = chosenRecorderFactory.CreateConfigurationControl();

            await RecorderSetupWindow.ShowAsync(
                recorderConfigurationControl,
                new System.Windows.Point(
                    System.Windows.Forms.Cursor.Position.X,
                    System.Windows.Forms.Cursor.Position.Y
                    )
                );

            var recorder = await chosenRecorderFactory.CreateRecorderAsync();

            await ReplaceRecorderWithAsync(recorder);
        }

        private static async Task ReplaceRecorderWithAsync(
            IRecorder recorder
            )
        {
            try
            {
                await _semaphore.WaitAsync();

                if (_currentRecorder is not null)
                {
                    _currentRecorder.RecorderStatusChangedSignal -= FireRecorderStatusChangedSignal;
                    await _currentRecorder.DisposeAsync();
                }

                _currentRecorder = recorder;
                _currentRecorder.RecorderStatusChangedSignal += FireRecorderStatusChangedSignal;
            }
            finally
            {
                _semaphore.Release();
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
