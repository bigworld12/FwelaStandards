using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace FwelaStandards
{
    public interface IMovableList<T> : IList<T>
    {
        public void Move(int oldIndex, int newIndex);
    }
    public delegate void ListItemPropertyChanged<T>(DictionaryList<T> list, T item, PropertyChangedEventArgs args) where T : INotifyPropertyChanged;
    public class DictionaryList<T> : ConcurrentDictionary<string, T>, IMovableList<T>, INotifyCollectionChanged, INotifyPropertyChanged where T : INotifyPropertyChanged
    {
        T IList<T>.this[int index]
        {
            get => this[index.TransformIndex()];
            set => ReplaceOrAdd(index, value);
        }
        private int indexItemsCount;
        private int version;
        public IList<T> AsList => this;



        #region Constructors
        public DictionaryList()
        {
            Init();
        }


        public void Init()
        {
            PreReset += DictionaryList_PreReset;
        }

        private void DictionaryList_PreReset(object sender, EventArgs e)
        {
            for (int i = 0; i < indexItemsCount; i++)
            {
                if (TryRemove((i).TransformIndex(), out var item))
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
        }
        #endregion

        #region Collection
        int ICollection<T>.Count => indexItemsCount;
        bool ICollection<T>.IsReadOnly => false;
        void ICollection<T>.Add(T item)
        {
            AsList.Insert(indexItemsCount, item);
        }
        public event EventHandler? PreReset;
        void ICollection<T>.Clear()
        {
            PreReset?.Invoke(this, EventArgs.Empty);
            indexItemsCount = 0;
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        bool ICollection<T>.Contains(T item)
        {
            return Values.Contains(item);
        }
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            int endC = arrayIndex + AsList.Count;
            for (int i = arrayIndex; i < endC; i++)
            {
                array[i] = AsList[i - arrayIndex];
            }
        }
        bool ICollection<T>.Remove(T item)
        {
            var index = AsList.IndexOf(item);
            if (index == -1) return false;
            AsList.RemoveAt(index);
            return true;
        }
        #endregion

        #region List
        int IList<T>.IndexOf(T target)
        {
            for (int i = 0; i < AsList.Count; i++)
            {
                var item = AsList[i];
                if (EqualityComparer<T>.Default.Equals(target, item))
                {
                    return i;
                }
            }
            return -1;
        }

        void IList<T>.Insert(int index, T newItem)
        {
            if (index > indexItemsCount)
                throw new ArgumentException("Can't have gaps while inserting items");
            //shift
            for (int i = indexItemsCount - 1; i >= index; i--)
            {
                this[(i + 1).TransformIndex()] = this[(i).TransformIndex()];
            }
            this[(index).TransformIndex()] = newItem;
            indexItemsCount++;
            newItem.PropertyChanged += Item_PropertyChanged;
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, index));
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ListItemPropertyChanged?.Invoke(this, (T)sender, e);
        }

        void IList<T>.RemoveAt(int index)
        {
            if (index >= indexItemsCount)
                throw new ArgumentException("Can't remove a non-existing item");
            var item = this[(index).TransformIndex()];
            for (int i = index; i < indexItemsCount - 1; i++)
            {
                this[(i).TransformIndex()] = this[(i + 1).TransformIndex()];
            }
            TryRemove((indexItemsCount - 1).TransformIndex(), out _);
            indexItemsCount--;
            item.PropertyChanged -= Item_PropertyChanged;
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void Move(int oldIndex, int newIndex)
        {
            var item = this[oldIndex.TransformIndex()];
            for (int i = oldIndex; i < indexItemsCount - 1; i++)
            {
                this[i.TransformIndex()] = this[(i + 1).TransformIndex()];
            }
            TryRemove((indexItemsCount - 1).TransformIndex(), out _);
            indexItemsCount--;
            for (int i = indexItemsCount - 1; i >= newIndex; i--)
            {
                this[(i + 1).TransformIndex()] = this[(i).TransformIndex()];
            }
            this[(newIndex).TransformIndex()] = item;
            indexItemsCount++;
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
        }

        private void ReplaceOrAdd(int index, T value)
        {
            if (index > indexItemsCount)
            {
                throw new IndexOutOfRangeException($"index {index} doesn't exist in the list");
            }
            else if (index == indexItemsCount)
            {
                //add
                AsList.Add(value);
            }
            else
            {
                var tr = index.TransformIndex();
                //replace
                if (TryGetValue(tr, out var oldVal))
                {
                    this[tr] = value;
                    oldVal.PropertyChanged -= Item_PropertyChanged;
                    value.PropertyChanged += Item_PropertyChanged;
                    RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldVal, index));
                }
            }
        }

        #endregion

        #region Enumerator
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private DictionaryList<T> parent;
            private int index;
            private int version;
            internal Enumerator(DictionaryList<T> parent) : this()
            {
                this.parent = parent;
                index = 0;
                version = parent.version;
            }

            public T Current { get; private set; }

            object? IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == parent.AsList.Count + 1)
                    {
                        throw new InvalidOperationException("Operation can't happen");
                    }
                    return Current;
                }
            }

            public void Dispose()
            {
            }
            public bool MoveNext()
            {
                if (version == parent.version && (uint)index < (uint)parent.AsList.Count)
                {
                    Current = parent.AsList[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }
            private bool MoveNextRare()
            {
                if (version != parent.version)
                {
                    throw new InvalidOperationException("List changed during enumeration");
                }
                index = parent.AsList.Count + 1;
                Current = default;
                return false;
            }
            public void Reset()
            {
                if (version != parent.version)
                {
                    throw new InvalidOperationException("List changed during enumeration");
                }
                index = 0;
                Current = default;
            }
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }
        #endregion

        #region Notifications
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        public event ListItemPropertyChanged<T>? ListItemPropertyChanged;
        public void RaisePropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        public void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            version++;
            if (args.Action != NotifyCollectionChangedAction.Move && args.Action != NotifyCollectionChangedAction.Replace)
            {
                RaisePropertyChanged(nameof(Count));
            }
            RaisePropertyChanged("Item[]");
            CollectionChanged?.Invoke(this, args);
        }
        #endregion
    }
}
