using System.Collections.Generic;

namespace PovuciPotegni.Core
{
    /// <summary>All text the player sees, written in Serbian Latin. Cyrillic comes from transliteration.</summary>
    public sealed class Localization
    {
        private static readonly Dictionary<string, string> Table = new Dictionary<string, string>
        {
            { "app.name", "Povuci-Potegni" },

            { "home.tag", "KVIZ DUEL" },
            { "home.headline", "Odgovori tačno kad on pogreši. Povuci ga u reku." },
            { "home.play", "Igraj" },
            { "home.opponent", "Protivnik: {0} · {1}" },
            { "home.opponents", "Protivnici" },
            { "home.unlocked", "{0} od {1} otključana" },
            { "home.stats", "Statistika" },
            { "home.strongest", "Najjača: {0}" },
            { "home.noData", "Još nema podataka" },
            { "stat.wins", "Pobede" },
            { "stat.streak", "Niz pobeda" },
            { "stat.correct", "Tačnih" },

            { "opponents.title", "Izaberi protivnika" },
            { "opponents.intro", "Svaki ima jaču i slabiju oblast. Pobede otključavaju jače protivnike." },
            { "opponents.play", "Igraj protiv {0}" },
            { "opponents.locked", "{0} · otključava se posle {1} {2}" },
            { "opponents.progress", "{0}/{1}" },

            { "bot.segrt.name", "Šegrt" },
            { "bot.segrt.genitive", "Šegrta" },
            { "bot.segrt.level", "Lak" },
            { "bot.segrt.traits", "jak u filmu, slab u nauci" },
            { "bot.profesor.name", "Profesor" },
            { "bot.profesor.genitive", "Profesora" },
            { "bot.profesor.level", "Srednji" },
            { "bot.profesor.traits", "jak u istoriji, slab u sportu" },
            { "bot.lisac.name", "Lisac" },
            { "bot.lisac.genitive", "Lisca" },
            { "bot.lisac.level", "Težak" },
            { "bot.lisac.traits", "jak u sportu, slab u umetnosti" },
            { "bot.majstor.name", "Majstor" },
            { "bot.majstor.genitive", "Majstora" },
            { "bot.majstor.level", "Ekspert" },
            { "bot.majstor.traits", "jak u svemu" },

            { "category.istorija", "Istorija" },
            { "category.geografija", "Geografija" },
            { "category.sport", "Sport" },
            { "category.film_tv", "Film i TV" },
            { "category.muzika", "Muzika" },
            { "category.nauka", "Nauka i priroda" },
            { "category.umetnost_knjizevnost", "Umetnost i književnost" },
            { "category.opste", "Opšte i zanimljivosti" },

            { "match.you", "Ti" },
            { "match.youShort", "TI" },
            { "match.vs", "VS" },
            { "match.toWin", "{0} do pobede" },
            { "match.questionOf", "{0} / {1}" },
            { "match.overtime", "Produžeci" },
            { "match.incoming", "Pitanje {0} stiže…" },
            { "match.seconds", "{0} s" },
            { "match.thinking", "{0} razmišlja…" },
            { "match.answered", "{0} je odgovorio" },
            { "match.correct", "tačno" },
            { "match.wrong", "netačno" },
            { "match.timeout", "isteklo vreme" },
            { "match.summaryYou", "Ti: {0}" },
            { "match.summaryBot", "{0}: {1}" },
            { "match.chip", "{0}: {1} · {2}" },
            { "joker.half", "50/50" },
            { "joker.half.hint", "ukloni jedan" },
            { "joker.shield", "Štit" },
            { "joker.shield.hint", "blokira potez" },
            { "joker.double", "Duplo" },
            { "joker.double.hint", "dva koraka" },
            { "joker.armed", "aktivan" },
            { "joker.used", "iskorišćen" },

            { "banner.pull.title", "Povukao si!" },
            { "banner.pull.text", "Ti si tačan, {0} nije. Ide korak ka vodi." },
            { "banner.double.title", "Dupli potez!" },
            { "banner.double.text", "Ti si tačan, {0} nije. Ide dva koraka ka vodi." },
            { "banner.pulled.title", "Povukao te je!" },
            { "banner.pulled.text", "{0} je tačan, ti nisi. Ideš korak ka vodi." },
            { "banner.shield.title", "Štit te je spasao" },
            { "banner.shield.text", "{0} je bio tačan, ali uže miruje." },
            { "banner.tie.title", "Izjednačeno" },
            { "banner.both.text", "Oboje tačno. Uže miruje, sledeće pitanje je teže." },
            { "banner.none.text", "Oboje netačno. Uže miruje." },

            { "result.after", "POSLE {0} PITANJA" },
            { "result.win", "Pobeda!" },
            { "result.lose", "Poraz" },
            { "result.winEdge", "{0} je završio u reci." },
            { "result.loseEdge", "{0} te je povukao u reku." },
            { "result.winPoints", "Uže je na tvojoj strani posle poslednjeg pitanja." },
            { "result.losePoints", "Uže je na strani protivnika posle poslednjeg pitanja." },
            { "result.winOvertime", "Dobio si produžetke." },
            { "result.loseOvertime", "Izgubio si produžetke." },
            { "result.yourCorrect", "Tvoji tačni" },
            { "result.toNext", "Do {0}" },
            { "result.toNextProgress", "{0} / {1} pobeda" },
            { "result.allUnlocked", "Svi protivnici su otključani" },
            { "result.rematch", "Revanš" },
            { "result.other", "Drugi protivnik" },
            { "result.home", "Početna" },

            { "stats.title", "Statistika" },
            { "stats.matches", "Partije" },
            { "stats.best", "Najbolji niz" },
            { "stats.byCategory", "Po kategorijama" },
            { "stats.noAnswers", "Odigraj prvu partiju i ovde će se pojaviti rezultati." },
            { "stats.categoryLine", "{0}% · {1} odg." },

            { "settings.title", "Podešavanja" },
            { "settings.script", "Pismo" },
            { "settings.latin", "Latinica" },
            { "settings.cyrillic", "Ćirilica" },
            { "settings.sound", "Zvuk" },
            { "settings.haptics", "Vibracija" },
            { "settings.on", "Uključeno" },
            { "settings.off", "Isključeno" },

            { "report.button", "Prijavi grešku" },
            { "report.done", "Prijavljeno, hvala" },

            { "error.questions", "Pitanja nisu mogla da se učitaju. Pokušaj ponovo." },
            { "error.retry", "Pokušaj ponovo" },
        };

        public TextScript Script { get; set; }

        public Localization(TextScript script)
        {
            Script = script;
        }

        public static bool HasKey(string key)
        {
            return Table.ContainsKey(key);
        }

        /// <summary>The text for a key; a missing key shows itself, so the mistake is visible.</summary>
        public string Get(string key)
        {
            return Content(Table.TryGetValue(key, out string text) ? text : key);
        }

        public string Format(string key, params object[] arguments)
        {
            return string.Format(Get(key), arguments);
        }

        /// <summary>Text from outside the table, such as questions, in the chosen script.</summary>
        public string Content(string latin)
        {
            return Script == TextScript.Cyrillic ? Transliterator.ToCyrillic(latin) : latin;
        }

        /// <summary>The word for "win" after a number: 1 pobeda, 2 pobede, 5 pobeda.</summary>
        public string WinsWord(int count)
        {
            int lastTwo = count % 100;
            int last = count % 10;
            return Content(last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14) ? "pobede" : "pobeda");
        }
    }
}
