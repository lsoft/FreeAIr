using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace WpfHelpers
{
    [Serializable]
    public sealed class ObservableCollection2<T> : ObservableCollection<T>
    {
        private readonly IList<T> _items;

        public ObservableCollection2()
        {
            var itemsField = typeof (Collection<T>).GetField("items", BindingFlags.Instance | BindingFlags.NonPublic);
            _items = (IList<T>)(itemsField.GetValue(this));
        }

        public void ReverseInsertAt0(
            IEnumerable<T> items
            )
        {
            var cntBefore = _items.Count;

            if (cntBefore == 0)
            {
                var li = 0;
                foreach (var i in items.Reverse())
                {
                    _items.Add(i);
                    li++;
                }

                this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

                for (var cc = 0; cc < li; cc++)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _items[cc], cc));
                }
            }
            else
            {
                foreach (var i in items)
                {
                    _items.Insert(0, i);

                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, i, 0));
                }

                this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            }
        }

        public void AddRange(
            IEnumerable<T> items
            )
        {
            var si = _items.Count;
            var li = si;

            foreach (var i in items)
            {
                _items.Add(i);
                li++;
            }

            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            for (var cc = si; cc < li; cc++)
            {
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _items[cc], cc));
            }
        }
    }
}
