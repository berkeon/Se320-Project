# Last Line (SE 320 Tower Defense)

Unity project for the SE 320 group project (Berke, Murat, Cavit, Şevval).

## Opening the project

1. Install **Unity Hub** and a **Unity 6.3 LTS** editor (6000.3.x). If the Hub asks about the
   editor version, choose any installed 6000.3 version.
2. Unity Hub > **Add** > **Add project from disk** > select this `TowerDefense` folder.
3. The first time it opens, Unity imports the TextMesh Pro essentials and then builds the scenes,
   prefabs and materials by itself (`Assets/Editor/SceneBuilder.cs`). A dialog says when it's done.
4. The `MainMenu` scene is open. Press **Play**, then **Start**.

If nothing gets built (for example because of an error on first import), use the menu
**SE320 > Rebuild Scenes and Prefabs**.

If the Console says `TMPro` or `UnityEngine.UI` can't be found, open **Window > Package Manager**,
install **Unity UI** (`com.unity.ugui`) from the Unity Registry, and let it recompile.
If the scenes appear but text is missing, run **Window > TextMeshPro > Import TMP Essential Resources**
and then the rebuild menu above.

Tip: set the Game view to **1920x1080** so you see the final UI layout.

## How to play right now

- Pick a tower in the panel at the bottom (the selected one turns green), then click a grey build spot.
- Enemies spawn at the dark red square and walk the path to the blue base.
- Each enemy that reaches the base costs 1 base health. At 0 the game over screen appears (Restart reloads the level).
- Kills give money, which you spend on more towers. The next wave starts by itself after a countdown.

## What is in the game (week 4 state)

| Area | What exists |
|---|---|
| Enemies | Path with waypoints. Three enemy types (basic, fast and weak, slow and tanky) share the `Enemy` base class. Speed, health and reward are editable per prefab. Each enemy has a small "nose" cube to show which way it faces. |
| Base | Base health (20), reduced when an enemy gets through. |
| Towers | Two tower types (basic fast-firing, slow heavy-damage) share the `Tower` base class. Range is a trigger sphere. The head turns to the closest enemy in range. Fire-rate delay. |
| Projectiles | Follow their target, have a trail renderer, and are destroyed if the target is already gone. |
| Waves | Spawner at the start of the path. Waves grow by 2 enemies each time, with an automatic countdown between them. One enemy type per wave for now (basic, fast, tank, then repeat). |
| Economy | Start money 150. Each enemy type has its own reward, each tower its own cost. Towers are bought by clicking a build spot (camera raycast on the `BuildSpot` layer). |
| UI | Main menu (Start, Quit, click sound), HUD (base health, money), tower selection panel, game over screen with Restart. Canvas Scaler set to Scale With Screen Size (1920x1080). UI clicks don't go through to the game world. |

**Still open:** the numbers are rough guesses, the first waves feel too easy, there are no boss enemies,
no wave counter on screen, no slowing or area-damage tower yet. Those are the week 5+ tasks.

## Who owns which files

| Person | Area | Files |
|---|---|---|
| Berke | Enemy path, movement, enemy types | `Scripts/Enemies/` (`Enemy`, `BasicEnemy`, `FastEnemy`, `TankEnemy`, `EnemyPath`, `PlayerBase`) |
| Murat | Tower targeting, projectiles, tower types | `Scripts/Towers/` (`Tower`, `BasicTower`, `HeavyTower`, `Projectile`) |
| Cavit | Waves, economy, buying and placing towers | `Scripts/WavesAndEconomy/` (`WaveManager`, `MoneyManager`, `TowerBuilder`, `BuildSpot`) |
| Şevval | UI, menus, HUD | `Scripts/UI/` (`HUD`, `MainMenu`, `TowerPanel`, `GameOverScreen`) |

## Balancing values (all editable in the Inspector)

| Thing | Value |
|---|---|
| Basic enemy | speed 2.5, health 6, reward 10 |
| Fast enemy | speed 4.5, health 3, reward 8 |
| Tank enemy | speed 1.4, health 20, reward 25 |
| Basic tower | cost 50, range 5, 2 shots/s, damage 1 |
| Heavy tower | cost 120, range 6, 0.5 shots/s, damage 5 |
| Waves | first wave 5 enemies, +2 per wave, 1.2 s between spawns, 5 s before wave 1, 10 s countdown between waves |
| Player | 20 base health, 150 start money |

## External help (has to be disclosed)

The course rules say all external help must be mentioned in the workshops, the presentation and the final report.

- **Click sound:** `Assets/Audio/click.wav` is `click1.wav` from Kenney's "UI Audio" pack (CC0, public domain),
  taken from the mirror https://github.com/Calinou/kenney-ui-audio (original: https://kenney.nl/assets/ui-audio).
  Credit: Kenney (kenney.nl). The weekly reports mention freesound.org, so either change the reports to say
  Kenney / CC0 or swap in a freesound sound before the final report.
- **Hit sounds:** `Assets/Audio/hit.wav` (Basic Tower, `click4.wav`) and `Assets/Audio/heavy_hit.wav` (Heavy Tower,
  deeper `rollover6.wav`) are both from the same Kenney pack (CC0). They play quietly when a projectile hits
  an enemy (`Projectile.cs`, volume field `Hit Volume` on each projectile prefab).
- **Build sounds:** `Assets/Audio/place.wav` (Basic Tower, `rollover2.wav`, light) and `Assets/Audio/heavy_place.wav`
  (Heavy Tower, `click2.wav`, low thud) from the same Kenney pack (CC0). They play once when a tower is built
  (`Tower.cs`, field `Place Volume` on each tower prefab). `Scripts/Audio/SoundEffects.cs` plays one-shot sounds.
- No tower-defense tutorials were used.

## Folder layout

```text
Assets/
  Scripts/Enemies, Towers, WavesAndEconomy, UI, Audio   game code
  Editor/SceneBuilder.cs                   one-time setup tool (can be deleted after the first build)
  Audio/click.wav, hit.wav, heavy_hit.wav, place.wav, heavy_place.wav
  Scenes/, Prefabs/, Materials/                   created on first open
ProjectSettings/ProjectVersion.txt               Unity creates the rest