using System.Collections.Generic;
using System.Linq;

namespace MarkovTest
{
    public class RingBuffer
    {
        private readonly List<string> _buffer;
        private int _currentPosition;
        private readonly int _size;

        public RingBuffer(int i)
        {
            _buffer = Enumerable.Range(0, i).Select(x => (string)null).ToList();
            _size = i;
            _currentPosition = -1;
        }

        public void Add(string s)
        {
            _currentPosition = (_currentPosition + 1) % _size;
            _buffer[_currentPosition] = s;
        }

        public IEnumerable<string> GetAll()
        {
            for (int i = 0; i < _size; i++)
            {
                int idx = (_currentPosition + 1 + i) % _size;
                string s = _buffer[idx];
                if (s == null)
                    break;
                yield return s;
            }
        }

        public string GetKey()
        {
            return string.Join("|", GetAll());
        }
    }
}
