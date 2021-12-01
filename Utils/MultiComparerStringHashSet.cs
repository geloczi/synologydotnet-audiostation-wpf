using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public class MultiComparerStringHashSet
    {
        private readonly List<HashSet<string>> _hashSets = new List<HashSet<string>>();

        public int Count => _hashSets[0].Count;

        public bool IsReadOnly => throw new NotImplementedException();

        public MultiComparerStringHashSet(params StringComparer[] comparers)
        {
            if (!(comparers?.Length > 0))
                throw new ArgumentException(nameof(comparers));
            foreach (var c in comparers)
                _hashSets.Add(new HashSet<string>(c));
        }

        public bool Add(string s)
        {
            bool result = false;
            foreach (var hs in _hashSets)
                if (hs.Add(s))
                    result = true;
            return result;
        }

        public bool Remove(string s)
        {
            bool result = false;
            foreach (var hs in _hashSets)
                if (hs.Remove(s))
                    result = true;
            return result;
        }

        public bool Contains(string s)
        {
            foreach (var hs in _hashSets)
                if (hs.Contains(s))
                    return true;
            return false;
        }

        public void Clear()
        {
            foreach (var hs in _hashSets)
                hs.Clear();
        }

        public string[] GetItems() => _hashSets[0].ToArray();
    }
}
