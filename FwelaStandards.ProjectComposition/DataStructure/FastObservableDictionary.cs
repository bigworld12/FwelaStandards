﻿namespace Catel.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using Catel.IoC;
    using Catel.Logging;
    using Catel.Services;

    public class FastObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>,
        IReadOnlyDictionary<TKey, TValue>,
        IDictionary,
        IList<KeyValuePair<TKey, TValue>>,
        ISerializable,
        IDeserializationCallback,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        #region Fields & Properties
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();


        /// <summary>
        /// maps from TKey to TValue
        /// </summary>
        private readonly Dictionary<TKey, TValue> _dict;

        /// <summary>
        /// maps from TKey to index
        /// </summary>
        private readonly Dictionary<TKey, int> _dictIndexMapping;

        /// <summary>
        /// maps from index to TKey (don't map to TValue here to avoid duplication of value was a ValueType)
        /// 
        /// this is only used to store the order in which keys were entered
        /// </summary>
        private readonly List<TKey> _list;

#if NET || NETCORE
        [field: NonSerialized]
#endif
        private readonly SerializationInfo _serializationInfo;

        /// <summary>
        /// Gets or sets a value indicating whether events should automatically be dispatched to the UI thread.
        /// </summary>
        /// <value><c>true</c> if events should automatically be dispatched to the UI thread; otherwise, <c>false</c>.</value>
        public bool AutomaticallyDispatchChangeNotifications { get; set; } = true;



        /// <see cref="Dictionary{TKey,TValue}.Comparer"/>>
        public IEqualityComparer<TKey> Comparer => _dict.Comparer;
        #endregion

        #region Constructors
        public FastObservableDictionary()
        {
            _dict = new Dictionary<TKey, TValue>();
            _dictIndexMapping = new Dictionary<TKey, int>();
            _list = new List<TKey>();
        }
        public FastObservableDictionary(int capacity)
        {
            _dict = new Dictionary<TKey, TValue>(capacity);
            _dictIndexMapping = new Dictionary<TKey, int>(capacity);
            _list = new List<TKey>(capacity);
        }
        public FastObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> originalDict)
        {
            if (originalDict is ICollection<KeyValuePair<TKey, TValue>> collection)
            {
                _dict = new Dictionary<TKey, TValue>(collection.Count);
                _dictIndexMapping = new Dictionary<TKey, int>(collection.Count);
                _list = new List<TKey>(collection.Count);
            }
            InsertMultipleValues(0, originalDict, false);
        }
        public FastObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            _list = new List<TKey>();
            _dictIndexMapping = new Dictionary<TKey, int>(comparer);
            _dict = new Dictionary<TKey, TValue>(comparer);
        }
        public FastObservableDictionary(IDictionary<TKey, TValue> dictionary) : this((IEnumerable<KeyValuePair<TKey, TValue>>)dictionary)
        {
        }
        public FastObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _list = new List<TKey>(capacity);
            _dictIndexMapping = new Dictionary<TKey, int>(capacity, comparer);
            _dict = new Dictionary<TKey, TValue>(capacity, comparer);
        }
        public FastObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            Argument.IsNotNull(nameof(dictionary), dictionary);

            _dict = new Dictionary<TKey, TValue>(dictionary.Count, comparer);
            _dictIndexMapping = new Dictionary<TKey, int>(dictionary.Count, comparer);
            _list = new List<TKey>(dictionary.Count);
            InsertMultipleValues(dictionary, false);
        }


        private FastObservableDictionary(Dictionary<TKey, TValue> dict, Dictionary<TKey, int> dictIndexMapping, List<TKey> list)
        {
            _dict = dict;
            _list = list;
            _dictIndexMapping = dictIndexMapping;
        }
        #endregion

        #region Methods
        public FastObservableDictionary<TKey, TValue> AsReadOnly()
        {
            return new FastObservableDictionary<TKey, TValue>(_dict, _dictIndexMapping, _list) { IsReadOnly = true };
        }

        /// <summary>
        /// Loops through the dictionary with the same order as the list
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable()
        {
            return _list.Select(x => new KeyValuePair<TKey, TValue>(x, _dict[x]));
        }



        /// <summary>
        /// Inserts a single item into the ObservableDictionary using its key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <param name="checkKeyDuplication"></param>
        public virtual void InsertSingleValue(TKey key, TValue newValue, bool checkKeyDuplication)
        {
            Argument.IsNotNull(nameof(key), key);
            Argument.IsNotNull(nameof(newValue), newValue);
            var changedItem = new KeyValuePair<TKey, TValue>(key, newValue);
            if (checkKeyDuplication && _dictIndexMapping.TryGetValue(key, out var oldIndex) && _dict.TryGetValue(key, out var oldValue))
            {
                //simply change the value
                //raise replace event
                //leave the indexes as is

                //DON'T raise count change
                _dict[key] = newValue;

                OnPropertyChanged(_cachedIndexerArgs);
                OnPropertyChanged(_cachedValuesArgs);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                    changedItem,
                    new KeyValuePair<TKey, TValue>(key, oldValue),
                    oldIndex));

            }
            else
            {

                //append to the end of the list
                var newIndex = Values.Count;
                _dict[key] = newValue;
                _dictIndexMapping[key] = newIndex;
                _list.Add(key);

                OnPropertyChanged(_cachedIndexerArgs);
                OnPropertyChanged(_cachedCountArgs);
                OnPropertyChanged(_cachedKeysArgs);
                OnPropertyChanged(_cachedValuesArgs);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    changedItem,
                    newIndex));
            }
        }

        /// <summary>
        /// Inserts a single item into the ObservableDictionary using its index and key
        /// if a key exists but with a different index, a 'move' operation will occur and the key will be moved to the new location
        /// if a key doesn't exist, an 'add' operation will occur
        /// </summary>
        /// <param name="index"></param>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <param name="checkKeyDuplication"></param>
        public virtual void InsertSingleValue(int index, TKey key, TValue newValue, bool checkKeyDuplication)
        {
            Argument.IsNotNull(nameof(key), key);
            Argument.IsNotNull(nameof(newValue), newValue);

            var changedItem = new KeyValuePair<TKey, TValue>(key, newValue);
            int changedIndex = index;
            if (checkKeyDuplication && _dict.TryGetValue(key, out var oldValue) && _dictIndexMapping.TryGetValue(key, out var oldIndex))
            {
                //DON'T raise count change
                if (oldIndex == index)
                {

                    //raise replace event
                    //leave the indexes as is
                    _dict[key] = newValue;
                    OnPropertyChanged(_cachedIndexerArgs);
                    OnPropertyChanged(_cachedValuesArgs);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                        changedItem,
                        new KeyValuePair<TKey, TValue>(key, oldValue),
                        index));
                }
                else
                {
                    InternalMoveItem(oldIndex, index, key, newValue);
                }
            }
            else
            {

                for (var i = index; i < Count; i++)
                {
                    var keyAtCurrentIndex = _list[i];
                    _dictIndexMapping[keyAtCurrentIndex] = i + 1;
                }
                _list.Insert(index, key);
                _dictIndexMapping[key] = index;

                OnPropertyChanged(_cachedIndexerArgs);
                OnPropertyChanged(_cachedCountArgs);
                OnPropertyChanged(_cachedKeysArgs);
                OnPropertyChanged(_cachedValuesArgs);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                    new KeyValuePair<TKey, TValue>(key, newValue),
                    index));
            }
        }

        public virtual void MoveItem(int oldIndex, int newIndex)
        {
            var key = _list[oldIndex];
            if (_dict.TryGetValue(key, out var oldValue))
            {
                InternalMoveItem(oldIndex, newIndex, key, oldValue);
            }
        }
        protected virtual void InternalMoveItem(int oldIndex, int newIndex, TKey key, TValue element)
        {
            Argument.IsNotNull(nameof(key), key);
            Argument.IsNotNull(nameof(element), element);

            var temp = _list[oldIndex];
            var sign = Math.Sign(newIndex - oldIndex);
            var checkCondition = sign > 0 ? (Func<int, bool>)((int i) => i < newIndex) : ((int i) => i > newIndex);
            for (var i = oldIndex; checkCondition(i); i += sign) //negative sign
            {
                _dictIndexMapping[_list[i] = _list[i + sign]] = i;
            }
            _list[newIndex] = temp;
            _dictIndexMapping[temp] = newIndex;

            var changedItem = new KeyValuePair<TKey, TValue>(key, element);
            OnPropertyChanged(_cachedIndexerArgs);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move,
                changedItem,
                oldIndex,
                newIndex));
        }

        /// <summary>
        /// Removes a single item using its key
        /// </summary>
        /// <param name="keyToRemove"></param>
        /// <param name="value">the removed value</param>
        /// <returns></returns>
        public virtual bool TryRemoveSingleValue(TKey keyToRemove, out TValue value)
        {
            Argument.IsNotNull(nameof(keyToRemove), keyToRemove);
            if (_dictIndexMapping.TryGetValue(keyToRemove, out var removedKeyIndex) && _dict.TryGetValue(keyToRemove, out var removedKeyValue))
            {

                _list.RemoveAt(removedKeyIndex);
                _dict.Remove(keyToRemove);

                for (var i = removedKeyIndex; i < _list.Count; i++)
                {
                    var curKey = _list[i];
                    _dictIndexMapping[curKey] = i;
                }

                OnPropertyChanged(_cachedIndexerArgs);
                OnPropertyChanged(_cachedCountArgs);
                OnPropertyChanged(_cachedKeysArgs);
                OnPropertyChanged(_cachedValuesArgs);

                var changedItem = new KeyValuePair<TKey, TValue>(keyToRemove, removedKeyValue);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    changedItem,
                    removedKeyIndex));

                //raise remove event 
                value = removedKeyValue;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// removes a single item using its index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <exception cref="IndexOutOfRangeException">if the index is outside boundaries</exception>
        public virtual void RemoveSingleValue(int index, out TValue value)
        {
            var keyToRemove = _list[index];
            value = _dict[keyToRemove];
            _dict.Remove(keyToRemove);
            _list.RemoveAt(index);
            for (var i = index; i < Count; i++)
            {
                var key = _list[i];
                _dictIndexMapping[key] = i;
            }
            OnPropertyChanged(_cachedIndexerArgs);
            OnPropertyChanged(_cachedCountArgs);
            OnPropertyChanged(_cachedKeysArgs);
            OnPropertyChanged(_cachedValuesArgs);
            var changedItem = new KeyValuePair<TKey, TValue>(keyToRemove, value);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    changedItem,
                    index));

        }


        /// <summary>
        /// Append or replace multiple values
        /// </summary>
        /// <param name="newValues"></param>
        /// <param name="checkKeyDuplication"></param>
        public virtual void InsertMultipleValues(IEnumerable<KeyValuePair<TKey, TValue>> newValues, bool checkKeyDuplication)
        {
            Argument.IsNotNull(nameof(newValues), newValues);

            if (checkKeyDuplication)
            {
                newValues = newValues.Where(x => !_dict.ContainsKey(x.Key));
            }
            var startIndex = _list.Count;
            if (newValues is ICollection<KeyValuePair<TKey, TValue>> collection)
            {
                var counterIndex = startIndex;
                _list.AddRange(newValues.Select(x => x.Key));
                foreach (var item in collection)
                {
                    _dict[item.Key] = item.Value;
                    _dictIndexMapping[item.Key] = counterIndex++;
                }

                OnPropertyChanged(_cachedIndexerArgs);
                OnPropertyChanged(_cachedCountArgs);
                OnPropertyChanged(_cachedKeysArgs);
                OnPropertyChanged(_cachedValuesArgs);

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection, startIndex));
            }
            else
            {
                foreach (var item in newValues)
                {
                    InsertSingleValue(item.Key, item.Value, checkKeyDuplication);
                }                
            }
        }


        /// <summary>
        /// Inserts multiple values with a start index
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="newValues"></param>
        /// <param name="checkKeyDuplication">only set to false if you are absolutely sure there is never going to be key duplication (e.g. during construction)</param>
        public virtual void InsertMultipleValues(int startIndex, IEnumerable<KeyValuePair<TKey, TValue>> newValues, bool checkKeyDuplication)
        {
            Argument.IsNotNull(nameof(newValues), newValues);

            if (checkKeyDuplication)
            {
                newValues = newValues.Where(x => !_dict.ContainsKey(x.Key));
            }
            var counterIndex = startIndex;
            if (newValues is ICollection<KeyValuePair<TKey, TValue>> collection)
            {
                var count = collection.Count;
                _list.InsertRange(startIndex, newValues.Select(x => x.Key));
                foreach (var item in collection)
                {
                    _dict[item.Key] = item.Value;
                    _dictIndexMapping[item.Key] = counterIndex++;
                }

                OnPropertyChanged(_cachedIndexerArgs);
                OnPropertyChanged(_cachedCountArgs);
                OnPropertyChanged(_cachedKeysArgs);
                OnPropertyChanged(_cachedValuesArgs);

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection, startIndex));
            }
            else
            {
                foreach (var item in newValues)
                {
                    InsertSingleValue(counterIndex++, item.Key, item.Value, checkKeyDuplication);
                }              
            }
        }


        /// <summary>
        /// removes a range of values
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        public virtual void RemoveMultipleValues(int startIndex, int count)
        {
            var lastIndex = startIndex + count - 1;
            var removed = new List<KeyValuePair<TKey, TValue>>();
            for (var i = startIndex; i <= lastIndex; i++)
            {
                var key = _list[i];
                if (_dict.TryGetValue(key, out var val))
                {
                    removed.Add(new KeyValuePair<TKey, TValue>(key, val));
                    _dict.Remove(key);
                    _dictIndexMapping.Remove(key);
                }
            }
            _list.RemoveRange(startIndex, count);

            OnPropertyChanged(_cachedIndexerArgs);
            OnPropertyChanged(_cachedCountArgs);
            OnPropertyChanged(_cachedKeysArgs);
            OnPropertyChanged(_cachedValuesArgs);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, startIndex));
        }

        /// <summary>
        /// Removes multiple keys from the dictionary
        /// </summary>
        /// <param name="keysToRemove">The keys to remove</param>
        public virtual void RemoveMultipleValues(IEnumerable<TKey> keysToRemove)
        {
            Argument.IsNotNull(nameof(keysToRemove), keysToRemove);

            Dictionary<int, List<KeyValuePair<TKey, TValue>>> removedList;
            if (keysToRemove is ICollection<TKey> collectionOfKeysToRemove)
            {
                removedList = new Dictionary<int, List<KeyValuePair<TKey, TValue>>>(collectionOfKeysToRemove.Count);
            }
            else
            {
                removedList = new Dictionary<int, List<KeyValuePair<TKey, TValue>>>();
            }
            foreach (var keyToRemove in keysToRemove)
            {
                if (_dictIndexMapping.TryGetValue(keyToRemove, out var removedKeyIndex) && _dict.TryGetValue(keyToRemove, out var valueToRemove))
                {
                    if (removedList.TryGetValue(removedKeyIndex - 1, out var toRemoveList))
                        toRemoveList.Add(new KeyValuePair<TKey, TValue>(keyToRemove, valueToRemove));
                    else
                        removedList[removedKeyIndex] = new List<KeyValuePair<TKey, TValue>>() { new KeyValuePair<TKey, TValue>(keyToRemove, valueToRemove) };

                    _dict.Remove(keyToRemove);
                    _dictIndexMapping.Remove(keyToRemove);
                }
            }

            OnPropertyChanged(_cachedIndexerArgs);
            OnPropertyChanged(_cachedCountArgs);
            OnPropertyChanged(_cachedKeysArgs);
            OnPropertyChanged(_cachedValuesArgs);
            foreach (var range in removedList)
            {
                _list.RemoveRange(range.Key, range.Value.Count);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, range.Value, range.Key));
            }
        }


        public virtual void RemoveAllItems()
        {
            _list.Clear();
            _dict.Clear();

            OnPropertyChanged(_cachedIndexerArgs);
            OnPropertyChanged(_cachedCountArgs);
            OnPropertyChanged(_cachedKeysArgs);
            OnPropertyChanged(_cachedValuesArgs);

            OnCollectionChanged(_cachedResetArgs);
        }


        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>>
        public int Count => _list.Count;
        public bool IsReadOnly { get; private set; }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Argument.IsNotNull(nameof(item), item);
            InsertSingleValue(item.Key, item.Value, true);
        }

        public void Clear()
        {
            RemoveAllItems();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            Argument.IsNotNull(nameof(item), item);

            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Argument.IsNotNull(nameof(array), array);
            if (array.Length - arrayIndex < Count) throw new IndexOutOfRangeException("Array doesn't have enough space to copy all the elements");
            for (var i = 0; i < Count; i++)
            {
                var key = _list[i];
                var value = _dict[key];
                array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(key, value);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            Argument.IsNotNull(nameof(item), item);
            return TryRemoveSingleValue(item.Key, out _);
        }
        #endregion

        #region IDictionary<TKey,TValue>
        public TValue this[TKey key]
        {
            get => _dict[key];
            set => InsertSingleValue(key, value, true);
        }

        public ICollection<TKey> Keys => _dict.Keys;
        public ICollection<TValue> Values => _dict.Values;

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            InsertSingleValue(key, value, true);
        }

        public bool Remove(TKey keyToRemove)
        {
            return TryRemoveSingleValue(keyToRemove, out _);
        }

        /// <summary>
        /// Attempts to get the value from a key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return AsEnumerable().GetEnumerator();
        }
        #endregion

        #region IReadOnlyDictionary<TKey,TValue>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dict.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dict.Values;
        #endregion

        #region IDictionary
        ICollection IDictionary.Keys => _dict.Keys;
        ICollection IDictionary.Values => _dict.Values;

        public bool IsFixedSize => ((IDictionary)_dict).IsFixedSize;
        public object SyncRoot => ((IDictionary)_dict).SyncRoot;
        public bool IsSynchronized => ((IDictionary)_dict).IsSynchronized;


        /// <summary>
        /// Gets or sets the value with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key, or <see langword="null" /> if <paramref name="key"/> is not in the dictionary or <paramref name="key"/> is of a type that is not assignable to the key of type <typeparamref name="TKey"/> of the <see cref="ObservableDictionary{TKey,TValue}"/>.</returns>
        public object this[object key]
        {
            get
            {
                if (key is TKey castedKey)
                {
                    return this[castedKey];
                }
                else
                {
                    return null;
                }
            }
            set => this[(TKey)key] = (TValue)value;
        }

        /// <summary>
        /// checks if the 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(object key)
        {
            if (key is TKey castedKey)
            {
                return ContainsKey(castedKey);
            }
            return false;
        }
        public void Add(object key, object value)
        {
            Argument.IsNotNull(nameof(key), key);
            Argument.IsNotNull(nameof(value), value);
            if (key is TKey castedKey)
            {
                if (value is TValue castedValue)
                {
                    InsertSingleValue(castedKey, castedValue, true);
                }
                else
                {
                    throw Log.ErrorAndCreateException<InvalidCastException>($"Value must be of type {typeof(TValue)}");
                }
            }
            else
            {
                throw Log.ErrorAndCreateException<InvalidCastException>($"Key must be of type {typeof(TKey)}");
            }
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            if (key is TKey castedKey)
            {
                TryRemoveSingleValue(castedKey, out _);
            }
        }

        public void CopyTo(Array array, int arrayIndex)
        {
            if (array.Length - arrayIndex < Count) throw new IndexOutOfRangeException("Array doesn't have enough space to copy all the elements");
            for (var i = 0; i < Count; i++)
            {
                var key = _list[i];
                var value = _dict[key];
                array.SetValue(new KeyValuePair<TKey, TValue>(key, value), arrayIndex + i);
            }
        }
        #endregion

        #region IList<KeyValuePair<TKey,TValue>>
        KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index]
        {
            get
            {
                var key = _list[index];
                return new KeyValuePair<TKey, TValue>(key, _dict[key]);
            }
            set => InsertSingleValue(index, value.Key, value.Value, true);
        }

        public int IndexOf(KeyValuePair<TKey, TValue> item)
        {
            return _dictIndexMapping[item.Key];
        }

        public void Insert(int index, KeyValuePair<TKey, TValue> item)
        {
            InsertSingleValue(index, item.Key, item.Value, true);
        }

        public void RemoveAt(int index)
        {
            RemoveSingleValue(index, out _);
        }
        #endregion

        #region ISerializable and IDeserializationCallback
        private const string entriesName = "entries";
        protected FastObservableDictionary(SerializationInfo info, StreamingContext context)
        {
            _serializationInfo = info;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            var entries = new Collection<KeyValuePair<TKey, TValue>>();
            foreach (var item in AsEnumerable())
            {
                entries.Add(item);
            }

            info.AddValue(entriesName, entries);
        }

        public void OnDeserialization(object sender)
        {
            if (_serializationInfo is null)
            {
                return;
            }

            var entries =
                (Collection<KeyValuePair<TKey, TValue>>)_serializationInfo.GetValue(entriesName, typeof(Collection<KeyValuePair<TKey, TValue>>));

            foreach (var keyValuePair in entries)
            {
                Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        #endregion

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        {
            return AsEnumerable().GetEnumerator();
        }


        #endregion

        #region INotifyPropertyChanged and INotifyCollectionChanged
        protected readonly PropertyChangedEventArgs _cachedIndexerArgs = new PropertyChangedEventArgs("Item[]");
        protected readonly PropertyChangedEventArgs _cachedCountArgs = new PropertyChangedEventArgs(nameof(Count));
        protected readonly PropertyChangedEventArgs _cachedKeysArgs = new PropertyChangedEventArgs(nameof(Keys));
        protected readonly PropertyChangedEventArgs _cachedValuesArgs = new PropertyChangedEventArgs(nameof(Values));

        protected readonly NotifyCollectionChangedEventArgs _cachedResetArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);


        protected virtual void OnPropertyChanged(PropertyChangedEventArgs eventArgs)
        {
            PropertyChanged?.Invoke(this, eventArgs);
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs eventArgs)
        {

            CollectionChanged?.Invoke(this, eventArgs);
        }

        /// <inheritdoc cref="INotifyCollectionChanged.CollectionChanged"/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    

    }
}