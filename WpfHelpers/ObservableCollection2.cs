using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace WpfHelpers
{
    /// <summary>
    /// <see cref="ObservableCollection{T}"/> extended with bulk operations (<see cref="AddRange"/>,
    /// <see cref="ReverseInsertAt0"/>) that raise one Count/Item[] change plus one Add notification
    /// per item, instead of forcing callers to add items one at a time.
    /// </summary>
    [Serializable]
    public sealed class ObservableCollection2<T> : ObservableCollection<T>
    {
        private readonly IList<T> _items;

        /// <summary>Reaches into the base <see cref="Collection{T}"/> via reflection to get direct list access, since the base class exposes no protected list accessor usable here.</summary>
        public ObservableCollection2()
        {
            var itemsField = typeof(Collection<T>).GetField("items", BindingFlags.Instance | BindingFlags.NonPublic);
            _items = (IList<T>)(itemsField.GetValue(this));
        }

        /// <summary>Creates the collection pre-populated with <paramref name="items"/>.</summary>
        public ObservableCollection2(IEnumerable<T> items)
            : this()
        {
            AddRange(items);
        }

        /// <summary>Inserts <paramref name="items"/> at the front, preserving their order, with a single Count/Item[] notification.</summary>
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

        /// <summary>Appends <paramref name="items"/> to the end, with a single Count/Item[] notification plus one Add notification per item.</summary>
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
