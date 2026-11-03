# Plan rada (MVP, ~6 nedelja razvoja)

Radi fazu po fazu. Posle svake faze: kratak izveštaj šta je urađeno, šta korisnik treba da proveri u editoru, i da li testovi prolaze. Pitanja (sadržaj) se pišu i proveravaju paralelno, van koda.

## Faza 0 — Projekat (dan 1)

- Unity 6 LTS, URP (Mobile), portret, Android + iOS build target, Input System, UI Toolkit.
- Folderi i `.asmdef` kao u `docs/ARHITEKTURA.md`; `Core` bez Unity referenci.
- `.gitignore` za Unity, `GameConfig` ScriptableObject sa vrednostima iz GDD-a.
- **Gotovo kad:** prazan projekat se otvara bez grešaka, prazan EditMode test prolazi iz komandne linije.

## Faza 1 — Motor igre (nedelje 1–2)

- `MatchRules`, `MatchState`, džokeri, kraj partije, produžeci, „teže pitanje posle dva tačna“.
- `QuestionBank` (učitavanje JSON-a), `QuestionPicker`, `Transliterator` (latinica → ćirilica, uključujući lj, nj, dž).
- `BotBrain` i četiri `BotProfile` iz GDD tabele.
- `BotMatchService : IMatchService`.
- **Testovi** (najmanje): svaki red tabele pomeranja; Štit poništava povlačenje; Duplo vuče 2 i ne prelazi ±3; pobeda na ivici; pobeda posle 15 pitanja; produžeci; 50/50 nikad ne uklanja tačan odgovor; bot sa seedom daje očekivanu tačnost (±2% na 10.000 simulacija, po težini i kategoriji); transliteracija.
- Simulacija iz komandne linije ili testa: 10.000 partija „prosečan igrač“ (65% tačnost) protiv svakog bota → ispiši procenat pobeda i prosečan broj pitanja. Uporedi sa ciljevima balansa iz GDD-a.
- **Gotovo kad:** svi testovi prolaze, simulacija ispisuje razumne brojke.

## Faza 2 — 3D scena (nedelje 3–4, prvi deo)

- Editor meni `Povuci-Potegni/Izgradi arenu`: teren, reka, obale, staza, oznake, drveće (fiksni seed), brda, oblaci, svetlo, magla, kamera — po `docs/SCENA.md`.
- `CharacterRig` od primitiva sa zglobovima i licem; pet likova kao prefabi.
- `RopeView` (proceduralna cev + tekstura + traka), `CameraRig`.
- Probna scena `Sandbox` sa dugmićima: povuci za mene / za njega / izjednačeno / pad / pobeda.
- **Gotovo kad:** scena izgleda kao `reference/slike/game0.png` i `game1.png`; bez treperenja ivica; 60 fps na srednjem Android telefonu.

## Faza 3 — Animacije (nedelje 3–4, drugi deo)

- `PoseController` (upiranje, disanje, povlačenje, posrtanje, izjednačeno, klaćenje, pad, plivanje, slavlje), raspoloženja lica.
- Krokodil sa vilicom i ponašanjem, `FxPool` (prašina, pljusak, krugovi na vodi, konfete), drmanje kamere i FOV.
- **Gotovo kad:** svako dugme u `Sandbox` sceni daje istu animaciju kao prototip (uporedi jedno pored drugog), tajming po `docs/SCENA.md`.

## Faza 4 — Ekrani i tok igre (nedelje 3–4 → 5)

- UI Toolkit ekrani po dizajnu: početni, izbor protivnika (portreti iz renderovanih slika ili kamere), partija (HUD, kartica pitanja, džokeri, traka rezultata), kraj partije, statistika, podešavanja (pismo, zvuk, haptika).
- `GameFlow` i `MatchController` povezuju `IMatchService` sa scenom i UI-jem.
- Otključavanje botova po pobedama, lokalna statistika (`PlayerPrefsStatsStore` ili JSON u `persistentDataPath`).
- **Gotovo kad:** cela partija protiv svakog bota može da se odigra od početnog ekrana do kraja i nazad.

## Faza 5 — Završni MVP (nedelje 5–6)

- Zvuk i haptika (`SfxPlayer`, `Haptics`), „Prijavi grešku“, analitika iza `IAnalytics` (pa Firebase Analytics + Crashlytics kad korisnik doda konfiguraciju), ćirilica u celom UI-ju.
- Uvoz 500 pitanja (Editor alat koji proverava format, duplikate, raspodelu po kategorijama i težinama i ispiše izveštaj).
- Pre izdanja: isključiti `AllowUnverified` u `Settings/GameConfig.asset` i skloniti scenu `Sandbox` iz builda. (U razvoju je uključeno, jer u bazi još nema proverenih pitanja.)
- Android build (AAB) i iOS build za TestFlight, ikonica i splash.
- **Gotovo kad:** build radi na pravom telefonu, partija bez pada, analitika šalje događaje.

## Posle MVP-a

Test sa 20–50 ljudi (dve nedelje), pa odluka o fazi 2 (online duel preko Firebasea, `FirebaseMatchService`), po meri uspeha iz GDD-a.
