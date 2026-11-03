# Povuci-Potegni — start paket za Claude Code (Unity)

## Šta je unutra

| Fajl | Čemu služi |
| --- | --- |
| `CLAUDE.md` | Uputstvo koje Claude Code čita na početku svake sesije: tehnologija, pravila za kod, komande |
| `docs/GDD.md` | Pravila igre, botovi, pitanja, obim MVP-a (izvor istine za pravila) |
| `docs/SCENA.md` | 3D scena, likovi, kamera i animacije sa vrednostima iz prototipa |
| `docs/ARHITEKTURA.md` | Struktura koda, `IMatchService`, Firebase za fazu 2 |
| `docs/ZADACI.md` | Plan rada po fazama, sa kriterijumima „gotovo kad“ |
| `reference/prototip-3d.html` | Igrivi 3D prototip (otvori u pregledaču) |
| `reference/slike/` | Kadrovi iz scene i portreti botova |
| `reference/pitanja-primer.json` | 12 primera pitanja u tačnom formatu (neprovereni, samo za razvoj) |

Online: [3D prototip](https://claude.ai/artifact/JcXbHAiJmv4ihnfY654ZhE) · [dizajn ekrana](https://claude.ai/artifact/XShqckUvZHJrbmvZp73dYt) · [dizajn dokument igre](https://claude.ai/artifact/WSmYfBeMpxM1Q1FTT5qakj)

## Kako da kreneš

1. U Unity Hubu napravi nov projekat: **Unity 6 LTS**, šablon **Universal 3D (URP)**, ime `PovuciPotegni`.
2. Zatvori Unity. U koren projekta (pored `Assets/`) iskopiraj sadržaj ovog paketa: `CLAUDE.md`, `README.md`, `docs/`, `reference/`.
3. Napravi git repozitorijum i prvi commit (Unity `.gitignore` će Claude Code dodati u fazi 0, ili ga dodaj odmah).
4. U terminalu, u korenu projekta, pokreni `claude` i pošalji poruku ispod.
5. Unity drži zatvoren dok Claude Code radi prvu fazu i pokreće testove iz komandne linije; posle otvori projekat i proveri šta je napravio.

## Prva poruka za Claude Code

```
Pročitaj CLAUDE.md i sve u docs/, pa pogledaj reference/prototip-3d.html i slike u reference/slike/.
Radimo fazu 0 i fazu 1 iz docs/ZADACI.md: postavi Unity projekat (URP, portret, Input System, UI Toolkit, asmdef struktura)
i napiši motor igre u čistom C# sa EditMode testovima i simulacijom balansa.
Pre nego što kreneš, napiši kratak plan fajlova i reci mi ako ti nešto u pravilima nije jasno.
Posle svake faze stani, napiši šta je urađeno, šta treba da proverim u editoru i rezultat testova.
```

Sledeće faze pokreći porukom tipa: `Radimo fazu 2 iz docs/ZADACI.md.`

## Napomene

- Pravila u `docs/GDD.md` imaju prednost nad prototipom. Prototip ima tajmer od 20 s i džoker Duplo koji se uključuje pre odgovora; GDD kaže 15 s i „sledeći tačan odgovor vuče 2“.
- Pitanja u `reference/pitanja-primer.json` nisu urednički proverena (`verifiedBy` je prazan). Pravih 500 pitanja ide kroz urednika.
- Likovi su za MVP od primitiva; Synty/Mixamo modeli se ubacuju kasnije bez menjanja koda igre.
