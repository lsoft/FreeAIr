using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using FreeAIr.Record;
using FreeAIr.Shared.Helper;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    public sealed class RecorderTranscriberPostProcessor
    {
        private readonly AsyncAwaitProductionCycle<RecordTranscribeResult> _productionCycle = new();

        private readonly Task _task;

        private CancellationTokenSource? _recordingCancellation;
        private RecordingProcessStatusEnum _recordingProcessStatus = RecordingProcessStatusEnum.Idle;

        public RecordingProcessStatusEnum RecordingProcessStatus => _recordingProcessStatus;

        public event RecordingStatusChangedDelegate RecordingStatusChangedSignal;

        public bool IsWorking => RecordingProcessStatus.NotIn(RecordingProcessStatusEnum.Idle);

        public RecorderTranscriberPostProcessor(
            )
        {
            _task = WorkAsync();
        }

        public async Task<RecordTranscribeResult?> RecordTranscribeAndPostProcessAsync(
            )
        {
            if (IsWorking)
            {
                return null;
            }
            
            var result = await _productionCycle.StartCycleAndWaitForProductAsync();
            return result;
        }

        public Task StopRecordingAsync()
        {
            if (!IsWorking)
            {
                return Task.CompletedTask;
            }

            _recordingCancellation?.Cancel();

            return Task.CompletedTask;
        }

        private async Task WorkAsync()
        {
            await TaskScheduler.Default;

            ChosenRecorder.RecorderStatusChangedSignal += RecorderStatusChangedSignal;

            try
            {
                while (true)
                {
                    UpdateRecordingProcessStatus(RecordingProcessStatusEnum.Idle);


                    await _productionCycle.WaitForCycleStartedAsync();
                    
                    RecordTranscribeResult? product = null;
                    try
                    {
                        product = await ProduceProductAsync();
                    }
                    finally
                    {
                        //finally needs in case of exception
                        //we MUST set cycle product otherwise deadlock happens
                        _productionCycle.SetCycleProduct(product);
                    }
                }
            }
            finally
            {
                ChosenRecorder.RecorderStatusChangedSignal -= RecorderStatusChangedSignal;
            }
        }

        private async Task<RecordTranscribeResult> ProduceProductAsync(
            )
        {
            RefreshCancellation();

            var recorder = await ChosenRecorder.GetRecorderAsync();

            var transcribeResult = await recorder.RecordAndTranscribeAsync(
                _recordingCancellation.Token
                );
            
            if (transcribeResult.TryGetText(out var text) && !string.IsNullOrEmpty(text))
            {
                var ppText = await PostProcessAsync(text);

                return RecordTranscribeResult.FromSuccess(ppText);
            }
            
            if (transcribeResult.TryGetError(out _))
            {
                return transcribeResult;
            }

            return null;
        }

        private void RefreshCancellation()
        {
            if (_recordingCancellation is not null)
            {
                _recordingCancellation.Dispose();
            }
            _recordingCancellation = new();
        }

        private void RecorderStatusChangedSignal(
            IRecorder sender,
            RecorderStatusEnum newStatus
            )
        {
            switch (newStatus)
            {
                case RecorderStatusEnum.Idle:
                    //Idle статус выставляется за пределами рекордеров; здесь нет нужды их выставлять
                    break;
                case RecorderStatusEnum.Recording:
                    UpdateRecordingProcessStatus(RecordingProcessStatusEnum.Recording);
                    break;
                case RecorderStatusEnum.Transcribing:
                    UpdateRecordingProcessStatus(RecordingProcessStatusEnum.Transcribing);
                    break;
            }
        }

        private async Task<string> PostProcessAsync(
            string text
            )
        {
            var chosenSupportActionName = RecordingPage.Instance.ChosenPostProcessActionName;
            if (string.IsNullOrEmpty(chosenSupportActionName))
            {
                return text;
            }

            var chosenSupportActions = await FreeAIrOptions.DeserializeSupportActionsAsync(
                a =>
                    a.Scopes.Contains(SupportScopeEnum.RecordPostProcess)
                    && a.Name == chosenSupportActionName
                );
            if (chosenSupportActions.Count == 0)
            {
                return text;
            }

            var chosenSupportAction = chosenSupportActions[0];
            if (string.IsNullOrEmpty(chosenSupportAction.AgentName))
            {
                return text;
            }

            var chosenAgent = await FreeAIrOptions.DeserializeAgentByNameAsync(
                chosenSupportAction.AgentName
                );
            if (chosenAgent is null)
            {
                return text;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    null
                    ),
                null,
                await FreeAIr.BLogic.ChatOptions.NoToolAutoProcessedTextResponseAsync(chosenAgent)
                );
            if (chat is null)
            {
                return text;
            }

            var supportContext = await SupportContext.WithRecordedTextAsync(
                text
                );

            var promptText = supportContext.ApplyVariablesToPrompt(
                chosenSupportAction.Prompt
                );

            chat.AddPrompt(
                UserPrompt.CreateTextBasedPrompt(
                    promptText
                    )
                );

            UpdateRecordingProcessStatus(
                RecordingProcessStatusEnum.PostProcessing
                );

            var postProcessedText = await chat.WaitForPromptCleanAnswerAsync(
                Environment.NewLine
                );
            if (string.IsNullOrEmpty(postProcessedText))
            {
                return text;
            }

            return postProcessedText;
        }

        private void UpdateRecordingProcessStatus(
            RecordingProcessStatusEnum status
            )
        {
            _recordingProcessStatus = status;

            RecordingStatusChangedSignal?.Invoke(this, status);
        }

    }

    public delegate void RecordingStatusChangedDelegate(
        RecorderTranscriberPostProcessor sender,
        RecordingProcessStatusEnum newStatus
        );

    public enum RecordingProcessStatusEnum
    {
        Idle,
        Recording,
        Transcribing,
        PostProcessing
    }

}
