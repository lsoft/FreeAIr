using FreeAIr.UI.Windows;
using System.Collections.Generic;
using FreeAIr.Chat.Context;

namespace FreeAIr.UI.ViewModels
{
    public sealed class AddRelatedItemsToContextBackgroundTask : BackgroundTask
    {
        private readonly ChatContextItemViewModel _viewModel;

        public override string TaskDescription => FreeAIr.Resources.Resources.Please_wait_for_the_code_dependencies;

        public IReadOnlyList<IChatContextItem>? Result
        {
            get;
            private set;
        }

        public AddRelatedItemsToContextBackgroundTask(
            ChatContextItemViewModel viewModel
            )
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;

            StartAsyncTask();
        }

        protected override async Task RunWorkingTaskAsync(
            )
        {
            //in case of exception set it null first
            Result = null;

            Result = await _viewModel.ContextItem.SearchRelatedContextItemsAsync();
        }
    }

}
