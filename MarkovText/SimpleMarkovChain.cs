using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkovText
{
    public class SimpleTextMarkovChain : IMarkovChain<string>
    {
        private readonly Tokenizer _tokenizer;

        public SimpleTextMarkovChain()
        {
            //_depth = depth;
            _tokenizer = new Tokenizer();
            Nodes = new Dictionary<string, IMarkovNode<string>>();
            Nodes.Add(Constants.SentenceStart, new SimpleStringMarkovNode(Constants.SentenceStart));
            Nodes.Add(Constants.SentenceBreak, new SimpleStringMarkovNode(Constants.SentenceBreak));
            Nodes.Add(Constants.TextEnd, new SimpleStringMarkovNode(""));
            Nodes.Add(Constants.ParagraphBreak, new SimpleStringMarkovNode("\n\n"));
        }

        public IDictionary<string, IMarkovNode<string>> Nodes { get; private set; }

        public void Train(string text)
        {
            IEnumerable<string> tokens = _tokenizer.Tokenize(text);
            Train(tokens);
        }

        public void Train(IEnumerable<string> sequence)
        {
            IEnumerator<string> enumerator = sequence.GetEnumerator();
            enumerator.MoveNext();
            string token = enumerator.Current;

            IMarkovNode<string> currentNode = GetNode(token);

            while (enumerator.MoveNext())
            {
                // Get the next node.
                token = enumerator.Current;
                IMarkovNode<string> node = GetNode(token);

                // Point the current node to the next node
                currentNode.AddNext(token);

                // Update the current node
                currentNode = node;
            }
        }

        private IMarkovNode<string> GetNode(string word)
        {
            if (!Nodes.ContainsKey(word))
                Nodes.Add(word, new SimpleStringMarkovNode(word));

            return Nodes[word];
        }

        public string Dump()
        {
            StringBuilder sb = new StringBuilder();
            foreach (SimpleStringMarkovNode node in Nodes.Values)
            {
                node.Dump(sb);
                sb.AppendLine(node.Value);
            }
            return sb.ToString();
        }

        private const int MaxGenerateAttempts = 10;

        public IEnumerable<string> GenerateSequence()
        {
            IMarkovNode<string> start = GetNode(Constants.SentenceStart);
            IMarkovNode<string> end = GetNode(Constants.SentenceBreak);
            return GetSequenceInternal(start, end);
        }

        private IEnumerable<string> GetSequenceInternal(IMarkovNode<string> start, IMarkovNode<string> end)
        {
            
            for (int i = 0; i < MaxGenerateAttempts; i++)
            {
                List<string> s = GenerateSequenceInternalFrom(start, end).ToList();
                if (s.Count < 3)
                    continue;
                return s;
            }
            return Enumerable.Empty<string>();
        }

        public string GenerateSentence()
        {
            IMarkovNode<string> start = GetNode(Constants.SentenceStart);
            IMarkovNode<string> end = GetNode(Constants.SentenceBreak);
            IEnumerable<string> sequence = GetSequenceInternal(start, end);
            return string.Join(" ", sequence);
        }

        private IEnumerable<string> GenerateSequenceInternalFrom(IMarkovNode<string> start, IMarkovNode<string> end)
        {
            IMarkovNode<string> current = start;
            while (true)
            {
                // TODO: If the character is a quote, comma or semi-colon, the previous word should
                // be used instead
                string nextKey = current.GetRandomNext();
                if (nextKey == end.Value)
                    break;

                string word = nextKey.Split('|').First();

                yield return word;
                current = GetNode(word);
            }
        }
    }
}