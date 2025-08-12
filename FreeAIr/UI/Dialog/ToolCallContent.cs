using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.Dialog
{
    public sealed class ToolCallContent : DialogContent
    {
        private ICommand _clickCommand;
        private ICommand _allowThisToolAllTimeCommand;
        private ICommand _allowAnyToolAllTimeCommand;

        public ToolCallStatusEnum Status
        {
            get;
            private set;
        }

        public string Name
        {
            get;
        }

        public string UIDescription
        {
            get
            {
                switch (Status)
                {
                    case ToolCallStatusEnum.Asking:
                        return $"Run {Name} tool once";
                    case ToolCallStatusEnum.Executing:
                        return $"Tool {Name} is executing...";
                    case ToolCallStatusEnum.Executed:
                        return $"Tool {Name} executed";
                    case ToolCallStatusEnum.Blocked:
                        return $"Tool {Name} is blocked";
                }

                return string.Empty;
            }
        }

        public ICommand ClickCommand
        {
            get
            {
                if (_clickCommand is null)
                {
                    _clickCommand = new RelayCommand(
                        a =>
                        {
                            Status = ToolCallStatusEnum.Executing;

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (Status == ToolCallStatusEnum.Asking)
                            {
                                return true;
                            }

                            return false;
                        });
                }

                return _clickCommand;
            }
        }

        public ICommand AllowThisToolAllTimeCommand
        {
            get
            {
                if (_allowThisToolAllTimeCommand is null)
                {
                    _allowThisToolAllTimeCommand = new RelayCommand(
                        a =>
                        {
                            Status = ToolCallStatusEnum.Executing;

                            AllowThisToolAllTime();

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (Status == ToolCallStatusEnum.Asking)
                            {
                                return true;
                            }

                            return false;
                        });
                }

                return _allowThisToolAllTimeCommand;
            }
        }

        public ICommand AllowAnyToolAllTimeCommand
        {
            get
            {
                if (_allowAnyToolAllTimeCommand is null)
                {
                    _allowAnyToolAllTimeCommand = new RelayCommand(
                        a =>
                        {
                            Status = ToolCallStatusEnum.Executing;

                            AllowAnyToolAllTime();

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (Status == ToolCallStatusEnum.Asking)
                            {
                                return true;
                            }

                            return false;
                        });
                }

                return _allowAnyToolAllTimeCommand;
            }
        }

        public ToolCallContent(
            string name
            ) : base(DialogContentTypeEnum.ToolCall, null)
        {
            Status = ToolCallStatusEnum.Asking;
            Name = name;
        }

        private void AllowThisToolAllTime()
        {
        }

        private void AllowAnyToolAllTime()
        {
        }



        public enum ToolCallStatusEnum
        {
            Asking,
            Executing,
            Executed,
            Blocked
        }
    }
}