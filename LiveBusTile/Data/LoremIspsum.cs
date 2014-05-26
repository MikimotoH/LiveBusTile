using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Resources;

namespace LiveBusTile.Data
{
    /// <summary>
    /// A class to return paragraphs of random sentences or a word list from lorem ipsum data.
    /// </summary>
    public class LoremIpsum : IEnumerable<string>
    {
        public enum Capitalization
        {
            None,
            FirstWord,
            AllWords
        }

        private static Random _rnd = new Random(42);

        private static StringBuilder _builder = new StringBuilder();
        private static List<string> _sentences;
        private static List<string> _words;

        /// <summary>
        /// Returns random lorem ipsum sentences combined into a single string.
        /// </summary>
        /// <param name="sentenceCount">The nunmber of sentences.</param>
        /// <returns>The paragraph, composed of random sentences.</returns>
        public static string GetParagraph(int sentenceCount)
        {
            EnsureSentences();

            _builder.Length = 0;

            while (sentenceCount-- > 0)
            {
                _builder.Append(_sentences[_rnd.Next(sentenceCount)]);
                if (sentenceCount > 0)
                {
                    _builder.Append(' ');
                }
            }

            return _builder.ToString();
        }

        /// <summary>
        /// Return an alphabetized, lower-case list of lorem ipsum words.
        /// </summary>
        public static ICollection<string> Words
        {
            get
            {
                EnsureWords();
                return (ICollection<string>)_words;
            }
        }

        /// <summary>
        /// Get a string composed of random lorem ipsum words. Will not end with punctuation.
        /// </summary>
        /// <param name="wordCount">Number of words.</param>
        /// <param name="capitalize">How to capitalize the words.</param>
        /// <returns></returns>
        public static string GetWords(int wordCount, Capitalization capitalization)
        {
            EnsureWords();

            _builder.Length = 0;

            while (wordCount-- > 0)
            {
                int position = _builder.Length;
                _builder.Append(_words[_rnd.Next( wordCount)]);
                if (capitalization == Capitalization.AllWords || (position == 0 && capitalization == Capitalization.FirstWord))
                {
                    _builder[position] = char.ToUpper(_builder[position]);
                }

                if (wordCount > 0)
                {
                    _builder.Append(' ');
                }
            }

            return _builder.ToString();
        }

        /// <summary>
        /// Enumerates the Words property.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<string> GetEnumerator()
        {
            return LoremIpsum.Words.GetEnumerator();
        }

        /// <summary>
        /// Enumerates the Words property.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return LoremIpsum.Words.GetEnumerator();
        }

        /// <summary>
        /// Reads the lorem ipsum sentences. Supplies some data in case reading fails, which
        /// it will do at design time.
        /// </summary>
        private static void EnsureSentences()
        {
            if (_sentences == null)
            {
                _sentences = new List<string>();

                StreamResourceInfo info = Application.GetResourceStream(new Uri("Data/LoremIpsum.txt", UriKind.Relative));
                if (info != null)
                {
                    Stream stream = info.Stream;
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                _sentences.Add(reader.ReadLine());
                            }
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Creates an alphabetized list of the words from the lorem ipsum text.
        /// </summary>
        private static void EnsureWords()
        {
            char[] separators = { ' ', ',', '.' };

            EnsureSentences();

            if (_words == null)
            {
                HashSet<string> set = new HashSet<string>();

                foreach (string sentence in _sentences)
                {
                    string[] words = sentence.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    set.UnionWith(words);
                }

                _words = set.ToList();
                _words.Sort();
            }
        }
    }

}
