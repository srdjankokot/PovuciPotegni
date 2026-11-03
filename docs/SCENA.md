# 3D scena, likovi i animacije

Referenca: `reference/prototip-3d.html` (three.js). Vrednosti ispod su izvučene iz prototipa; jedinice su metri, Unity koristi isti sistem (Y gore). Prototip koristi desnoruki sistem sa kamerom koja gleda ka −Z; u Unityju okreni Z osu (z_unity = −z_prototip) ili rotiraj ceo `ArenaRoot` za 180°, kako ti je lakše — samo budi dosledan.

Na slikama u `reference/slike/`: `home.png` (početni ekran), `game0.png` (početak partije), `game1.png` (posle jednog povlačenja), `win.png` (kraj), `p-*.png` (portreti botova).

**Razlike u odnosu na prototip:** likovi su zdepasti i detaljniji, reka krivuda, a arena ima dodatne ukrase. Na slikama je stari izgled; za raspored i kameru one i dalje važe. Referenca za stil likova je `reference/karakteri/` (samo proporcije i utisak; likovi moraju da ostanu originalni, bez preuzimanja likova iz tog paketa). U sporu između slika i ovog dokumenta važi ovaj dokument.

## Stil

- Šaren low-poly, crtani izgled. Likovi sa toon senčenjem u 3 nivoa sa mekim prelazima (120/190/255). U URP-u: `Art/Shaders/Stylized.shader`.
- Teren, drveće i kamenje: flat shading.
- Nebo jednobojno `#A8DCFF`, magla iste boje, start 45 m, kraj 110 m.
- Svetlo: Hemisphere (nebo `#D6ECFF`, tlo `#4F8A3F`, 0,55), Ambient 0,08, Directional `#FFF3E0` jačine 0,7 iz smera (7, 14, 9) sa mekim senkama.

## Arena (koordinate prototipa)

| Element | Mera / pozicija | Boja |
| --- | --- | --- |
| Reka (voda) | traka dužine 80 m, y = −0,32, oblik po `RiverShape`; na |x| ≤ 1,75 prava i široka 3,2 m preko z = 0, dalje krivuda i menja širinu | `#4CB2EC`, dublji deo `#3FA6E4`, plićak `#8FD6F7` |
| Obale (zemlja) | prate reku: vrh širine 0,34 m na y ≈ +0,008 (malo iznad trave, da nema treperenja) i strm zid do vode; kod staze na z = ±1,5 | `#A9784A` |
| Bliže ostrvo | površina y = 0, od obale do 40 m ka kameri | `#8FD07C` |
| Dalje ostrvo | površina y = 0, od obale do 60 m dalje | `#8FD07C` |
| Staza | 3,2 m široka, svetlija `#B8E3A4`, duž z ose na oba ostrva | |
| Oznake koraka | bele linije 2,6 × 0,09 m na z = ±2,6, ±3,8, ±5,0 | `#FFFFFF` |
| Talasići | 9 tankih svetlih traka koje klize po vodi duž x | `#D8F0FF` |
| Drveće | četinari (kupe) i kamenje, nasumično ali sa fiksnim seedom, ne bliže od 4 m od staze; nema drveća između kamere i igrača | zelene nijanse |
| Brda | velike low-poly sfere na z = −72 | `#9FCFA6`, `#8CC49A` |
| Oblaci | grupe belih sfera na y 13–18, polako klize | bela |

**Ukrasi** (`ArenaDecor`, poseban fiksni seed, da raspored drveća i kamenja iz prototipa ostane isti; ono što bi palo u reku se preskače): mrlje druge nijanse trave, busenje, cveće, žbunje, listopadno drveće, panjevi, pečurke, šljunak uz stazu, drvena ograda duž dalje staze (z 6,5–24), tabla bez teksta, kamenje, trska i lokvanji u reci. Visoki ukrasi na strani igrača ostaju bar 5,5 m od staze, da ne zaklone pogled kamere.

**Pozicije igrača:** igrač na z = 5 + s, protivnik na z = −5 + s, gde je `s = k × 1,2` (k je pozicija užeta). Uže, traka i oba igrača se pomeraju zajedno. Kod k = ±3 gubitnik stoji na ivici reke.

**Kamera** prati igrača iza desnog ramena: pozicija (0,75; 3,1; max(zIgrača; 3,2) + 6,2), gleda u (0,15; 1,0; max(zIgrača; 3,2) − 7,5). FOV 52° u portretu (42° ako je ekran širi od 0,7 odnosa). Pomera se glatko (lerp sa `1 − e^(−4·dt)`). Near plane 0,3, far 200.

**Kamera u meniju:** pogled odozgo na oba lika, iznad donjeg panela: pozicija (−4,2; 11; −13,5) u Unity koordinatama, gleda u (0,3; −3,6; −1,2), FOV 56°. Prelaz u partiju je glatak (ista brzina kao praćenje).

## Uže

- Cev (tube) poluprečnika 0,055 m kroz tačke: rep iza igrača na tlu → pored tela, uz kuk (0,55 m u stranu, da ne prolazi kroz lik) → ruke igrača → sredina (malo ugnuta, 0,12 m ispod linije ruku) → ruke protivnika → pored tela → rep iza protivnika na tlu. Catmull-Rom (centripetal). U Unityju: `LineRenderer` je prihvatljiv za početak, bolje proceduralni mesh cevi koji se obnavlja svaki frame.
- Tekstura: žute kose pruge (`#F2CF55` i `#D4A62A`), ponavljanje duž užeta.
- Crvena traka (čvor) na sredini užeta: kuglica `#B3262B` i dve trake `#E5484D` koje vise i lepršaju (rotacija z ±0,15 rad, sin(t·5)).
- Talas tokom povlačenja: pomeraj y = 0,09 · sin(π·f) · sin(34·t − 9·f) duž zategnutog dela (f od 0 do 1), slabi ka kraju povlačenja.
- Kad pobednik slavi, pusti uže: njegov kraj pada na tlo 0,7 m ispred njega, a ostatak ide u stranu, da ne prolazi kroz noge.
- Ko padne u reku, pusti uže tokom skoka: kraj pluta po vodi 0,85 m pored njega, a uže prelazi preko ivice obale, da ne prolazi kroz lik.

## Likovi

Za MVP su likovi sklopljeni od primitiva (kao u prototipu), sa ovim zglobovima, da kasnije mogu da se zamene Mixamo humanoidom:

`Root → Hips (y 0,6) → { Thigh L/R → Knee → (shin, cipela) ; Body → { Torso, Neck, Head, Shoulder L/R → Elbow → Hand } }`

Mere (zdepaste proporcije, ukupna visina oko 1,8 m): butina 0,24, potkolenica 0,2, nadlaktica 0,24, podlaktica 0,22, šaka poluprečnik 0,115, glava poluprečnik 0,38 (lice je raspoređeno za 0,3 i skalirano sa 1,27), ramena na ±0,33 i y 0,4 od kukova, glava na y 0,82 od kukova.

**Lice:** male tamne oči sa odsjajem, zaobljene obrve, obrazi; obrve se rotiraju, a troja usta se pale po raspoloženju.

Raspoloženja:
- `Grit` — stisnuti zubi, obrve nagnute ka unutra (rotacija ±0,28): dok vuku.
- `Oh` — okrugla otvorena usta, obrve podignute: kad ih vuku i kad padaju.
- `Smile` — osmeh: pobeda.

| Lik | Izgled |
| --- | --- |
| Igrač | crveni kačket okrenut unazad (traka i dugme), plavi dres `#3550E8` sa belom prugom, oznakom na grudima i brojem 7 na leđima, teget pantalone, bele patike |
| Šegrt | kratka smeđa kosa sa šiškama, zelena dukserica `#3BBF8E` sa kapuljačom, vezicama i džepom |
| Profesor | ćelav sa sedim pramenovima, brkovi, okrugle naočare, narandžasta košulja `#FF8A3D` sa kragnom, dugmadima i džepom sa olovkom, plava leptir-mašna, kaiš sa kopčom |
| Lisac | riđa šiljata kosa `#D9572B`, ljubičasta majica `#6E4BD8` sa žutom prugom, žute narukvice |
| Majstor | kratka seda kosa, brkovi, naočare, crna rolka `#1E2230`, zlatna medalja, kaiš |

## Poze i animacije (proceduralne za MVP)

Uglovi su u radijanima, u lokalnom prostoru lika (lik gleda ka svom +Z).

**Upiranje (osnovna poza):** Hips y = 0,54; Body rotacija x = −0,42 (zabačen unazad) + disanje 0,025·sin(2,2t); prednja noga: butina −0,6, koleno 0,5; zadnja noga: butina 0,25, koleno 0,35; ramena x = −1,15 − lean·0,55, z = ±0,42; laktovi −0,55; glava x = −lean·0,55, lagano okretanje y 0,06·sin(0,7t).

**Mirovanje užeta:** s = k·1,2 + 0,035·sin(1,3t) + 0,015·sin(3,3t). Uže nikad ne stoji potpuno mirno.

**Povlačenje** (trajanje 1150 ms; p od 0 do 1):
- 0–18%: zalet — uže ide 0,12 m u pogrešnu stranu, pobednik se dodatno zabacuje.
- 18–100%: s ide od starog do novog položaja sa easeOutBack (c1 = 1,4), tj. malo preskoči i vrati se.
- Pobednik: lean −0,28 dodatno, vučenje ruku naizmenično (ramena ±0,45·sin(4πp)), korači unazad (noge ±0,45).
- Gubitnik: lean ide ka +0,53 (posrće napred), ruke ispružene, brzi koraci, raspoloženje `Oh`, prašina ispod nogu svakih 90 ms (meke bež kuglice koje rastu i blede 0,9 s).
- Kamera: blago drmanje 0,05·sin(60t) i FOV −4° na sredini povlačenja.

**Izjednačeno** (1000 ms): oboje se napnu, uže se klati ±0,13 m oko istog mesta, ne pomera se.

**Pad u reku** (2000 ms; p od 0 do 1):
- 0–30%: klaćenje na ivici — telo napred 0,35, ruke kao vetrenjača (ramena se okreću brzinom 18 rad/s), jedna noga u vazduhu.
- 30–100%: skok u reku po luku (gore 0,45 m, pa dole do y = −0,8), rotacija napred do 1,1 rad pa nazad.
- Na 62%: veliki pljusak (70 kapljica, bele i svetloplave, gravitacija 9,8) i 3 kruga na vodi koja se šire 1,4 s.
- Posle: lik pliva u vodi (y −0,8 + 0,05·sin(3t); prototip ima −1,05, ali bi to potopilo niže likove), maše rukama iznad glave, raspoloženje `Oh`.

**Pobeda:** lik pusti uže, skače (|sin(5,5t)|·0,5 m), obe ruke gore i mašu, raspoloženje `Smile`. Kad pobedi igrač: 120 konfeta u bojama igre padaju iznad scene.

**Krokodil:** telo, rep od 4 segmenta koji vijuga (±0,18 rad), glava sa donjom vilicom na zglobu, žute oči sa uspravnim zenicama.
- Normalno patrolira rekom (x = 9·sin(0,22t)).
- Kad je |k| ≥ 2, dopliva ispod onoga koji je bliže vodi i otvara i zatvara čeljust.
- Posle pada dopliva do lika u vodi i škljoca brže.
- Kretanje glatko (damp 1,1), okreće se u smeru kretanja.

## Pitanje preko scene

Kartica sa pitanjem izlazi odozdo (520 ms, cubic-bezier 0,2/0,85/0,25/1) preko scene, a scena se zatamni (crna 30%). Posle prikaza rezultata (oko 1,7 s) kartica se spušta, i tek tada kreće povlačenje. Posle povlačenja izlazi traka sa porukom i dugmetom za dalje (pop animacija 380 ms). Raspored i boje ekrana: dizajn na canvasu (link u `README.md`) i `reference/prototip-3d.html`.

## Zamena likova pravim modelima (kasnije)

Mixamo humanoid + Animator: stanja Idle-Pull, Pull-Win, Pull-Stumble, Teeter, Fall, Swim, Celebrate. Ruke se vezuju za uže sa Animation Rigging (Two Bone IK) na tačke duž užeta. Lice: blend shapes ili zamena tekstura za tri raspoloženja. Kod igre ne sme da zavisi od toga da li su likovi primitivi ili modeli (vidi `ICharacterView` u `ARHITEKTURA.md`).
