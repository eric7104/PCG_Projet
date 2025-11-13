# 🧩 Génération Procédurale sur Grille — Unity
*Framework modulaire pour donjons, biomes et cartes procédurales.*

![Screenshot Placeholder](./Docs/screenshot_main.png)

---

# 📖 Table des matières
2. [Fonctionnalités](#-fonctionnalités)
3. [Architecture](#-architecture)
4. [Système de Grille](#-système-de-grille)
5. [Gestion des Objets (Tiles)](#-gestion-des-objets-tiles)
6. [Pipeline de Génération](#-pipeline-de-génération)
7. [Méthodes de Génération](#-méthodes-de-génération)
   - [SimpleRoomPlacement](#1️⃣-simpleroomplacement)
   - [BSP2](#2️⃣-bsp2-bsp-classique-amélioré)
   - [Cellular Automata](#3️⃣-cellular-automata)
   - [Noise](#4️⃣-noise-opensimplex-biomes)
8. [Comparatif](#-comparatif-rapide)
9. [Utilisation](#-utilisation)
10. [Extensibilité](#-extensibilité)
11. [Glossaire](#-glossaire)
12. [Crédits](#-crédits)

---

# 🎯 Fonctionnalités
- Grille centrée (Grid + Cell)
- Placement intelligent d’objets via Template → Factory → Controller
- Génération **asynchrone** (UniTask + cancellation)
- Génération déterministe via **RandomService**
- Plusieurs algorithmes :
  - SimpleRoomPlacement
  - BSP2
  - Cellular Automata
  - Noise-based biomes
- Debug visuel intégré
- Facile à étendre

---

# 🏛️ Architecture
```
ProceduralGridGenerator
│
├── ProceduralGenerationMethod (ScriptableObject)
│     ├── SimpleRoomPlacement
│     ├── BSP2
│     ├── CellularAutomata
│     └── Noise
│
├── Grid / Cell
│
└── GridObjectTemplate → GridObjectFactory → GridObjectController
```

![Screenshot Placeholder](./Docs/screenshot_griddebug.png)

---

# 🟦 Système de Grille

## Grid
- `Width`, `Lenght` (typo volontaire), `CellSize`
- Origine centrée (`OriginPosition`)
- Fonctions clés :
  - `GetWorldPosition()`
  - `TryGetCellByCoordinates()`
  - `DrawGridDebug()`

## Cell
- Coordonnées (Vector2Int)
- Stocke un `GridObject` + son `GridObjectController`
- Méthodes :
  - `AddObject(template, override)`
  - `ClearGridObject()`

---

# 🧱 Gestion des Objets (Tiles)

## GridObjectTemplate
- Nom logique (ex : Grass, Room, Water)
- Prefab Unity utilisé comme vue

## GridObjectFactory
- `SpawnOnGridFrom(template, cell)`  
- Gestion complète de l’override

## GridObjectController
- Gère position, rotation et apparence

---

# ⚙️ Pipeline de Génération
1. Création de la grille  
2. Exécution de la méthode procédurale  
3. Placement des tiles  
4. Remplissage du sol (selon algo)  
5. Debug optionnel  

---

# 🧠 Méthodes de Génération

## 1️⃣ SimpleRoomPlacement
- Placement de salles rectangulaires non chevauchées  
- Couloirs en L  
- Remplissage des zones vides → Grass  
- Override : Rooms = true, Corridors = true

## 2️⃣ BSP2 (BSP classique amélioré)
- Découpe récursive avec ratio  
- Chaque leaf génère une Room  
- Connexions hiérarchiques  
- Override sélectif  
- Non async

## 3️⃣ Cellular Automata
- Initialisation eau/herbe  
- Itérations avec règles de voisinage  
- Très organique  
- Coût élevé sur grandes grilles

## 4️⃣ Noise (OpenSimplex Biomes)
- Water → Sand → Grass → Rock  
- FastNoiseLite (FBm)  
- Très performant  

---

# 🚀 Utilisation
```csharp
var generator = FindObjectOfType<ProceduralGridGenerator>();
await generator.GenerateGrid();
```

Paramétrer :
- méthode (ScriptableObject)
- seed
- debug
- step delay

---

# 🔧 Extensibilité
1. Créer un ScriptableObject dérivé de `ProceduralGenerationMethod`
2. Utiliser `RandomService`
3. Utiliser `AddGridObjectToCell`
4. Gérer `cancellationToken`

---

# 📚 Glossaire
- Room : zone rectangulaire  
- Corridor : couloir en L  
- Tile : objet visuel issu d'un template  
- BSP : Binary Space Partitioning  
- FBm : Fractal Brownian Motion  

---

# 📜 Crédits
- FastNoiseLite (OpenSimplex2)
