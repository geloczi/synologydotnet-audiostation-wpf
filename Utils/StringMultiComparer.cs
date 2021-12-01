using System;

namespace Utils
{
    public class StringMultiComparer
    {
        private readonly StringComparison[] _comparions;
        public StringMultiComparer(params StringComparison[] comparisons)
        {
            if (!(comparisons?.Length > 0))
                throw new ArgumentException(nameof(comparisons));
            _comparions = comparisons;
        }

        public bool Equals(string s1, string s2)
        {
            if (s1 is null ^ s2 is null)
                return false;
            if (s1 is null)
                return true;
            foreach (var c in _comparions)
            {
                if (string.Compare(s1, s2, c) == 0)
                    return true;
            }
            return false;
        }

        public bool Equals(string[] s1, string[] s2)
        {
            if (s1 is null ^ s2 is null)
                return false;
            if (s1 is null)
                return true;
            if (s1.Length != s2.Length)
                return false;
            if (s1.Length == 0)
                return true;
            for (int i = 0; i < s1.Length; i++)
            {
                if (!Equals(s1[i], s2[i]))
                    return false;
            }
            return true;
        }
    }
}
