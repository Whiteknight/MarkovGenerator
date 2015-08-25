using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MarkovTest
{
    public class MarkovNode
    {
        private class NextWordList
        {
            public NextWordList()
            {
                Words = new Dictionary<string, int>();
                Prefixes = new Dictionary<string, NextWordList>();
                TotalNext = 0;
            }

            public Dictionary<string, int> Words { get; private set; }
            public Dictionary<string, NextWordList> Prefixes { get; private set; }
            public int TotalNext { get; set; }

            public void AddWord(string word)
            {
                if (!Words.ContainsKey(word))
                    Words.Add(word, 0);

                Words[word]++;
                TotalNext++;
            }

            public string GetRandom()
            {
                if (TotalNext == 0)
                    return null;

                int idx = RandomHelper.Get(TotalNext) + 1;
                foreach (KeyValuePair<string, int> kvp in Words)
                {
                    idx = idx - kvp.Value;
                    if (idx <= 0)
                        return kvp.Key;
                }
                return null;
            }
        }

        public MarkovNode(string word)
        {
            Word = word;
            _next = new NextWordList();
        }

        public string Word { get; private set;  }
        private readonly NextWordList _next;

        private List<NextWordList> CreateNextWordListChain(IList<string> prefix)
        {
            if (prefix == null)
                return new List<NextWordList> { _next };

            /* For a prefix "I love cats and", and word  "dogs"
             * We want a chain starting from _next for the following:
             * "I love cats and" -> "dogs"
             * "love cats and" -> "dogs"
             * "cats and" -> "dogs"
             * "and" -> "dogs"
             * "" -> "dogs"
             * */
            List<NextWordList> chain = new List<NextWordList>(prefix.Count);

            for (int i = 0; i < prefix.Count; i++)
            {
                NextWordList next = _next;
                for (int j = i; j < prefix.Count; j++)
                {
                    string p = prefix[j];
                    if (!next.Prefixes.ContainsKey(p))
                        next.Prefixes.Add(p, new NextWordList());
                    next = next.Prefixes[p];
                    chain.Add(next);
                }
            }
            chain.Add(_next);
            return chain;
        }

        private List<NextWordList> GetNextWordListChain(IList<string> prefix)
        {
            if (prefix == null)
            {
                return new List<NextWordList> {
                    _next
                };
            }
            List<NextWordList> chain = new List<NextWordList>(prefix.Count);

            /* With a prefix "I love cats and"
             * We want a chain starting from _next for the following:
             * "I love cats and"
             * "love cats and"
             * "cats and"
             * "and"
             * ""
             * */
            for (int i = 0; i < prefix.Count; i++)
            {
                NextWordList next = _next;
                for (int j = i; j < prefix.Count; j++)
                {
                    string p = prefix[j];
                    if (!next.Prefixes.ContainsKey(p))
                        break;
                    next = next.Prefixes[p];
                    chain.Add(next);
                }
            }
            chain.Add(_next);
            return chain;
        }
        
        public void AddNext(IList<string> prefix, string word)
        {
            List<NextWordList> chain = CreateNextWordListChain(prefix);
            foreach (NextWordList nwl in chain)
                nwl.AddWord(word);
        }

        public string GetRandomNext(IList<string> prefix)
        {
            List<NextWordList> chain = GetNextWordListChain(prefix);
            foreach (NextWordList link in chain)
            {
                string s = link.GetRandom();
                if (!string.IsNullOrEmpty(s))
                    return s;
            }
            throw new Exception("No children for " + Word);
        }
    }

    public class MarkovChain
    {
        private readonly int _depth;
        // TODO: Instead of keeping special symbols like this, use child chains. When we see a
        // paragraph-start, enter the paragraph child chain and generate until paragraph end. Likewise
        // for sentence-start and sentence-stop, quote-start and quote-stop, etc.

        public Tokenizer Tokenizer;

        public MarkovChain(int depth)
        {
            _depth = depth;
            Tokenizer = new Tokenizer();
            Nodes = new Dictionary<string, MarkovNode>();
            Nodes.Add(Constants.SentenceStart, new MarkovNode(Constants.SentenceStart));
            Nodes.Add(Constants.SentenceBreak, new MarkovNode(Constants.SentenceBreak));
            Nodes.Add(Constants.TextEnd, new MarkovNode(""));
            Nodes.Add(Constants.ParagraphBreak, new MarkovNode("\n\n"));
            Nodes[Constants.ParagraphBreak].AddNext(Constants.SentenceStart);
            Nodes[Constants.SentenceBreak].AddNext(Constants.SentenceStart);
        }

        public Dictionary<string, MarkovNode> Nodes { get; private set; }

        public void Train(string text)
        {
            RingBuffer buffer = new RingBuffer(_depth);
            IEnumerable<string> tokens = Tokenizer.Tokenize(text);
            IEnumerator<string> enumerator = tokens.GetEnumerator();
            enumerator.MoveNext();
            string token = enumerator.Current;

            MarkovNode currentNode = GetNode(token);

            while(enumerator.MoveNext())
            {
                // Get the next node.
                token = enumerator.Current;
                MarkovNode node = GetNode(token);

                // Point the current node to the next node
                buffer.Add(token);
                string key = buffer.GetKey();
                //string key = token;
                currentNode.AddNext(key);

                // Update the current node
                currentNode = node;
            }
        }

        private MarkovNode GetNode(string word)
        {
            if (!Nodes.ContainsKey(word))
                Nodes.Add(word, new MarkovNode(word));
            
            return Nodes[word];
        }

        public string Dump()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MarkovNode node in Nodes.Values)
            {
                sb.AppendLine(node.Word);
                foreach (KeyValuePair<string, int> kvp in node.Next)
                    sb.AppendFormat("\t{1} -> {0}\n", kvp.Key, kvp.Value);
            }
            return sb.ToString();
        }

        public string GenerateSentence()
        {
            MarkovNode node = GetNode(Constants.SentenceStart);
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

        private IEnumerable<string> GenerateSentenceInternalFrom(MarkovNode start)
        {
            //RingBuffer buffer = new RingBuffer(_depth);
            MarkovNode current = start;
            //buffer.Add(start.Word);
            while (true)
            {
                // TODO: If the character is a quote, comma or semi-colon, the previous word should
                // be used instead
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

    class Program
    {
        static void Main(string[] args)
        {
            string text = File.ReadAllText("SampleText.txt");
            //string text = "\"a?\n\n\"I hope you're feeling well.\"\n\n\"I'm Fine,\" she replies.";
            //Console.WriteLine(text);
            //foreach (string token in new Tokenizer().Tokenize(text))
            //    Console.WriteLine(token);
            MarkovChain chain = new MarkovChain(2);
            chain.Train(text);
            string dump = chain.Dump();
            File.WriteAllText("outfile.txt", dump);
            //while (true)
            //{
            //    char c = Console.ReadKey().KeyChar;
            //    if (c == 'Q' || c == 'q')
            //        break;

            //    string s = chain.GenerateSentence();
            //    Console.WriteLine(s);
            //    Console.WriteLine();
            //}

            //Console.ReadKey();
        }
    }
}
