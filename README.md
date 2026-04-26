# Outbreak2D — 2D Zombie Survival Multiplayer Shooter

A top-down 2D zombie survival shooter built with **Unity 6 + Universal Render Pipeline** and **Photon Fusion 2 (Shared Mode)** networking.

---

## Features

- **Photon Fusion Shared Mode** – no dedicated server required; every player is a peer
- **Input-authoritative player movement** – WASD / gamepad stick; mouse-aim rotation synced to all clients
- **Hitscan shooting** via Fusion's lag-compensated raycast
- **Zombie AI** driven by the "Zombie Master" (the player with State Authority over all zombie objects)
- **Wave system** with increasing difficulty and zombie variant composition
- **Networked scoring** – kill credit attributed per player
- **Full UI** – Lobby, HUD, Game Over screen

---

## Project Structure

```
Assets/
  Scripts/
    Player/
      PlayerInput.cs        INetworkInput struct (move, aim, shoot, reload)
      PlayerMovement.cs     Rigidbody2D movement + networked aim angle
      PlayerAnimator.cs     Drives Animator from velocity
      PlayerHealth.cs       Networked HP, damage RPC, respawn coroutine
      PlayerShooter.cs      Reads input and delegates to the equipped WeaponBase
      PlayerSpawner.cs      Spawns/despawns players at random SpawnPoints
    Weapons/
      WeaponData.cs         ScriptableObject – damage, fire rate, ammo, reload time
      WeaponBase.cs         Abstract NetworkBehaviour – ammo, fire-rate cooldown, reload
      Pistol.cs             Hitscan via Runner.LagCompensation.Raycast
    Zombies/
      ZombieData.cs         ScriptableObject – speed, HP, damage, score value
      ZombieController.cs   NavMeshAgent AI (Idle/Chase/Attack/Dead); runs on Zombie Master
      ZombieHealth.cs       Networked HP; RPC from any client, applied on State Authority
    Game/
      GameLauncher.cs       INetworkRunnerCallbacks – session startup + input polling
      GameManager.cs        Top-level event wiring
      WaveManager.cs        Networked wave state, zombie spawning, score dictionary
      ScoreManager.cs       Read-only score accessor for UI
    UI/
      LobbyUI.cs            Room name input + Join button
      HUDController.cs      Health bar, ammo, wave counter, scoreboard
      GameOverUI.cs         Final stats + play-again / main-menu
    Audio/
      AudioManager.cs       Music, pooled positional SFX, event subscriptions
    Utilities/
      ObjectPool.cs         Generic GameObject pool (VFX, projectiles)
      PooledVFX.cs          Self-returning particle component
```

---

## Setup

### 1 – Photon App ID
1. Create a Fusion app at https://dashboard.photonengine.com
2. In Unity open **Window → Fusion → Realtime Settings** and paste your App ID

### 2 – Scenes
| Scene | Purpose |
|-------|---------|
| `LobbyScene` | Menu with `GameLauncher` + `LobbyUI` |
| `GameScene` | Gameplay with `WaveManager`, `PlayerSpawner`, HUD |

Add both to **Build Settings** (Lobby = index 0, Game = index 1).

### 3 – Prefabs
| Prefab | Components |
|--------|-----------|
| `PlayerChar` | `NetworkObject`, `Rigidbody2D`, `PlayerMovement`, `PlayerHealth`, `PlayerAnimator`, `PlayerShooter`, weapon child with `Pistol` |
| `Zombie_Walker` | `NetworkObject`, `Rigidbody2D`, `NavMeshAgent`, `ZombieController`, `ZombieHealth` |
| `Zombie_Runner` | Same as Walker with `ZombieData` asset for higher speed |

Register all network prefabs in the `NetworkRunner` prefab table or via a `NetworkPrefabTable` asset.

### 4 – Spawn Points
Place empty GameObjects tagged `SpawnPoint` in `GameScene` and wire them into `PlayerSpawner._spawnPoints[]`.

### 5 – NavMesh (for zombies)
1. Install **AI Navigation** package (`com.unity.ai.navigation`)
2. Add a `NavMeshSurface` component to your tilemap parent
3. Set **Agent Type** to 2D and bake

---

## Architecture Notes

### Shared Mode authority model
- Each **player** is State Authority over their own `PlayerHealth` – only they can reduce their own HP.
- The **Zombie Master** (`IsSharedModeMasterClient`) is State Authority over all `NetworkObject`s it spawns (zombies, WaveManager).
- Damage calls from any player reach zombies via `RPC_TakeDamage(RpcSources.All, RpcTargets.StateAuthority)`.

### Input flow
```
INetworkRunnerCallbacks.OnInput   ← GameLauncher collects Unity Input
  → PlayerInput struct
    → PlayerMovement.FixedUpdateNetwork  (movement + aim)
    → PlayerShooter.FixedUpdateNetwork   (fire / reload)
```

### Wave → Zombie spawn flow
```
WaveManager (Zombie Master)
  → StartNextWave()
  → SpawnWave() coroutine → Runner.Spawn(zombiePrefab)
  → ZombieController.FixedUpdateNetwork (AI, runs on Zombie Master)
  → ZombieHealth.RPC_TakeDamage (called by any player, applied on Zombie Master)
  → WaveManager.OnZombieDied() → decrement ZombiesRemaining, award score
```