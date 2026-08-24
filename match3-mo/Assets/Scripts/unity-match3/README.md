# Unity Match-3 Logic (ported from Cocos Creator)

This folder is a **logic-only** port of the match-3 rules in `logic/game`.
Cocos views, tweens, and Excel tables are not copied. The algorithms are.

## What was ported

| Original (TypeScript) | Unity C# |
| --- | --- |
| `finder/helper/CellFinder.ts`, `GridFinder.ts` | `Finder/CellFinder.cs`, `GridFinder.cs` |
| `finder/implements/*` | `Finder/Finders.cs` |
| `checker/*` | `Checker/Checkers.cs`, `CheckMgr.cs` |
| `checker/CheckHelper.ts` | `Checker/CheckHelper.cs` |
| `drop/CellDownMgr.ts` + V1 notify | `Drop/DropSystem.cs` |
| `controller/MapController.ts` swap/cascade | `Engine/Match3Engine.cs` |
| `model/CellItemModel.ts` | `Core/Cell.cs` |
| Special cleanups (missile / keg / propeller / light ball) | `Resolve/SpecialResolver.cs` |

## Coordinate system (same as the original)

- `(0, 0)` is **bottom-left**
- `X` grows right, `Y` grows **up**
- Gravity moves cells toward **Y - 1**

## Match rules (same priority as `CHECK_DOWN_LIST`)

1. Special + special swap combos
2. 5 in a line → Light Ball
3. T shape → Powder Keg
4. L shape → Powder Keg
5. 4 horizontal → Horizontal Missile
6. 4 vertical → Vertical Missile
7. 2×2 square → Propeller
8. 3 in a line → normal clear

Adjacent obstacles / grass take one hit when a neighbor is cleared (`canCleanUpByRoundItem`).

## How to use in Unity

1. Copy `unity-match3/Runtime` into your Unity project (`Assets/Match3/`).
2. Put `Match3BoardView` on a scene object, assign a cell prefab.
3. Call `engine.TrySwap(a, b)` from input. Play the returned commands.

Core logic does not need GameObjects. `Match3BoardView` is only a sample visual.

The original game loaded finder/checker parameters from Excel (`CheckItemModel`, `FinderItemModel`, `ShapeItemModel`). Those tables are not in this dump, so default Royal-Match-style shapes and checker order are hardcoded in `ShapeTable.cs` and `CheckMgr.cs`. If you later export those JSON/Excel rows, plug them into `CheckMgr.LoadFromConfig`.
