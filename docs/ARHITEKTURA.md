# Arhitektura

Motor igre se piše jednom i ne zna da li je protivnik bot ili čovek. Igra priča samo sa `IMatchService`; za MVP iza njega je bot, u fazi 2 Firebase, a kasnije možda Nakama — bez menjanja igre.

## Struktura projekta

```
Assets/_Project/
  Core/                     PovuciPotegni.Core.asmdef  (noEngineReferences: true)
    Rules/                  GameConfig, MatchState, RopePosition, Outcome, Jokers, MatchRules
    Bots/                   BotProfile, BotBrain
    Questions/              Question, QuestionBank, QuestionPicker, Transliterator
    Match/                  IMatchService, BotMatchService, MatchEvents
    Abstractions/           IRandom, IClock, IStatsStore, IAnalytics, IReportSink
  Core.Tests/               PovuciPotegni.Core.Tests.asmdef (EditMode, NUnit)
  Game.Tests/               PovuciPotegni.Game.Tests.asmdef (EditMode: podešavanja, scena, poze)
  Game.PlayTests/           PovuciPotegni.Game.PlayTests.asmdef (PlayMode nad pravim scenama)
  Game/                     PovuciPotegni.Game.asmdef  (Unity strana)
    Flow/                   GameFlow (state machine ekrana), MatchController
    Scene/                  ArenaBuilder, ArenaDecor, RiverShape, RopeView, CharacterRig, PoseController, ArenaDirector, ArenaMotion, CrocodileView, FxPool, CameraRig
    UI/                     UXML + USS + kontroleri ekrana (Home, Opponents, Match, Result, Settings, Stats)
    Services/               PlayerPrefsStatsStore, NullAnalytics, LocalReportSink, (kasnije) Firebase*
    Audio/                  SfxPlayer, Haptics
  Editor/                   meni "Povuci-Potegni/..." (izgradi arenu, napravi probne likove, uvezi pitanja)
  Art/  Fonts/  Audio/  Settings/ (URP, GameConfig.asset)
Assets/StreamingAssets/questions/sr-Latn.json
```

## Ključni interfejsi (C#, Core)

```csharp
public interface IMatchService
{
    event Action<QuestionShown> QuestionShown;       // pitanje + rok, BEZ tačnog odgovora
    event Action<OpponentAnswered> OpponentAnswered; // samo da je protivnik odgovorio, ne šta
    event Action<JokerUsed> JokerUsed;               // koji džoker; za 50/50 i uklonjeni indeks
    event Action<RoundResolved> RoundResolved;       // tačan odgovor, oba izbora, pomeraj, nova pozicija k
    event Action<MatchEnded> MatchEnded;             // pobednik, razlog (ivica, 15 pitanja, produžeci), statistika

    void Start(MatchSetup setup);                    // protivnik (bot profil), seed, kategorije
    void SubmitAnswer(int optionIndex);              // -1 = isteklo vreme
    bool UseJoker(JokerType joker);                  // 50/50 vraća uklonjeni indeks kroz događaj JokerUsed
    void Continue();                                 // sledeće pitanje, kad se animacija završi; tek tada kreće tajmer
    void Tick(double nowSeconds);                    // pokreće tajmere (bot, rok) u MVP-u
}
```

- `MatchRules` je čista funkcija: `(stanje, odgovor igrača, odgovor protivnika, aktivni džokeri) → RoundResult`. Sva pravila iz GDD-a, uključujući produžetke i „teže pitanje posle dva tačna“, žive tu i testiraju se bez Unityja.
- `BotBrain` računa verovatnoću tačnog odgovora (osnova + težina pitanja + jača/slabija kategorija) i vreme odgovora; koristi `IRandom`.
- `QuestionPicker` bira pitanje po kategoriji i težini, bez ponavljanja dok se baza ne potroši (istorija čuva `IStatsStore`).
- Unity strana sluša događaje i animira: `RoundResolved` → kartica se spušta → `RopeView`/`PoseController` puštaju povlačenje → traka sa porukom.
- `ArenaDirector` (Game) drži vreme animacija i usklađuje uže, likove, kameru, krokodila i efekte: `PlayPull(steps, onDone)`, `PlayFall(loser, onDone)`, `PlayCelebration(winner)`.
- `ICharacterView` (Game) sakriva da li je lik od primitiva ili Mixamo model. Vreme vodi `ArenaDirector`, pa lik prima napredak: `SetMood`, `ShowIdle()`, `ShowPull(role, progress)`, `ShowFall(progress)`, `ShowSwimming()`, `ShowCelebration()`, `UpdateView(baseZ, time)`, `HandsPoint`.

## Faza 2: online duel (Firebase)

Firebase Unity SDK (Android, iOS). Realtime Database za partije i prisutnost, Cloud Functions za logiku, Auth za naloge, region `europe-west1`. Igra je na poteze, pa ne treba pravi server za igru.

1. **Uparivanje:** igrač upiše sebe u red čekanja; Cloud Function upari dvojicu i napravi `matches/{id}`. Ako niko ne dođe za ~10 s, dobija bota.
2. **Pitanje:** server upiše pitanje i rok po vremenu servera, bez tačnog odgovora.
3. **Odgovor:** klijent upiše samo svoj izbor; Security Rules dozvoljavaju igraču da piše samo svoj odgovor, i to jednom.
4. **Potez:** Cloud Function sačeka oba odgovora ili istek roka, proveri ih (isti `MatchRules` kao u klijentu, prepisan u TypeScript ili deljen kroz testove), upiše rezultat i tačan odgovor.
5. **Prekid veze:** `onDisconnect` označi igrača kao odsutnog; server dodeljuje poraz ili ga menja botom.

`FirebaseMatchService : IMatchService` je jedina nova klasa na klijentu. Funkcije partije dobijaju `minInstances` da prvi poziv ne kasni.

**Kapacitet (gruba procena):** jedna RTDB baza ~1.000 upisa/s → ~1.500 partija, ~3.000 igrača istovremeno; više baza u istom projektu skalira linearno (partija se smešta po ID-u). Na milion partija mesečno: ~100 GB prometa (~$100) i ~20 miliona poziva funkcija.

## Analitika (MVP)

`IAnalytics` događaji: `match_start` (bot, kategorija), `match_end` (ishod, broj pitanja, trajanje), `question_answered` (kategorija, težina, tačno), `joker_used`, `question_reported`, `bot_unlocked`. Povratak posle 1 i 7 dana Firebase računa sam. Do uvođenja Firebasea: `NullAnalytics` koji piše u Console.
