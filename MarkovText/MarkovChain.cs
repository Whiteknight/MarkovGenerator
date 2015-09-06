using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkovText
{
    public class MarkovChain
    {
        //private readonly int _depth;
        // TODO: Instead of keeping special symbols like this, use child chains. When we see a
        // paragraph-start, enter the paragraph child chain and generate until paragraph end. Likewise
        // for sentence-start and sentence-stop, quote-start and quote-stop, etc.

        public Tokenizer Tokenizer;

        public MarkovChain()
        {
            //_depth = depth;
            Tokenizer = new Tokenizer();
            Nodes = new Dictionary<string, SimpleStringMarkovNode>();
            Nodes.Add(Constants.SentenceStart, new SimpleStringMarkovNode(Constants.SentenceStart));
            Nodes.Add(Constants.SentenceBreak, new SimpleStringMarkovNode(Constants.SentenceBreak));
            Nodes.Add(Constants.TextEnd, new SimpleStringMarkovNode(""));
            Nodes.Add(Constants.ParagraphBreak, new SimpleStringMarkovNode("\n\n"));
        }

        public Dictionary<string, SimpleStringMarkovNode> Nodes { get; private set; }

        public void Train(string text)
        {
            //RingBuffer buffer = new RingBuffer(_depth);
            IEnumerable<string> tokens = Tokenizer.Tokenize(text);
            IEnumerator<string> enumerator = tokens.GetEnumerator();
            enumerator.MoveNext();
            string token = enumerator.Current;

            SimpleStringMarkovNode currentNode = GetNode(token);

            while (enumerator.MoveNext())
            {
                // Get the next node.
                token = enumerator.Current;
                SimpleStringMarkovNode node = GetNode(token);

                // Point the current node to the next node

                //string key = buffer.GetKey();
                string key = token;
                //currentNode.AddNext(buffer.GetAll().ToList(), key);
                currentNode.AddNext(token);
                //buffer.Add(token);

                // Update the current node
                currentNode = node;
            }
        }

        private SimpleStringMarkovNode GetNode(string word)
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

        public string GenerateSentence()
        {
            SimpleStringMarkovNode node = GetNode(Constants.SentenceStart);
            while (true)
            {
                List<string> s = GenerateSentenceInternalFrom(node).ToList();
                if (s.Count < 3)
                    continue;

                //s[0] = Capitalize(s[0]);

                return string.Join(" ", s);
            }
        }

        private string Capitalize(string s)
        {
            return s.Substring(0, 1).ToUpperInvariant() + s.Substring(1);
        }

        private IEnumerable<string> GenerateSentenceInternalFrom(SimpleStringMarkovNode start)
        {
            //RingBuffer buffer = new RingBuffer(_depth);
            SimpleStringMarkovNode current = start;
            //buffer.Add(start.Value);
            while (true)
            {
                // TODO: If the character is a quote, comma or semi-colon, the previous word should
                // be used instead
                //string nextKey = current.GetRandomNext(buffer.GetAll().ToList());
                string nextKey = current.GetRandomNext();
                if (nextKey == Constants.SentenceBreak || nextKey == Constants.ParagraphBreak || nextKey == Constants.TextEnd)
                    break;

                string word = nextKey.Split('|').First();

                yield return word;
                //buffer.Add(word);
                //string key = buffer.GetKey();
                string key = word;
                current = GetNode(key);
            }
        }
    }
}
