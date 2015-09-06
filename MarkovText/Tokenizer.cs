using System.Collections.Generic;

namespace MarkovText
{
    public class Tokenizer
    {
        private static bool IsSentenceStartChar(char c)
        {
            return char.IsLetter(c) || char.IsNumber(c) || c == '"';
        }

        private static bool IsSentenceEndChar(char c)
        {
            return c == '.' || c == '?' || c == '!';
        }

        private static readonly HashSet<string> _abbrevsWithPeriod = new HashSet<string>(new[] {
            "mrs",
            "mr",
            "dr",
            "etc",
            "esq"
        });

        public IEnumerable<string> Tokenize(string text)
        {
            int i = 0;
            text = text.Trim() + new string('\0', 20);

            bool isQuoteStarted = false;
            yield return Constants.ParagraphStart;
            yield return Constants.SentenceStart;
            for (; i < text.Length; i++)
            {
                char c = text[i];
                
                if (c == '\0')
                    // TODO: Need to fill in any missing end tokens here?
                    break;

                if (c == ' ' || c == '\t')
                    continue;

                // Paragraph break.
                if (c == '\n')
                {
                    yield return Constants.ParagraphBreak;

                    while (text[i + 1] == '\n')
                        i++;
                    char d = text[i + 1];
                    if (IsSentenceStartChar(d))
                    {
                        // If anything comes after a paragraph break, it's a new 
                        // paragraph and a new sentence.
                        yield return Constants.ParagraphStart;
                        yield return Constants.SentenceStart;
                    }
                    continue;
                }

                // Blocks to handle special cases relating to re-ordering quote symbols.
                if (IsSentenceEndChar(c) && text[i + 1] == '\n' && isQuoteStarted)
                {
                    // We really don't have any good way of handling a dangling end-quote.
                    // Insert a new EndQuote before the end of the sentence/paragraph 
                    // and continue tokenizing like normal.
                    yield return Constants.QuoteEnd;
                    isQuoteStarted = false;
                }
                else if ((IsSentenceEndChar(c) || c == ',') && text[i + 1] == '"')
                {
                    // re-arrange period-quote to be quote-period. The end of the quote happens
                    // logically before the end of the sentence.

                    if (isQuoteStarted)
                    {
                        yield return Constants.QuoteEnd;
                        isQuoteStarted = false;
                        i++;

                        // At this point, c = '.' but text[i] = '"', so we 
                        // will be handled by the next control block and will
                        // skip the quote entirely.
                    }
                }
                
                if (IsSentenceEndChar(c))
                {
                    // TODO: We should lookback and correct for 
                    yield return c.ToString();
                    yield return Constants.SentenceBreak;

                    // Skip spaces until we see the next non-whitespace character.
                    // Don't consume the sentence-start character.
                    // If we run into a newline, break so the ParagraphBreak machinery
                    // can handle things.
                    while (text[i + 1] == ' ')
                        i++;

                    if (IsSentenceStartChar(text[i + 1]))
                        yield return Constants.SentenceStart;
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
                    string s = text.Substring(startIdx, endIdx - startIdx + 1);
                    if (text[i + 1] == '.' && _abbrevsWithPeriod.Contains(s.ToLowerInvariant()))
                    {
                        // By the rules of grammar, an abbreviation with a period at the end of a
                        // sentence does not require an additional period. The abbreviation and it's
                        // period is understood to be the end of the sentence. At the same time,
                        // it's considered bad form to do this. This tokenizer ignores that case,
                        // because there's no obvious and general way to detect this programmatically
                        // (without some other trained, learning algorithm, which defeats the
                        // purpose of this.)
                        i++;
                        yield return s + ".";
                        continue;
                    }
                    yield return s;
                    if (text[i + 1] == '\n')
                    {
                        yield return ".";
                        yield return Constants.SentenceBreak;
                    }
                    continue;
                }
                if (c == ',' || c == ';' || c == ':')
                {
                    yield return c.ToString();
                    i++;
                    while (char.IsWhiteSpace(text[i]))
                        i++;
                    i--;
                    continue;
                }
                if (c == '"')
                {
                    if (isQuoteStarted)
                    {
                        isQuoteStarted = false;
                        yield return Constants.QuoteEnd;
                        continue;
                    }
                    isQuoteStarted = true;
                    yield return Constants.QuoteStart;
                    continue;
                }
            }
            yield return Constants.ParagraphBreak;
            yield return Constants.TextEnd;
        }
    }
}