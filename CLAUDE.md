# Povuci-Potegni — uputstvo za Claude Code

Kviz duel jedan na jedan na srpskom. Oba igrača odgovaraju na isto pitanje; ko je tačan kad protivnik pogreši, povuče uže korak ka sebi. Ko protivnika prevuče u reku, pobeđuje. MVP se igra protiv bota, bez servera.

Pre bilo kakvog rada pročitaj, ovim redom:
1. `docs/GDD.md` — pravila igre, botovi, pitanja, obim MVP-a. **Izvor istine za pravila.**
2. `docs/SCENA.md` — 3D scena, likovi, kamera i animacije (sa tačnim vrednostima iz prototipa).
3. `docs/ARHITEKTURA.md` — struktura koda, interfejsi, multiplayer za kasnije.
4. `docs/ZADACI.md` — redosled rada po fazama, sa kriterijumima završetka.

Vizuelna referenca: `reference/prototip-3d.html` (otvori u pregledaču, radi bez interneta osim three.js sa CDN-a) i slike u `reference/slike/`. Prototip je referenca za izgled, kameru i tajming animacija. Ako se prototip i GDD razlikuju u pravilima, važi GDD.

## Tehnologija

- **Unity 6 LTS** (najnovija 6000.x LTS iz Unity Huba), **URP**, ciljne platforme Android i iOS, portret orijentacija.
- **UI Toolkit** (UXML + USS) za sve ekrane. Dizajn ekrana je HTML/CSS, pa se lako prenosi u USS.
- **TextMeshPro / UI Toolkit fontovi:** Unbounded (naslovi) i Manrope (tekst), oba sa Google Fonts, licenca OFL. Font asset mora da uključi Latin Extended (š, đ, č, ć, ž) i ćirilicu.
- **Input System** paket (novi), dodir i miš.
- Bez plaćenih paketa sa Asset Store-a. Tweenovi i animacije u sopstvenom kodu (korutine ili mali `Tween` helper), ne DOTween.
- Firebase (Analytics, Crashlytics) dolazi kasnije u MVP-u, iza interfejsa; do tada stub implementacije, da projekat radi bez `google-services.json`.

## Pravila za kod

- Sav kod u `Assets/_Project/` (vidi `docs/ARHITEKTURA.md`). Namespace `PovuciPotegni.*`.
- `PovuciPotegni.Core` (motor igre, bot, pitanja) je **čist C# bez `UnityEngine` zavisnosti** u svom `.asmdef` (`noEngineReferences: true`). Sva pravila žive tu i pokrivena su EditMode testovima.
- Scena i UI samo prikazuju stanje i šalju odluke igrača. Ne računaju pravila.
- Nasumičnost kroz `IRandom` interfejs (seed u testovima), vreme kroz `IClock`. Nikad `UnityEngine.Random` ili `Time` u Core.
- Imena u kodu na engleskom; tekst koji igrač vidi na srpskom, iz `Localization` tabele (`sr-Latn`; ćirilica transliteracijom).
- Male, česte izmene. Posle svake faze: testovi prolaze, projekat se kompajlira bez upozorenja iz našeg koda.

## Komande

Testovi iz komandne linije (prilagodi putanju do Unity editora):

```bash
# macOS
"/Applications/Unity/Hub/Editor/<verzija>/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./TestResults/editmode.xml -logFile -
# Windows (PowerShell)
& "C:\Program Files\Unity\Hub\Editor\<verzija>\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults .\TestResults\editmode.xml -logFile -
```

Unity editor ne sme biti otvoren nad istim projektom dok se testovi pokreću iz komandne linije. Ako je otvoren, zamoli korisnika da pokrene testove iz Test Runner prozora i javi rezultat.

## Kako radiš sa mnom

- Pre svake faze iz `docs/ZADACI.md` napiši kratak plan (koje fajlove praviš), pa radi.
- Kad nešto mora da se uradi ručno u editoru (napraviti scenu, podesiti URP asset, uvesti font), napiši tačne korake. Gde može, napravi Editor skriptu (meni `Povuci-Potegni/...`) koja to uradi jednim klikom.
- Ne izmišljaj pravila. Ako GDD nešto ne pokriva, pitaj.
- Pitanja u igri moraju biti tačna. Ne dodaj nova pitanja u bazu bez napomene da ih urednik mora proveriti (`verifiedBy` ostaje prazan).
