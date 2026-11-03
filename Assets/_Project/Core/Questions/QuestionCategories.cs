using System.Collections.Generic;

namespace PovuciPotegni.Core
{
    /// <summary>Category keys used in the question JSON and in bot profiles.</summary>
    public static class QuestionCategories
    {
        public const string History = "istorija";
        public const string Geography = "geografija";
        public const string Sport = "sport";
        public const string FilmTv = "film_tv";
        public const string Music = "muzika";
        public const string Science = "nauka";
        public const string ArtLiterature = "umetnost_knjizevnost";
        public const string General = "opste";

        public static readonly IReadOnlyCollection<string> All = new HashSet<string>
        {
            History,
            Geography,
            Sport,
            FilmTv,
            Music,
            Science,
            ArtLiterature,
            General,
        };
    }
}
