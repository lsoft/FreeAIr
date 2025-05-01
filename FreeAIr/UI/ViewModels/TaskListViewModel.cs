using EnvDTE;
using FreeAIr.BLogic.Tasks;
using FreeAIr.Helper;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(TaskListViewModel))]
    public sealed class TaskListViewModel : BaseViewModel
    {
        private readonly AITaskContainer _taskContainer;

        private AITaskWrapper _selectedTask;
        
        private ICommand _openAsMdCommand;
        private ICommand _removeCommand;
        private ICommand _stopCommand;

        public ObservableCollection2<AITaskWrapper> TaskList
        {
            get;
        }

        public AITaskWrapper? SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;

                OnPropertyChanged();
            }
        }

        public string SelectedTaskResponse
        {
            get
            {
                if (_selectedTask is null)
                {
                    return string.Empty;
                }

                return _selectedTask.Task.ReadResponse();
            }
        }

        public ICommand OpenAsMdCommand
        {
            get
            {
                if (_openAsMdCommand == null)
                {
                    _openAsMdCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await VS.Documents.OpenAsync(_selectedTask.Task.ResultFilePath);
                        },
                        a => _selectedTask is not null && _selectedTask.Task.Status == AITaskStatusEnum.Completed
                        );
                }

                return _openAsMdCommand;
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                if (_removeCommand == null)
                {
                    _removeCommand = new RelayCommand(
                        a =>
                        {
                            _taskContainer.RemoveTaskAsync(_selectedTask.Task)
                                .FileAndForget(nameof(AITaskContainer.RemoveTaskAsync));
                        },
                        a => _selectedTask is not null && _selectedTask.Task.Status == AITaskStatusEnum.Completed
                        );
                }

                return _removeCommand;
            }
        }

        public ICommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                {
                    _stopCommand = new RelayCommand(
                        a =>
                        {
                            _taskContainer.StopTaskAsync(_selectedTask.Task)
                                .FileAndForget(nameof(AITaskContainer.StopTaskAsync));
                        },
                        a => _selectedTask is not null && _selectedTask.Task.Status.In(AITaskStatusEnum.WaitForAnswer, AITaskStatusEnum.ReadAnswer)
                        );
                }

                return _stopCommand;
            }
        }

        [ImportingConstructor]
        public TaskListViewModel(
            AITaskContainer taskContainer
            )
        {
            if (taskContainer is null)
            {
                throw new ArgumentNullException(nameof(taskContainer));
            }

            _taskContainer = taskContainer;

            taskContainer.TaskCollectionChangedEvent += TaskCollectionChanged;
            taskContainer.TaskStatusChangedEvent += TaskStatusChanged;

            TaskList = new ObservableCollection2<AITaskWrapper>();
            UpdateControl();

        }

        private async void TaskCollectionChanged(object sender, EventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                UpdateControl();
            }
            catch (Exception excp)
            {
                //todo
            }
        }

        private async void TaskStatusChanged(object sender, TaskEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                foreach (var task in TaskList)
                {
                    task.Update();
                }

                OnPropertyChanged();
            }
            catch (Exception excp)
            {
                //todo
            }
        }

        private void UpdateControl()
        {
            SelectedTask = null;

            TaskList.Clear();
            TaskList.AddRange(
                _taskContainer.Tasks.Reverse().Select(t => new AITaskWrapper(t))
                );
        }

        public sealed class AITaskWrapper : BaseViewModel
        {
            public AITask Task
            {
                get;
            }

            public string FirstRow
            {
                get
                {
                    return Task.Kind.Kind.AsString();
                }
            }

            public string SecondRow
            {
                get
                {
                    return Task.Kind.FileName;
                }
            }

            public string ThirdRow
            {
                get
                {
                    return Task.Started.HasValue
                        ? Task.Started.Value.ToString()
                        : string.Empty
                        ;
                }
            }

            public string FourthRow
            {
                get
                {
                    return Task.Status.AsString();
                }
            }

            public AITaskWrapper(
                AITask task
                )
            {
                if (task is null)
                {
                    throw new ArgumentNullException(nameof(task));
                }

                Task = task;
            }

            public void Update()
            {
                OnPropertyChanged();
            }
        }
    }
}
