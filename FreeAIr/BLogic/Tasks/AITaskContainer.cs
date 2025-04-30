using FreeAIr.Helper;
using OpenAI;
using OpenAI.Chat;
using SauronEye.UI.Informer;
using System;
using System.ClientModel;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.BLogic.Tasks
{
    [Export(typeof(AITaskContainer))]
    public sealed class AITaskContainer
    {
        private readonly object _locker = new();

        private readonly UIInformer _uIInformer;
        private readonly List<AITask> _tasks = new();

        public IReadOnlyList<AITask> Tasks => _tasks;

        public event TaskCollectionChangedDelegate TaskCollectionChangedEvent;
        public event TaskStatusChangedDelegate TaskStatusChangedEvent;

        [ImportingConstructor]
        public AITaskContainer(
            UIInformer uiInformer
            )
        {
            if (uiInformer is null)
            {
                throw new ArgumentNullException(nameof(uiInformer));
            }

            _uIInformer = uiInformer;
        }

        public void StartTask(
            TaskKind kind,
            string query
            )
        {
            var task = new AITask(
                kind,
                query
                );
            task.TaskStatusChangedEvent += TaskStatusChanged;

            lock (_locker)
            {
                _tasks.Add(task);
                FireTaskCollection();
            }

            task.Run();
        }

        public async Task RemoveTaskAsync(AITask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (!CheckIfTaskIsInCollection(task))
            {
                return;
            }

            await task.StopAsync();

            task.TaskStatusChangedEvent -= TaskStatusChanged;

            lock (_locker)
            {
                _tasks.Remove(task);
                FireTaskCollection();
            }

            task.Dispose();
        }

        public async Task StopTaskAsync(
            AITask task
            )
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (!CheckIfTaskIsInCollection(task))
            {
                return;
            }

            await task.StopAsync();
        }

        private bool CheckIfTaskIsInCollection(AITask task)
        {
            lock (_locker)
            {
                if (_tasks.Any(t => ReferenceEquals(t, task)))
                {
                    return true;
                }
            }

            return false;
        }

        private void TaskStatusChanged(object sender, TaskEventArgs ea)
        {
            var anyIsInProgress = _tasks.Any(t => t.Status.In(AITaskStatusEnum.WaitForAnswer, AITaskStatusEnum.ReadAnswer));
            if (anyIsInProgress)
            {
                _uIInformer.UpdateUIStatusAsync(TasksStatusEnum.Working);
            }
            else
            {
                _uIInformer.UpdateUIStatusAsync(TasksStatusEnum.Idle);
            }

            FireTaskEvent(ea);
        }

        private void FireTaskCollection()
        {
            var e = TaskCollectionChangedEvent;
            if (e is not null)
            {
                e(this, new EventArgs());
            }
        }
        
        private void FireTaskEvent(TaskEventArgs ea)
        {
            var e = TaskStatusChangedEvent;
            if (e is not null)
            {
                e(this, ea);
            }
        }

    }

    public delegate void TaskCollectionChangedDelegate(object sender, EventArgs e);
}
