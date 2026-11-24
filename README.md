# Arena Defender

A 2D top-down arena survival shooter built with **C#**, **.NET 9** and **MonoGame**.

You are the last defender in a neon arena. Waves of enemies pour in from the edges. They get faster, tougher and more numerous every 25 seconds. Survive as long as you can, chain kills for combo multipliers, grab power-ups and climb the local leaderboard.

---

## Features

| System | What's in the game |
|---|---|
| **Player** | WASD / arrow-key movement with smooth acceleration, mouse aiming, 100 health, 3 lives, respawn invulnerability |
| **Enemies** | 3 types with different behaviour: **Grunt** (standard chaser), **Runner** (fast and weak, with a limited turn rate so you can sidestep it), **Brute** (slow and tanky, with **frontal armour** that blocks 70 % of damage from the front) |
| **Attacks** | Primary blaster (hold left click / Space) with light aim assist. **Nova blast** (right click / E): a shockwave whose damage falls off with distance and that knocks enemies back, on a 6 s cooldown |
| **Collisions** | Circle collisions for player ↔ enemy, projectile ↔ enemy and player ↔ power-up. Enemies also push apart so they form a crowd instead of stacking |
| **Power-ups** | **Health** (+35 HP), **Speed** (×1.5 for 7 s), **Shield** (blocks all damage for 5 s), **Rapid Fire** (×2.5 fire rate for 7 s). Enemies drop them and they also spawn on a timer. Pickups are pulled towards you when you get close |
| **Scoring** | Points per kill × combo multiplier (up to ×3), +5 points per second survived, +50 per pickup |
| **Difficulty** | A new wave every 25 s. Spawn rate, enemy health, enemy speed and enemy cap all scale with the wave. Runners unlock at wave 2 and Brutes at wave 3 |
| **UI** | Main menu with Play/Quit, controls and high scores. HUD with animated health bar, lives, score, combo, wave timer, nova cooldown and active power-ups. Pause menu. Game-over screen with stats and leaderboard rank |
| **Leaderboard** | Top-5 scores saved to a local JSON file |

## How to play

| Action | Keys |
|---|---|
| Move | **W A S D** or **arrow keys** |
| Aim | **Mouse** |
| Shoot | **Left mouse button** or **Space** (hold) |
| Nova blast | **Right mouse button** or **E** / **Shift** |
| Pause | **Esc** or **P** |
| Menus | Mouse, or **W/S / arrows** + **Enter** |

**Tips**
- Kill enemies quickly one after another to build your **combo**. The multiplier resets if you go 2.5 s without a kill.
- **Brutes** shrug off bullets from the front (sparks fly off their armour). Circle around them and shoot them in the back. The nova pushes them away, but their armour also soaks a blast that hits the front.
- **Runners** can't turn quickly. Step sideways at the last moment and they'll overshoot.
- Health packs become more common when you are below half health.

## How to run

### Requirements
- **.NET 9 SDK or later**. Check with `dotnet --version`.
- **Visual Studio Code** with the *C# Dev Kit* extension (recommended).
- Windows, macOS (Intel or Apple Silicon) or Linux.

MonoGame is downloaded automatically from NuGet as a package. **No content pipeline or asset files are needed.** All graphics (shapes and the pixel font) are generated in code.

### Run the game
```bash
git clone https://github.com/<your-username>/ArenaDefender.git
cd ArenaDefender
dotnet run --project src/ArenaDefender
```

### Run the unit tests
```bash
dotnet test
```

### Build everything
```bash
dotnet build ArenaDefender.sln
```

> **Linux only:** if the window fails to open, install SDL2/OpenGL libraries, e.g. `sudo apt install libsdl2-2.0-0 libopenal1`.

## Project structure

```
ArenaDefender/
├── ArenaDefender.sln
├── src/
│   ├── ArenaDefender.Core/        ← ALL game rules. Pure C#, no MonoGame dependency
│   │   ├── Mathematics/GameMath.cs   distance, vectors, dot, cross, lerp
│   │   ├── Components/               Health, Cooldown
│   │   ├── Entities/                 Entity, Player, Projectile (+ pool), PowerUp
│   │   ├── Enemies/                  Enemy (abstract), Grunt, Runner, Brute, EnemyFactory
│   │   ├── PowerUps/                 IPowerUpEffect strategies, catalog, active effects
│   │   ├── Systems/                  Score, Difficulty, Spawner, Combat, WeightedRandom
│   │   ├── Persistence/              High-score table + JSON repository
│   │   └── World/                    GameWorld (the simulation), PlayerInput, events
│   └── ArenaDefender/             ← MonoGame front-end: screens, drawing, input, effects
│       ├── Screens/                  ScreenManager (stack), Menu, Gameplay, Pause, Game Over
│       ├── Graphics/                 ShapeRenderer, PixelFont, Camera2D, WorldRenderer, Palette
│       ├── UI/                       Hud, Menu/Button, AnimatedBar, MenuBackground
│       ├── Effects/                  Particles, floating text
│       └── Input/                    InputState → PlayerInput
├── UnitTests/
│   └── ArenaDefender.Tests/       ← NUnit tests (114 test methods, 154 cases)
└── docs/
    ├── ARCHITECTURE.md             application design document
    ├── TESTING.md                  testing strategy and results
    └── PRESENTATION_NOTES.md       answers to the presentation questions
```

## Documentation

- [Application Design Document](docs/ARCHITECTURE.md): architecture, diagrams, patterns, data structures and where each maths concept is used.
- [Testing Strategy](docs/TESTING.md): approach, edge cases, results and a failing-test story.
- [Presentation Notes](docs/PRESENTATION_NOTES.md): prepared answers to the assessment questions.

Public types and members carry XML documentation comments (`///`), and the projects generate XML documentation files on build.

## Where saved data goes

| File | Location |
|---|---|
| High scores | `<LocalAppData>/ArenaDefender/highscores.json` |
| Log (errors and warnings) | `<LocalAppData>/ArenaDefender/log.txt` |

`<LocalAppData>` is `%LOCALAPPDATA%` on Windows, `~/Library/Application Support` on macOS and `~/.local/share` on Linux. Delete `highscores.json` to reset the leaderboard.

## Author

Nkingi Gakindi Chris, BSc Software Engineering, African Leadership University
