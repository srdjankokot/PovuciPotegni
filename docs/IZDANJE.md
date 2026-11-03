# Izdanje: šta je ostalo da se uradi ručno

Kod je spreman, a ovi koraci traže tvoje naloge, ključeve ili urednika.

## 1. Pitanja

1. Urednik pripremi JSON sa 500 proverenih pitanja (format u `docs/GDD.md`).
2. U Unityju: **Povuci-Potegni → Uvezi pitanja…** i izaberi fajl. Neispravan fajl se ne uvozi, a izveštaj navodi svaku grešku.
3. **Povuci-Potegni → Proveri pitanja** ispisuje broj, raspodelu po kategorijama i težinama, duplikate i šta još blokira izdanje.
4. U `Assets/_Project/Settings/GameConfig.asset` isključi **Allow Unverified**. Build za prodavnicu to proverava i odbija da radi bez toga.

## 2. Android

- **Test na telefonu:** **Povuci-Potegni → Build → Android test APK**, pa instaliraj `Builds/Android/PovuciPotegni-test.apk`.
- **Ključ za potpisivanje:** Project Settings → Player → Android → Publishing Settings → Keystore Manager. Sačuvaj keystore i lozinke van projekta; bez njih kasnije ne možeš da izdaš novu verziju iste aplikacije.
- **Prodavnica:** **Build → Android AAB za prodavnicu**, pa `Builds/Android/PovuciPotegni.aab` otpremi na Google Play Console (potreban je nalog).

## 3. iOS

1. **Build → iOS za TestFlight** pravi Xcode projekat u `Builds/iOS`.
2. Otvori `Unity-iPhone.xcodeproj` u Xcode-u, u Signing & Capabilities izaberi svoj Team (potreban je Apple Developer nalog).
3. Product → Archive, pa otpremi na TestFlight.

## 4. Firebase (Analytics i Crashlytics)

Igra sada beleži događaje kroz `IAnalytics`, a `NullAnalytics` ih samo ispisuje u Console. Za prave podatke:

1. Napravi Firebase projekat i dodaj aplikacije sa ID-em `com.srdjankokot.povucipotegni`.
2. Stavi `google-services.json` i `GoogleService-Info.plist` u `Assets/` (ne ulaze u git).
3. Uvezi Firebase Unity SDK (Analytics, Crashlytics).
4. Napiši `FirebaseAnalytics : IAnalytics` i zameni `new NullAnalytics()` u `GameFlow`. Prijave pitanja se slično šalju kroz `IReportSink`.

Povratak posle 1 i 7 dana Firebase računa sam.

## 5. Pre slanja u prodavnicu

- Proveri ID aplikacije, verziju (`0.1.0`) i ime.
- Ikonica i splash su generisani iz igre (`Art/Branding`); zameni ih ako imaš pravi dizajn.
- Zvuci su sintetizovani kodom (`Editor/SoundBaker`); poslušaj ih i zameni ako treba.
