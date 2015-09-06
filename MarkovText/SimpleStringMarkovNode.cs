using System;
using System.Collections.Generic;
using System.Text;

namespace MarkovText
{
    public class SimpleStringMarkovNode : IMarkovNode<string>
    {
        public SimpleStringMarkovNode(string word)
        {
            Value = word;
            _next = new Dictionary<string, int>();
            _totalNext = 0;
        }

        public string Value { get; private set; }
        private readonly Dictionary<string, int> _next;
        private int _totalNext;

        public void AddNext(string word)
        {
            if (!_next.ContainsKey(word))
                _next.Add(word, 0);
            _next[word]++;
            _totalNext++;
        }

        public string GetRandomNext()
        {
            if (_totalNext == 0)
                return null;

            int i = RandomHelper.Get(_totalNext);
            foreach (KeyValuePair<string, int> kvp in _next)
            {
                i -= kvp.Value;
                if (i <= 0)
                    return kvp.Key;
            }
            throw new Exception("This shouldn't happen");
        }

        public void Dump(StringBuilder sb)
        {
            sb.AppendFormat("\t\"{0}\" : {{\n", Value);
            const string indent = "\t\t";
            //_next.DumpJson(indent, sb);
            sb.AppendFormat("\t}}\n");
        }
    }
}