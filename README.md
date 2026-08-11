# Fusion Obstacle Course

A small online multiplayer obstacle-course racer built with **Unity** and **Photon Fusion 2**, using Fusion's **Shared Mode** topology. Two players race through a course of moving hazards, collapsing platforms and blinking walkways — first to the flag wins.

> Originally built as a university networking assignment, then cleaned up and documented as a portfolio piece. It is a **prototype**: the art is placeholder primitives and the scope is one level. The interesting part is the networking.

<!-- Next up, in priority order:
     1. A gameplay GIF right here — the single highest-value thing this README is missing.
        Record two clients side by side so the state sync is visible.
     2. A WebGL build on itch.io, then restore the "Play in the browser" line below:
        **▶ Play it in the browser:** <link>
-->

**Status:** playable prototype, runs against Photon Cloud. Browser build in progress.

---

## Networking model

The project runs on **Fusion Shared Mode**, where there is no authoritative host: every client spawns its own player object and holds **state authority** over it. Photon Cloud relays state between peers.

| Concern | How it is handled |
|---|---|
| Player spawning | Each client spawns its own player in `IPlayerJoined.PlayerJoined`, picking a spawn point by join order |
| Player movement | Simulated locally in `FixedUpdateNetwork`, replicated via `NetworkRigidbody3D` |
| Player colour | `[Networked]` property with `OnChangedRender` — late joiners get the current value automatically, no explicit sync message |
| Hazards & platforms | Simulated by the state authority only, replicated through `NetworkTransform` |
| Collapsing / blinking platforms | RPCs, because the state being mirrored is renderer and collider enablement rather than transform data |
| Race result | A single broadcast RPC; each client resolves it relative to its own player, so the same message renders as a win on one side and a loss on the other |

### Known trade-offs

These are deliberate prototype-scope decisions, not oversights:

- **Input is polled locally rather than routed through Fusion's `NetworkInput` pipeline.** In Shared Mode each client already owns its player, so this works — but it gives up prediction, reconciliation and any cheat resistance. A production build would move movement input into a `INetworkInput` struct.
- **The race-start flag is client-local.** `PlayerSpawner.GameStart` is derived independently on each client from the active player count instead of being a `[Networked]` property, because `PlayerSpawner` is a `SimulationBehaviour` rather than a `NetworkBehaviour`.
- **Designed around two players.** The session supports more and four spawn points are configured, but the pacing and the win condition assume a head-to-head race.
- **`CamPos` derives from `NetworkObject`** purely as a camera anchor marker. It should be a plain transform; changing it requires re-authoring the player prefab.

---

## Tech

- **Unity 2022.3.5f1** (LTS), Built-in Render Pipeline
- **Photon Fusion 2.0.1** (Shared Mode) + Fusion Physics addon
- **ParrelSync** for running two editor instances locally without building
- TextMeshPro for UI

---

## Running it locally

The Photon Fusion SDK is **not committed to this repository** — Photon's licence does not cover redistributing it, and the settings asset carries a private App ID. Two extra steps are needed after cloning:

1. **Install Unity 2022.3.5f1** with the **WebGL** module (only needed for browser builds).

2. **Import Photon Fusion 2.0.1.**
   Download the SDK from the [Photon dashboard](https://dashboard.photonengine.com/) and import it into the project. It must land at `Assets/Photon/`.

3. **Add your Fusion App ID.**
   Create a free Fusion application in the Photon dashboard, then paste its App ID into
   `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` (or via **Tools → Fusion → Fusion Hub**).

4. **Open `Assets/Scenes/SampleScene.unity` and press Play.**
   The scene uses Fusion's `FusionBootstrap` prototyping GUI — pick **Shared** and a room name to connect.

### Testing two players locally

The project ships with [ParrelSync](https://github.com/VeriorPies/ParrelSync). Use **ParrelSync → Clones Manager → Create new clone**, open the clone, and press Play in both editors. Both join the same room and the race starts once the second player connects.

### Headless builds

`Assets/Scripts/Editor/Builder.cs` exposes a build entry point for CI and the Unity CLI:

```bash
unity build . --target WebGL --execute-method Builder.BuildWebGL
```

WebGL compression is forced off in that path, because itch.io serves builds as plain static files and Unity's compressed loader fails there without custom headers.

---

## Controls

| Input | Action |
|---|---|
| `WASD` / arrow keys | Move |
| `Space` | Jump (with a short coyote-time grace window after leaving a ledge) |

---

## What I would do next

- Replace Fusion's prototyping bootstrap GUI with a real connect/lobby screen
- Move input into `NetworkInput` so movement is predicted and reconciled
- Make the race-start gate networked state instead of a per-client derivation
- More than one level, and art that is not grey primitives

---

## Licence

The project code is under the MIT licence (see `LICENSE`). Photon Fusion and ParrelSync are covered by their own licences and are not redistributed here.
