using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MarkovTest
{
    public class MarkovNode
    {
        public MarkovNode(string word)
        {
            Word = word;
            Next = new Dictionary<string, int>();
            _totalNext = 0;
        }

        public string Word { get; private set;  }
        public Dictionary<string, int> Next { get; private set; }
        private int _totalNext;


        public void AddNext(string word)
        {
            if (!Next.ContainsKey(word))
                Next.Add(word, 0);
            Next[word]++;
            _totalNext++;
        }

        public string GetRandomNext()
        {
            int idx = RandomHelper.Get(_totalNext) + 1;
            foreach (KeyValuePair<string, int> kvp in Next)
            {
                idx = idx - kvp.Value;
                if (idx <= 0)
                    return kvp.Key;
            }
            throw new Exception("Whoops");
        }
    }

    public class MarkovChain
    {
        // TODO: Instead of keeping special symbols like this, use child chains. When we see a
        // paragraph-start, enter the paragraph child chain and generate until paragraph end. Likewise
        // for sentence-start and sentence-stop, quote-start and quote-stop, etc.

        public Tokenizer Tokenizer;

        public MarkovChain()
        {
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
            IEnumerable<string> tokens = Tokenizer.Tokenize(text);
            MarkovNode currentNode = GetNode(Constants.SentenceStart);

            foreach (string t in tokens.Skip(1))
            {
                if (t == Constants.SentenceStart)
                {
                    currentNode = GetNode(Constants.SentenceStart);
                    continue;
                }

                if (t == Constants.TextEnd)
                {
                    currentNode.AddNext(Constants.TextEnd);
                    currentNode = GetNode(Constants.SentenceStart);
                    continue;
                    // This should be the end
                }

                if (t == Constants.ParagraphBreak)
                {
                    currentNode.AddNext(Constants.ParagraphBreak);
                    continue;
                }

                MarkovNode node = GetNode(t);
                currentNode.AddNext(node.Word);
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
            MarkovNode current = start;
            while (true)
            {
                // TODO: If the character is a quote, comma or semi-colon, the previous word should
                // be used instead
                string nextWord = current.GetRandomNext();
                if (nextWord == Constants.SentenceBreak || nextWord == Constants.ParagraphBreak || nextWord == Constants.TextEnd)
                    break;

                yield return nextWord;
                current = GetNode(nextWord);
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
            MarkovChain chain = new MarkovChain();
            chain.Train(text);
            string s = chain.Dump();
            File.WriteAllText("outfile.txt", s);
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
