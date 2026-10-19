# GAME REFERENCE SPECIFICATION — TOP-DOWN PIXEL ACTION RPG

## Source and limitations
- Video: `gameplay_reference.mp4` (~62.55s, 576x1024, portrait 9:16); selected stills in `frames/`.
- This is a **reference prototype**, not a request to reuse copyrighted art/audio or recreate the original source code.
- Certain mechanics cannot be established from footage (control scheme, numeric formulas, upgrade system). Mark such mechanics as ASSUMPTIONS and implement sensible configurable defaults rather than claiming the footage proves them.

## Video observations (grounded)
- 2D top-down camera, pixel-art fantasy forest biome; greens with pale gray/green dirt/grass, clustered stylized trees, foliage, cliffs/path edges.
- A small white-haired adventurer with blue/dark clothing wanders between distinctive green horned tents, wooden bowls/braziers, skull/bone piles and flags.
- Camera remains player-centered or follows during roaming.
- Enemies labeled `Goblin Lv.3` and `Gobrider Lv.5` appear around camp. Many congregate around hero; some display health bars.
- Combat seen ~35–55 s includes blue/cyan/white sword swing and dash/impact visuals, floating damage numbers, multi-hit counter (`x2 HITS`, etc.), dust/particle bursts, enemy health changes.
- Bottom-anchored hotbar with pixel framed icons, labels visible such as 2, 3, 4, R, V, C; a selected skill is highlighted yellow; some icons show cooldown counts/overlay.
- A horizontal red status bar is visible near upper edge during combat; exact meaning cannot be determined solely from frames (treat as hero HP for prototype, mark assumption).

## Technical target
- Unity 6+, 2D project, portrait 9:16 default (test 576x1024, 1080x1920). Keep core game in Unity, not Flutter. Optional Flutter integration **only later**, if explicitly requested.
- Pixel-Perfect/Orthographic camera that smoothly tracks hero, nearest neighbor sprite sampling, integer-friendly pixel scale, sprites crisp with no blur.
- Game systems separated into PlayerMovement, CameraFollow, EnemyController, EnemySpawner, Health/Damage, Combat/Skills, CooldownUI, DamagePopup, FX.
- Do not overwrite existing Scenes or delete any assets; create a separate test Scene e.g. `GameplayPrototype.unity`.
- First attempt to reuse user-owned existing assets. If missing, use simple **original** placeholder pixel-art sprites/shapes and describe missing assets; do not fetch/store third-party assets without approval.

## Controls (prototype assumptions)
- WASD/arrow move on PC; mobile joystick later.
- Default basic attack either click/space or auto-target nearest enemy with clear animation. Skills bound 2,3,4,R,V,C as shown on hotbar (actual effects/inputs not fully evidenced).
- Build initial minimum: basic melee slash (cooldown 0.35s), dash/blue shockwave (cooldown 4s), radial AoE combo (cooldown 7s). All configurable; don't pretend values are observed facts.

## Enemies and loop
- Goblin melee (Lvl 3), Gobrider bruiser (Lvl 5), distinct hp/speed/attack/range. Detect player within aggro radius, chase around obstacles (basic collision avoidance for prototype), attack in range, receive knockback/hit flash, die with effect.
- Spawn patrol groups at forest camps, with limits and no spawning on hero.
- Hero moves around camp, attracts enemies, slashes them; enemies show labels/health; damage numbers and hit counters are legible; hero can escape to quieter area.
- Hero HP + damage reception, death/reset optional for later stage.

## Presentation
- Terrain is the focal point, with character small, environment fairly detailed, large readable VFX bursts during combat.
- Bottom hotbar must never shift when camera follows. Use Unity UI Canvas safe area anchors; key labels and dark pixel frames; selected icon yellow border; countdown overlays.
- Floating white damage numbers and combo counter disappear after a short animation.
- Pixel art must remain legible in 9:16 at multiple resolutions.

## Incremental implementation milestones
1. Observe project and summarize existing systems/assets; ask for clarification only when truly blocking. Snapshot with Git before edits.
2. Stage 1 (PLAYABLE): new scene; original placeholder pixel environment and player; player movement; camera follow; orthographic portrait layout; terrain collisions. Check Unity Console.
3. Stage 2 (COMBAT LOOP): goblin and gobrider; health bars + names; detection/chasing; basic attack + damage popups; enemies die. Check Unity Console and Play Mode.
4. Stage 3 (SKILLS): blue/white crescent slash, dash shockwave, wide attack, cooldown and bottom hotbar, multiple hit counter; small VFX; fps target 60 on desktop.
5. Stage 4 (POLISH): tent cluster/props/flag/bone details, attack feedback, animation, sprite layering, spawn pacing, sound placeholders (only user-owned audio), performance.

## Completion checks
- Scene runs in Play Mode with **no compile errors**.
- Hero moves and camera tracks without jitter; 2D sprite art crisp.
- At least 3 Goblins and 1 Gobrider can spawn/chase/take damage/die.
- Combat creates visible blue/white arc, damage text, hit counter, health loss.
- Skills cannot fire during cooldown and hotbar visibly counts down.
- Bottom hotbar always visible in portrait Game view.
- Agent reports exact files/Scenes created, Unity MCP tools actually invoked, test outcomes, and next steps. Do not report a successful MCP call unless tool logs show it.
