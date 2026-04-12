#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FreeAIr.Shared.ish
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    public class cNotifyBase : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged ;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            protected bool SetProperty<T>(ref T field, T value, Action OnOk = null, [CallerMemberName] string propertyName = "")
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return (false);
                field = value;
                OnPropertyChanged(propertyName);
                OnOk?.Invoke();
                return (true);
            }
        }
}
