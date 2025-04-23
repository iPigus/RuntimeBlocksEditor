# Przewodnik Użytkownika - Runtime Blocks Editor

## Spis Treści

1. [Wprowadzenie](#wprowadzenie)
2. [Instalacja](#instalacja)
3. [Podstawowe Użycie](#podstawowe-użycie)
4. [Zaawansowane Funkcje](#zaawansowane-funkcje)
5. [Rozwiązywanie Problemów](#rozwiązywanie-problemów)

## Wprowadzenie

Runtime Blocks Editor to narzędzie, które pozwala na dynamiczną edycję bloków w czasie rzeczywistym w Twojej grze Unity. Jest to idealne rozwiązanie dla gier typu sandbox, kreatorów poziomów lub gier z elementami budowania.

## Instalacja

1. Pobierz pakiet z Unity Asset Store
2. W Unity, przejdź do `Assets > Import Package > Custom Package`
3. Wybierz pobrany plik .unitypackage
4. Upewnij się, że wszystkie pliki są zaznaczone i kliknij "Import"

## Podstawowe Użycie

### Konfiguracja TriplanarMaterialManager

1. Utwórz nowy obiekt w scenie (prawy przycisk myszy w hierarchii > Create Empty)
2. Zmień nazwę na "BlockManager"
3. Dodaj komponent `TriplanarMaterialManager` (Add Component > Scripts > TriplanarMaterialManager)
4. Skonfiguruj podstawowe parametry:
   - Base Texture: główna tekstura bloku
   - Normal Map: mapa normalnych (opcjonalnie)
   - Tiling: powtarzanie tekstury
   - Blend Strength: siła mieszania tekstur

### Tworzenie Bloków

1. W trybie edycji:
   - Użyj lewego przycisku myszy do tworzenia bloków
   - Prawy przycisk myszy do usuwania
   - Środkowy przycisk myszy do modyfikacji istniejących bloków

2. W czasie rzeczywistym:
   - Użyj klawisza 'B' do przełączenia trybu edycji
   - Wszystkie operacje są takie same jak w trybie edycji

## Zaawansowane Funkcje

### Custom Materials

Możesz tworzyć własne materiały z różnymi teksturami dla każdej strony bloku:

1. Utwórz nowy materiał (Create > Material)
2. Ustaw shader na "Custom/TriplanarBlock"
3. Skonfiguruj tekstury dla każdej strony
4. Przypisz materiał do TriplanarMaterialManager

### Optymalizacja

- Używaj LOD (Level of Detail) dla lepszej wydajności
- Włącz "Use GPU Instancing" dla masowych operacji
- Dostosuj "Chunk Size" w zależności od potrzeb

## Rozwiązywanie Problemów

### Częste Problemy

1. **Bloki nie są widoczne**
   - Sprawdź, czy materiał jest poprawnie przypisany
   - Upewnij się, że URP jest aktywny w projekcie

2. **Wydajność jest niska**
   - Zmniejsz rozmiar chunków
   - Włącz optymalizacje w TriplanarMaterialManager
   - Użyj LOD dla odległych bloków

3. **Tekstury się nie mieszają**
   - Sprawdź ustawienia Blend Strength
   - Upewnij się, że tekstury mają odpowiedni format
   - Sprawdź ustawienia Tiling

### Wsparcie

W przypadku problemów:
1. Sprawdź dokumentację w folderze Documentation
2. Skontaktuj się przez email: [Twój email]
3. Odwiedź stronę na Unity Asset Store 