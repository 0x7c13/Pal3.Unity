# Battle core

This assembly is independent of Unity and original binary readers. It owns captured battle definitions, deterministic rules, authoritative state, fixed-clock execution, commands and replay.

| Directory | Responsibility |
| --- | --- |
| `Configuration` | Mutable authoring DTOs and deep-copied initial setup. |
| `Contracts` | Immutable actor/ability/equipment definitions and state snapshots. |
| `Runtime` | Commands, ordered inputs/events, replay and variant state. `Session` contains the one authoritative state writer, split by mechanism. |
| `Rules` | Verified original calculations and policy; AI selection is grouped under `AI`. |
| `Stage` | Logical body/movement/camera/effect protocols and scheduling. These timings determine Work callbacks and rule execution, so they stay in the core. |
| `Presentation` | Separate engine-independent assembly for semantic HUD intent, focus, selection and visual projections; references the battle kernel. |
| `Results` | Immutable results and the result/body presentation protocol. |

The resource adapter compiles metadata before play. The core never searches resources, polls devices or derives simulation time from rendering. UI queries must not advance state or consume RNG. Snapshots and replay inputs must preserve ownership and ordering.

`IBattleRules` currently customizes the small basic battle ruleset. Full original battles compose the verified mechanic rules; this is not yet an arbitrary rule-plugin interface. Extend an actual mechanic through definitions/commands and its explicit execution boundary, then update deterministic replay coverage. Replay always uses the current implementation with captured setup, definitions and inputs; it has no rules-version negotiation or legacy execution paths. Resource fingerprints identify the captured data.

`Pal3.Battle.Presentation` owns semantic HUD intent, focus, selection and menu state. Unity resource/rendering and editor interaction live in `Editor/BattleEditor`. The existing story-side `Pal3.Game/GameSystems/Battle` remains a separate placeholder until inventory, story outcome and save adapters are connected to this kernel.

`Presentation/` is physically nested here but owns its own asmdef, so Unity excludes it from the kernel assembly. A standalone SDK project must likewise exclude `Presentation/**/*.cs` from the kernel and build that folder as a separate project referencing the kernel.
