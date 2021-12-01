using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SynAudio.Utils
{
    /// <summary>
    /// An optimized version of ObservableCollection<T> which provides additional methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [PropertyChanged.DoNotNotify]
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        public RangeObservableCollection() : base()
        {
        }
        public RangeObservableCollection(IEnumerable<T> collection) : base(collection)
        {
        }
        public RangeObservableCollection(IList<T> list) : base(list)
        {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));
            _suppressNotification = true;
            foreach (T item in list)
                Add(item);
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void RemoveRange(IEnumerable<T> list)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));
            _suppressNotification = true;
            foreach (T item in list)
                Remove(item);
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}