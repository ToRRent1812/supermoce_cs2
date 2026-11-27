# Supermoce CS2

Fork jrandomskills (który jest forkiem drandomskills), używany na serwerze Nostalgia.
Losowe supermoce dla graczy, przebudowane pod aktualne CS2 i CounterStrikeSharp.

## Co jest innego?

- Odchudzenie skryptu z niepotrzebnych funkcji
- Optymalizacja kodu
- Dodatkowe null checki by zminimalizować ryzyko zawieszenia serwera
- Usunąłem CS2FlashingHudFix ponieważ znacznie zwiększa zużycie procesora i psuje rozgrzewkę.
- Dodałem 3 nowe wzorce pisania mocy: pasywne, aktywne i menu
- Zmieniono wiadomość powitalną, by nauczyć graczy używania mocy.
- Usunąłem supermoce, które nie podobają się graczom z serwera
- Opis supermocy dostępny na środku ekranu, zamiast na czacie
- Usunięto niepotrzebny mi system językowy oraz configi
- Niektóre supermoce mają zmienioną nazwę lub funkcję
- Wszystkie supermoce na przycisk mają losowy cooldown
- Dodałem nowe moce: Komandos, Dopaminka, CS2, Trujące naboje, Ciepłe piwo, Ćpun, Komornik, Rusznikarz, Krwiopijca, Matrix, Chachmęciarz, Bąk morderca, Podejrzane naboje, Zdrajca, Szybka pomoc, Half-Life, Owsiak, Robin Hood, Chemik, Ciężarne hosty, Strzał w kolano, Skarbówka, Inflacja, NanoKevlar, Olbrzym, Zamiana respów, Psychopata, Burza piaskowa, Nalot powietrzny, Konfiskata, Super Smoke, Wąski, Mr. Błysk, Granat sprawiedliwości, Realizm, Medytacja, Telewabik
- Dodałem własny HUD Panorama przez co plugin nie gryzie się z innymi pluginami

I wiele więcej, ale już zapomniałem, co się zmieniło.

## Instalacja

Pobierz i zainstaluj na serwerze:
- [Najnowszy Metamod:Source](https://www.metamodsource.net/downloads.php?branch=dev)
- [Najnowszy CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases)
- [Najnowszy MultiAddonManager](https://github.com/Source2ZE/MultiAddonManager) z wymuszeniem pobierania [tego dodatku](https://steamcommunity.com/sharedfiles/filedetails/?id=3846338191)

Pobierz .zip z zakładki [Releases](https://github.com/ToRRent1812/supermoce_cs2/releases)  
Wypakuj do ```server/game/csgo/addons/counterstrikesharp/```  
Dopisz wewnątrz
`game/csgo/cfg/multiaddonmanager/multiaddonmanager.cfg`

```
mm_extra_addons "3846338191"
```

