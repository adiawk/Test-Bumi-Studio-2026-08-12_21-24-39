# Dungeon Deckbuilder

A 2D turn-based deckbuilder prototype for the Bumi Studio programmer technical test

## Game Overview

This is a short dungeon run played with cards. You start with a small deck, pick rooms on a chapter map, and fight enemies in turn-based combat.

Each combat turn you draw a hand, spend energy to play cards, then end the turn so enemies resolve their telegraphed intents. Cards deal damage, gain Block, heal, or draw. Block absorbs damage before HP. HP and the run deck persist between fights.

**Win:** clear the boss room.  
**Lose:** reach 0 HP in combat.  
After either result you can start a new run.

The map has multiple rooms (normal combat, elite, reward, boss), so a run has more than two decision points. Reward rooms let you add one card from a random offer of three.

## How to Run

- Engine & version used: Unity 6000.0.75f1, C#
- Language: C#
- Platform: Windows PC
- Build location: https://adiawk.itch.io/dungeon-deckbuilder

### Play the Windows build

1. Download rar file
2. Open and run `Test Bumi Studio.exe`.
3. If Windows SmartScreen appears, choose **More info** → **Run anyway**.

### Play from the Unity Editor

1. Install Unity Hub and editor version **6000.0.75f1**.
2. Open this project folder (`Test Bumi Studio`).
3. Open `Assets/Scenes/MainMenu.unity`.
4. Press Play.

### Controls

- **Main Menu:** Start New Run
- **Map:** click a reachable room (green). Cleared rooms are dark; locked rooms cannot be clicked.
- **Combat:**
  - Drag a card onto an enemy (attack) or onto yourself / lift it up (Defend, Heal, Quick Draw)
  - Or click a card, then click its target
  - Right-click cancels a pending card
  - **End Turn** discards the hand and lets enemies act
- **Reward:** click one of the three cards to add it to the run deck
- **Result:** New Run starts a fresh run

Starter cards: Strike (6 damage), Defend (8 Block), Heavy Strike (12 damage), Heal (5 HP), Quick Draw (draw 2).

## Technical Decisions

I treated this as an architecture test, not a content test. The card system had to stay data-driven so new cards do not need new C# classes.

**Scene routing vs run state.** `GameManager` is a persistent singleton. It only loads scenes (`MainMenu`, `Map`, `Combat`, `Reward`, `Result`). `RunManager` owns one run: HP, the master deck, and map progress. Combat objects are destroyed with the Combat scene; HP is written back to `RunManager` when the fight ends. I did not put deck or HP on `GameManager` so routing and run rules stay separate.

**Cards as data + composed effects.** `CardData` is a ScriptableObject (name, cost, target type, list of effects). `RuntimeCard` is the in-run instance and only points at that asset. Effects (`DamageEffect`, `BlockEffect`, `HealEffect`, `DrawEffect`) are also ScriptableObjects. A card is a list of effects, not a subclass. `EffectContext` passes `IEffectTarget` and `ICardDrawer`, so effects do not depend on `Player` or `Enemy`. Adding a card in the editor is: create a `CardData`, assign cost/target, drag in effect assets.

**Two decks.** `RunManager.runDeck` is the persistent list of `CardData` for the whole run. `DeckManager` clones it into draw / hand / discard for one fight, then throws those clones away when the scene unloads. Rewards append to the run deck, so the next fight sees the new card. I did not mutate combat piles across scenes.

**Shared combat math.** `Player` and `Enemy` both wrap a `Combatant` for HP and Block. Damage hits Block first, then HP. That keeps the rules in one place.

**Turns.** `TurnManager` is a two-phase loop: player turn (clear Block, reset energy, draw 5) then enemy turn (discard hand, each living enemy executes intent, then picks the next one). Enemies telegraph Attack or Defend. AI is a fixed Attack → Defend cycle on purpose — readable, and not a fake behaviour tree.

**Map is authored, not generated.** `StageManager` only stores cleared node ids and the current selection. `MapUI` reads `StageNodeButton` objects placed in the scene, with `NextNodes` wired by hand. Reachability is “starting nodes” at run start, then the last cleared node’s neighbours. I skipped procedural maps so the run loop and card systems could ship first.

**UI does not own rules.** Menu, map, combat HUD, reward, and result scripts call `GameManager`. `CombatUI` rebuilds hand views from `DeckManager.Hand`. Card drag/targeting lives on `UICard` + `CombatManager` so the HUD is not the rules layer.

**What I deliberately skipped**

- No paid plugins and no card-game framework. Gameplay code is custom. Visuals come from a free 2D character pack.
- No card remove, or synergies. Those are stretch goals; the effect list can support them later without rewriting combat.
- No save/load. A run lives in memory on the persistent managers.
- Unity MCP (`com.coplaydev.unity-mcp`) is included as an editor/development helper. It is not a gameplay framework and is not required to play the build.

## What I Would Do With More Time

If I had 2–3 extra days, in this order:

1. **Windows build** and a pass on first-run UX: energy, Block, and intent are readable, but the map and reward screens still look like placeholders.
2. **Card remove (and a simple upgrade).** A rest-site node that removes one card, or an upgrade that swaps a `CardData` reference (Strike → Heavy Strike) without new combat code.
3. **Richer enemy intents.** Weighted patterns, multi-enemy focus fire, and at least one boss-only intent, still using `EnemyIntent` so the telegraph UI stays the same.
4. **End-of-run summary.** Cards added, rooms cleared, remaining HP — the Result scene currently only shows Victory / Defeat.
5. **A short tutorial beat** on the first map: energy, Block, and “enemies act after End Turn”.