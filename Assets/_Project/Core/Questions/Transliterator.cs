using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PovuciPotegni.Core
{
    /// <summary>Converts Serbian Latin text to Cyrillic, leaving symbols and foreign words in Latin.</summary>
    public static class Transliterator
    {
        private static readonly Dictionary<char, char> Letters = new Dictionary<char, char>
        {
            { 'a', 'а' }, { 'b', 'б' }, { 'c', 'ц' }, { 'č', 'ч' }, { 'ć', 'ћ' }, { 'd', 'д' }, { 'đ', 'ђ' },
            { 'e', 'е' }, { 'f', 'ф' }, { 'g', 'г' }, { 'h', 'х' }, { 'i', 'и' }, { 'j', 'ј' }, { 'k', 'к' },
            { 'l', 'л' }, { 'm', 'м' }, { 'n', 'н' }, { 'o', 'о' }, { 'p', 'п' }, { 'r', 'р' }, { 's', 'с' },
            { 'š', 'ш' }, { 't', 'т' }, { 'u', 'у' }, { 'v', 'в' }, { 'z', 'з' }, { 'ž', 'ж' },
        };

        private static readonly Dictionary<string, char> Digraphs = new Dictionary<string, char>
        {
            { "lj", 'љ' }, { "nj", 'њ' }, { "dž", 'џ' },
        };

        /// <summary>Word parts where two letters only look like a digraph; '|' marks the boundary.</summary>
        private static readonly string[] DigraphExceptions =
        {
            "in|jek", "kon|jug", "kon|junk", "van|jez", "nad|živ", "pod|živ", "od|živ", "nad|žnj", "pod|žnj", "od|žal",
        };

        /// <summary>Case-sensitive symbols that stay in Latin. Symbols that spell a word, like "Na", are left out.</summary>
        private static readonly HashSet<string> LatinWords = new HashSet<string>
        {
            "Au", "Ag", "Zn", "Fe", "Cu", "Pb", "Hg", "Sn", "Mg", "H2O", "CO2",
        };

        private static readonly Regex Words = new Regex(@"[\p{L}\p{N}]+");

        private static readonly Regex RomanNumeral = new Regex(
            "^(?=[IVXLCDM]{2,}$)M{0,3}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$");

        public static string ToCyrillic(string latin)
        {
            return string.IsNullOrEmpty(latin) ? latin : Words.Replace(latin, match => ConvertWord(match.Value));
        }

        private static string ConvertWord(string word)
        {
            if (LatinWords.Contains(word) || RomanNumeral.IsMatch(word) || word.Any(IsForeignLetter))
            {
                return word;
            }

            HashSet<int> boundaries = FindDigraphExceptions(word);
            var result = new StringBuilder(word.Length);

            for (int i = 0; i < word.Length; i++)
            {
                if (i + 1 < word.Length && !boundaries.Contains(i + 1))
                {
                    string pair = word.Substring(i, 2).ToLowerInvariant();
                    if (Digraphs.TryGetValue(pair, out char digraph))
                    {
                        result.Append(MatchCase(digraph, word[i]));
                        i++;
                        continue;
                    }
                }

                char lower = char.ToLowerInvariant(word[i]);
                result.Append(Letters.TryGetValue(lower, out char letter) ? MatchCase(letter, word[i]) : word[i]);
            }

            return result.ToString();
        }

        private static HashSet<int> FindDigraphExceptions(string word)
        {
            var boundaries = new HashSet<int>();
            string lowerWord = word.ToLowerInvariant();

            foreach (string exception in DigraphExceptions)
            {
                int boundary = exception.IndexOf('|');
                string stem = exception.Remove(boundary, 1);

                int index = lowerWord.IndexOf(stem, System.StringComparison.Ordinal);
                while (index >= 0)
                {
                    boundaries.Add(index + boundary);
                    index = lowerWord.IndexOf(stem, index + 1, System.StringComparison.Ordinal);
                }
            }

            return boundaries;
        }

        private static bool IsForeignLetter(char character)
        {
            char lower = char.ToLowerInvariant(character);
            return lower == 'q' || lower == 'w' || lower == 'x' || lower == 'y';
        }

        private static char MatchCase(char cyrillic, char source)
        {
            return char.IsUpper(source) ? char.ToUpperInvariant(cyrillic) : cyrillic;
        }
    }
}
