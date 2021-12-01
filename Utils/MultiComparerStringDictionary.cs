using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public class MultiComparerStringDictionary<T>
    {
        private readonly List<Dictionary<string, T>> _dicts = new List<Dictionary<string, T>>();

        public int Count => _dicts[0].Count;

        public ICollection<string> Keys => _dicts[0].Keys;

        public ICollection<T> Values => _dicts[0].Values;

        public T this[string key]
        {
            get
            {
                if (TryGetValue(key, out var v))
                    return v;
                throw new KeyNotFoundException(key);
            }
            set
            {
                Add(key, value);
            }
        }

        public MultiComparerStringDictionary(params StringComparer[] comparers)
        {
            if (!(comparers?.Length > 0))
                throw new ArgumentException(nameof(comparers));
            foreach (var c in comparers)
                _dicts.Add(new Dictionary<string, T>(c));
        }

        public bool ContainsKey(string key)
        {
            foreach (var d in _dicts)
                if (d.ContainsKey(key))
                    return true;
            return false;
        }

        public void Add(string key, T value)
        {
            foreach (var d in _dicts)
                d[key] = value;
        }

        public bool Remove(string key)
        {
            var result = false;
            foreach (var d in _dicts)
                if (d.Remove(key))
                    result = true;
            return result;
        }

        public bool TryGetValue(string key, out T value)
        {
            foreach (var d in _dicts)
                if (d.TryGetValue(key, out value))
                    return true;
            value = default(T);
            return false;
        }

        public void Clear()
        {
            foreach (var d in _dicts)
                d.Clear();
        }

        public T[] GetItems() => _dicts[0].Values.ToArray();
    }
}
