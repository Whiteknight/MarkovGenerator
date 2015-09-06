using System.IO;

namespace MarkovText.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleTextMarkovChain chain = new SimpleTextMarkovChain();
            foreach (string file in args)
            {
                string text = File.ReadAllText(file);
                //text = text.Split('\n')[0];
                //string text = "\"a?\n\n\"I hope you're feeling well.\"\n\n\"I'm Fine,\" she replies.";
                //Console.WriteLine(text);
                //foreach (string token in new Tokenizer().Tokenize(text))
                //    Console.WriteLine(token);
                
                chain.Train(text);
            }

            //string dump = chain.Dump();
            //File.WriteAllText("outfile.txt", dump);

            while (true)
            {
                char c = System.Console.ReadKey().KeyChar;
                if (c == 'Q' || c == 'q')
                    break;

                string s = chain.GenerateSentence();
                System.Console.WriteLine(s);
                System.Console.WriteLine();
            }

            System.Console.ReadKey();

        }
    }
}
