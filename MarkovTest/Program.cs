using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MarkovTest
{
    public static class RandomHelper
    {
        private static readonly Random s_random = new Random();

        public static int Get(int max)
        {
            return s_random.Next(max);
        }
    }

    public class MarkovNode
    {
        public MarkovNode(string word)
        {
            Word = word;
            Next = new Dictionary<string, int>();
            m_totalNext = 0;
        }

        public string Word { get; private set;  }
        public Dictionary<string, int> Next { get; private set; }
        private int m_totalNext;

        public void AddNext(string word)
        {
            if (!Next.ContainsKey(word))
                Next.Add(word, 0);
            Next[word]++;
            m_totalNext++;
        }

        public string GetRandomNext()
        {
            int idx = RandomHelper.Get(m_totalNext) + 1;
            foreach (KeyValuePair<string, int> kvp in Next)
            {
                idx = idx - kvp.Value;
                if (idx <= 0)
                    return kvp.Key;
            }
            throw new Exception("Whoops");
        }
    }

    public class Tokenizer
    {
        public IEnumerable<string> Tokenize(string text)
        {
            int i = 0;
            text = text + new string('\0', 20);

            bool wasEnd = true;
            bool wasBreak = true;
            for (; i < text.Length; i++)
            {
                if (wasEnd)
                {
                    wasEnd = false;
                    yield return MarkovChain.SentenceStart;
                }
                char c = text[i];
                if (wasBreak && c == '\n')
                    continue;
                wasBreak = false;
                
                if (c == '\0')
                {
                    break;
                }
                if (c == '\n')
                {
                    wasBreak = true;
                    yield return MarkovChain.ParagraphBreak;
                    continue;
                }
                if (c == '.')
                {
                    yield return MarkovChain.SentenceBreak;
                    wasEnd = true;
                    i++;
                    continue;
                }
                if (char.IsLetter(c))
                {
                    int startIdx = i;
                    while (char.IsLetter(c) || c == '\'')
                    {
                        i++;
                        c = text[i];
                    }
                    i--;
                    int endIdx = i;
                    yield return text.Substring(startIdx, endIdx - startIdx + 1);
                    continue;
                }
                if (c == ',' || c == ';')
                {
                    yield return c.ToString();
                }
            }
            yield return MarkovChain.TextEnd;
        }
    }

    public class MarkovChain
    {
        public const string ParagraphBreak = "<<<NEWLINE>>>";
        public const string SentenceStart = "<<<START>>>";
        public const string SentenceBreak = "<<<PERIOD>>>";
        public const string TextEnd = "<<<END>>>";

        public Tokenizer Tokenizer;

        public MarkovChain()
        {
            Tokenizer = new Tokenizer();
            Nodes = new Dictionary<string, MarkovNode>();
            Nodes.Add(SentenceStart, new MarkovNode(SentenceStart));
            Nodes.Add(SentenceBreak, new MarkovNode(SentenceBreak));
            Nodes.Add(TextEnd, new MarkovNode(""));
            Nodes.Add(ParagraphBreak, new MarkovNode("\n\n"));
            Nodes[ParagraphBreak].AddNext(SentenceStart);
            Nodes[SentenceBreak].AddNext(SentenceStart);
        }

        public Dictionary<string, MarkovNode> Nodes { get; private set; }

        public void Train(string text)
        {
            IEnumerable<string> tokens = Tokenizer.Tokenize(text);
            MarkovNode currentNode = GetNode(SentenceStart);

            foreach (string t in tokens.Skip(1))
            {
                if (t == SentenceStart)
                {
                    currentNode = GetNode(SentenceStart);
                    continue;
                }

                if (t == TextEnd)
                {
                    currentNode.AddNext(TextEnd);
                    currentNode = GetNode(SentenceStart);
                    continue;
                    // This should be the end
                }

                if (t == ParagraphBreak)
                {
                    currentNode.AddNext(ParagraphBreak);
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

        public void Dump()
        {
            foreach (MarkovNode node in Nodes.Values)
            {
                Console.WriteLine(node.Word);
                foreach (KeyValuePair<string, int> kvp in node.Next)
                    Console.WriteLine("\t{1} -> {0}", kvp.Key, kvp.Value);
            }
        }

        public string GenerateSentence()
        {
            MarkovNode node = GetNode(SentenceStart);
            while (true)
            {
                List<string> s = GenerateSentenceInternalFrom(node).ToList();
                if (s.Count < 3)
                    continue;

                //s[0] = Capitalize(s[0]);

                return string.Join(" ", s)  + ".";
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
                string nextWord = current.GetRandomNext();
                if (nextWord == SentenceBreak || nextWord == ParagraphBreak || nextWord == TextEnd)
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
            //foreach (string token in new Tokenizer().Tokenize(text))
            //    Console.WriteLine(token);
            MarkovChain chain = new MarkovChain();
            chain.Train(text);
            for (int i = 0; i < 20; i++)
            {
                string s = chain.GenerateSentence();
                Console.WriteLine(s);
            }

            Console.ReadKey();
        }
    }
}
