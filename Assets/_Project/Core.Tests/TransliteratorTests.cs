using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    public class TransliteratorTests
    {
        [TestCase("abvgdđežzijklmnoprstćufhcčš", "абвгдђежзијклмнопрстћуфхцчш")]
        [TestCase("ABVGDĐEŽZIJKLMNOPRSTĆUFHCČŠ", "АБВГДЂЕЖЗИЈКЛМНОПРСТЋУФХЦЧШ")]
        public void ConvertsEveryLetter(string latin, string cyrillic)
        {
            Assert.That(Transliterator.ToCyrillic(latin), Is.EqualTo(cyrillic));
        }

        [TestCase("ljubav", "љубав")]
        [TestCase("Ljubav", "Љубав")]
        [TestCase("LJUBAV", "ЉУБАВ")]
        [TestCase("konj", "коњ")]
        [TestCase("Njegoš", "Његош")]
        [TestCase("NJEGOŠ", "ЊЕГОШ")]
        [TestCase("džak", "џак")]
        [TestCase("Džak", "Џак")]
        [TestCase("DŽAK", "ЏАК")]
        public void ConvertsDigraphs(string latin, string cyrillic)
        {
            Assert.That(Transliterator.ToCyrillic(latin), Is.EqualTo(cyrillic));
        }

        [TestCase("injekcija", "инјекција")]
        [TestCase("Injekcija", "Инјекција")]
        [TestCase("konjugacija", "конјугација")]
        [TestCase("konjunkcija", "конјункција")]
        [TestCase("nadživeti", "надживети")]
        [TestCase("odžaliti", "оджалити")]
        public void KeepsLookalikeLettersSeparate(string latin, string cyrillic)
        {
            Assert.That(Transliterator.ToCyrillic(latin), Is.EqualTo(cyrillic));
        }

        [TestCase("Au")]
        [TestCase("Ag")]
        [TestCase("Zn")]
        [TestCase("H2O")]
        [TestCase("XIV")]
        [TestCase("IV")]
        [TestCase("New")]
        [TestCase("Wimbledon")]
        public void LeavesSymbolsAndForeignWordsInLatin(string word)
        {
            Assert.That(Transliterator.ToCyrillic(word), Is.EqualTo(word));
        }

        [TestCase("Na", "На")]
        [TestCase("TV", "ТВ")]
        [TestCase("U", "У")]
        [TestCase("MIR", "МИР")]
        public void ConvertsWordsThatResembleSymbols(string latin, string cyrillic)
        {
            Assert.That(Transliterator.ToCyrillic(latin), Is.EqualTo(cyrillic));
        }

        [Test]
        public void KeepsPunctuationAndNumbers()
        {
            string result = Transliterator.ToCyrillic("Ko je napisao roman „Na Drini ćuprija“?");
            Assert.That(result, Is.EqualTo("Ко је написао роман „На Дрини ћуприја“?"));

            Assert.That(Transliterator.ToCyrillic("Godine 1804."), Is.EqualTo("Године 1804."));
        }

        [Test]
        public void KeepsLatinSymbolInsideSentence()
        {
            Assert.That(Transliterator.ToCyrillic("Simbol Au je zlato."), Is.EqualTo("Симбол Au је злато."));
        }

        [TestCase("")]
        [TestCase(null)]
        public void EmptyInput_IsReturnedAsIs(string input)
        {
            Assert.That(Transliterator.ToCyrillic(input), Is.EqualTo(input));
        }
    }
}
