# Povuci-Potegni — pravila i sadržaj (GDD)

Izvod iz dizajn dokumenta igre. Sve brojčane vrednosti su početne i menjaju se posle testiranja, zato ih drži u jednom `GameConfig` (ScriptableObject za Unity stranu, običan objekat za Core).

## Koncept

Kviz duel jedan na jedan na srpskom. Oba igrača odgovaraju na isto pitanje sa tri ponuđena odgovora. Ko odgovori tačno kad protivnik pogreši, povuče uže korak ka sebi. Ko protivnika prevuče preko ivice (u reku), pobeđuje. Igra se protiv bota (MVP), kasnije protiv drugih igrača.

Ciljna publika: ljubitelji TV kvizova na ex-YU tržištu i dijaspora. Ime "Potera", tragači i TV format se **ne koriste**; likovi i izgled moraju biti originalni.

## Pravila partije

- Partija traje 3–5 minuta, najviše **15 pitanja**. Ko prvi prevuče protivnika, pobeđuje odmah.
- **Uže:** 7 polja, čvor (marker) kreće sa sredine. Pozicija `k` ide od −3 do +3 (0 = sredina, + = ka igraču). Svaki igrač ima 3 polja do svoje ivice. `k = +3` → igrač pobedio, `k = −3` → protivnik pobedio.

**Tok jednog pitanja:**
1. Oba igrača vide isto pitanje sa tri ponuđena odgovora (redosled odgovora meša aplikacija).
2. Tajmer je **15 sekundi**, samo da partija ne stoji. **Brzina odgovora ne utiče na ishod.**
3. Posle oba odgovora (ili isteka vremena) prikazuje se tačan odgovor i šta je ko odgovorio. Igrač koji odgovori prvi čeka protivnika.
4. Uže se pomera po pravilima ispod.

**Pomeranje:**

| Ishod | Pomeranje |
| --- | --- |
| Samo jedan igrač tačan | 1 polje ka tačnom igraču |
| Oba tačna | Uže stoji, sledeće pitanje je teže |
| Oba netačna ili bez odgovora | Uže stoji |

**Težina pitanja:**
- Osnovna težina zavisi od rednog broja pitanja: 1–5 lako, 6–10 srednje, 11–15 i produžeci teško.
- Svako pitanje na koje su oba igrača tačna podiže sledeće pitanje za jedan nivo u odnosu na osnovnu težinu (lako → srednje → teško), najviše do teškog.
- Čim neko pogreši ili ne odgovori, težina se vraća na osnovnu.

**Kraj partije:**
- Uže pređe ivicu → pobeda onog ko je vukao.
- Posle 15 pitanja pobeđuje onaj na čijoj strani uže NIJE (k > 0 → igrač, k < 0 → protivnik).
- Uže na sredini posle 15 pitanja → **produžeci**: pitanja se nastavljaju dok na isto pitanje jedan igrač ne odgovori tačno, a drugi netačno. Ako Štit poništi to povlačenje, produžeci se nastavljaju.

**Džokeri** (po jedan od svakog u partiji, ne prodaju se za novac):
- **50/50:** uklanja jedan netačan odgovor na trenutnom pitanju.
- **Štit:** poništava jedno povlačenje protiv tebe (aktivira se pre odgovora, važi za to pitanje). Troši se i ako povlačenja nije bilo.
- **Duplo:** sledeći tačan odgovor (kad protivnik pogreši) vuče 2 polja. Aktivira se pre odgovora, ne može da se isključi i ostaje aktivan dok zaista ne povuče; ako protivnikov Štit poništi povlačenje, Duplo ostaje aktivan. Ne prelazi ivicu.
- Svi džokeri se koriste pre odgovora, a na istom pitanju može više njih.
- Botovi u MVP-u ne koriste džokere; pravila su ipak ista za obe strane.
- Dodatni džoker kasnije može da se dobije gledanjem nagradne reklame, najviše jednom po partiji (faza 2).

**Kasnije, ne u MVP-u:** rizičan start, kategorije koje bira gubitnik prethodnog pitanja, turniri.

## Botovi

Bot je protivnik sa imenom i karakterom; težina je verovatnoća tačnog odgovora. Vreme odgovora služi samo da bot deluje prirodno.

| Lik | Težina | Tačnost | Vreme odgovora | Otključavanje | Jak / slab |
| --- | --- | --- | --- | --- | --- |
| Šegrt | Lak | 55% | 4–8 s | Od starta | film / nauka |
| Profesor | Srednji | 70% | 3–6 s | Posle 3 pobede | istorija / sport |
| Lisac | Težak | 82% | 2–5 s | Posle 10 pobeda | sport / umetnost |
| Majstor | Ekspert | 90% | 1,5–4 s | Posle 25 pobeda | nema (za sada) |

**Model ponašanja:**
- Tačnost zavisi od težine pitanja: lako +10 poena, teško −15 poena u odnosu na tabelu.
- Jača kategorija +10, slabija −10 poena. Rezultat je ograničen na 0–100%.
- Kad bot greši, bira nasumično jedan od netačnih odgovora.
- Vreme odgovora nasumično unutar raspona; igrač vidi samo „Profesor je odgovorio“, a **odgovor bota vidi tek kad i sam odgovori**.
- Balans (cilj): prosečan igrač pobedi Šegrta ~75%, Profesora ~50%, Lisca ~30% puta.
- Isti bot kasnije služi kao zamena u multiplayeru kad nema protivnika; igrač tada vidi da igra protiv bota.

Izgled likova: vidi `docs/SCENA.md` i `reference/slike/p-*.png`.

## Pitanja

MVP: **500 proverenih pitanja** u aplikaciji (offline), bez ponavljanja dok se baza ne potroši. Netačno pitanje ruši poverenje brže od bilo kog baga.

| Kategorija | Ključ u JSON-u | Udeo |
| --- | --- | --- |
| Istorija | `istorija` | 15% |
| Geografija | `geografija` | 15% |
| Sport | `sport` | 15% |
| Film i TV | `film_tv` | 12% |
| Muzika | `muzika` | 12% |
| Nauka i priroda | `nauka` | 11% |
| Umetnost i književnost | `umetnost_knjizevnost` | 10% |
| Opšte i zanimljivosti | `opste` | 10% |

Najmanje trećina baze su lokalna pitanja (domaća istorija, sport, muzika, TV). Težine 1 (lako), 2 (srednje), 3 (teško); partija kreće lakšim pitanjima i postaje teža.

**Format** (`Assets/StreamingAssets/questions/sr-Latn.json`, niz objekata; primer u `reference/pitanja-primer.json`):

```json
{
  "id": "q_000123",
  "lang": "sr-Latn",
  "category": "sport",
  "difficulty": 2,
  "text": "Koji grad je bio domaćin Univerzijade 2009?",
  "options": ["Beograd", "Novi Sad", "Zagreb"],
  "answer": 0,
  "source": "https://...",
  "verifiedBy": "ime urednika",
  "verifiedAt": "2026-11-15",
  "stats": { "shown": 0, "correct": 0 }
}
```

- `answer` je indeks u `options` pre mešanja.
- U igru ulaze samo pitanja sa popunjenim `verifiedBy`. (U razvoju dozvoli i neproverena, iza podešavanja `AllowUnverified`.)
- Svako pitanje ima tačno tri različita odgovora. Neispravan zapis obara celo učitavanje, da pogrešno pitanje ne stigne do igrača.
- Kad nema neviđenih pitanja tražene težine, bira se najbliža težina; kad se baza potroši, istorija kreće ispočetka.
- Igrač može da prijavi pitanje dugmetom „Prijavi grešku“; prijavljeno pitanje se sklanja lokalno i šalje (kasnije Firebase).
- **Pismo:** latinica i ćirilica iz istog zapisa (automatska transliteracija), izbor u podešavanjima. Rimski brojevi, hemijski simboli i reči sa q, w, x ili y ostaju latinicom.

## Obim MVP-a

Cilj: za oko osam nedelja (šest razvoja, dve testiranja) proveriti da li je igra zabavna i da li se ljudi vraćaju.

Ulazi u MVP:
- [ ] Glavni meni: Igraj, izbor protivnika (bot), statistika, podešavanja
- [ ] Ekran partije: 3D scena sa užetom preko reke, pitanje preko scene, tri odgovora, tajmer, prikaz odgovora oba igrača
- [ ] Sva pravila iznad, uključujući produžetke
- [ ] Tri džokera (50/50, Štit, Duplo)
- [ ] Četiri bota sa otključavanjem po broju pobeda
- [ ] 500 proverenih pitanja u aplikaciji (offline), bez ponavljanja dok se baza ne potroši
- [ ] Lokalna statistika: partije, pobede, niz pobeda, procenat po kategoriji
- [ ] Zvuk i haptika (tajmer, tačno, netačno, povlačenje, pad, pobeda)
- [ ] Dugme „Prijavi grešku“
- [ ] Osnovna analitika: započete i završene partije, povratak posle 1 i 7 dana
- [ ] Latinica i ćirilica

Čeka na kasnije: online duel (faza 2), izazov prijatelju (2), rang liste (2), reklame i kupovine (2), pitanja sa servera (2), kozmetika (3), sponzorisani turniri i B2B (3), engleska verzija (3), posebne arene po protivniku (lava, led, močvara).

**Mera uspeha MVP-a** (test sa 20–50 ljudi): većina završi prvu partiju, prosečno 3+ partije po sesiji, bar trećina se vrati sledećeg dana.
