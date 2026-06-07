Ameko zostało zaprojektowane wokół projektów. Korzystanie z nich nie jest wymagane, możesz edytować napisy i korzystać
z Ameko bez nich, projekty zostały przemyślane pod kątem ułatwienia obsługi wielu napisów, szczególnie podczas
współpracy z innymi osobami.

## Podgląd projektu

![](../assets/project-explorer-empty.png)

Podczas pierwszego uruchomienia Ameko, przegląd projektu będzie po lewej stronie, zawierać będzie obecnie otwarte dokumenty. Jeśli nie wczytano pliku projektu, pliki otwarte podczas sesji zostaną wrzucone do domyślnego projektu. Możesz zapisać projekt do pliku, jeśli chcesz wykorzystać zalety korzystania z plików projektu.

## Otwieranie katalogu jako projekt

Jeśli masz już katalog projektu, możesz przenieść jego strukturę do Ameko, otwierając katalog jako projekt.
Spowoduje to wczytanie wszystkich podkatalogów i plików napisów do przeglądu projektu, gdzie można je edytować i zapisać wynikowy projekt do pliku.

## Key Names and Phrases

Chances are, your project has names and phrases you want to keep consistent throughout the show. Projects can have a Key
Names & Phrases (KNP) bible to help keep everyone on track:

![](../assets/knp-window.png)

Terms that appear in the script or reference files will appear in the Editing Area. See the User Interface tab for more
details.

## Nazwy wyświetlane i ty

Chociaż struktury i nazwy w projekcie _mogą_ odpowiadać plikom na dysku, możesz je przenosić i zmieniać ich nazwy wedle woli, bez wpływu na faktyczne pliki. Spójrzmy przykładowo na następującą płaską hierarchię plików z dokładnymi nazwami:

```
Kono Bijutsubu ni wa Mondai ga Aru/
  [AMK] Konobi - 01 - Dialogue.ass
  [AMK] Konobi - 01 - Typesetting1.ass
  [AMK] Konobi - 01 - Typesetting2.ass
  Konobi - 01 - Captions.ja.srt
  [AMK] Konobi - 02 - Dialogue.ass
  [AMK] Konobi - 02 - Typesetting1.ass
  [AMK] Konobi - 02 - Typesetting2.ass
  Konobi - 02 - Captions.ja.srt
```

Mogą zostać przeorganizowane i oczyszczone w projekcie za pomocą nazw wyświetlanych i folderów bez ingerowania w istniejące pliki:

```
01/
  Dialogue.ass
  TS1.ass
  TS2.ass
  Captions.srt
02/
  Dialogue.ass
  TS1.ass
  TS2.ass
  Captions.srt
```

## Ustawienia projektu

![](../assets/project-config.png)

Jedną z kluczowych zalet korzystania z projektów podczas pracy w zespole jest synchronizacja ustawień. Opcje ustawione w ustawieniach projektu nadpiszą ustawienia użytkownika na czas pracy z projektem. Jest to świetny sposób, aby upewnić się, że każdy będzie miał ten sam próg ostrzeżenia ZNS i, co ważniejsze, wspólny słownik sprawdzania pisowni oraz język. Jeśli w projekcie ustawiono język np. angielski (UK), _każdy_ będzie korzystał z angielski (UK), a to dodatkowe u w colour nie zostanie przeoczone.

![](../assets/project-install-dictionary.png)

Użytkownik zostanie poproszony o pobranie odpowiedniego słownika, jeśli jeszcze go nie posiada.

![](../assets/spellcheck.png)

Słowa mogą zostać dodane do słownika projektu bezpośrednio z korektora pisowni.

## Integracja z Git

![](../assets/git-toolbox.png)

Po zapisaniu w katalogu projektu (obok katalogu `.git`), pliki projektów pozwalają na łatwy dostęp do podstawowych funkcji Git, jak zatwierdzanie, wypychanie, ściąganie i przeglądanie listy najnowszych zatwierdzeń.
