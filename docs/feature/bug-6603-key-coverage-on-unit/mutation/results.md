# Mutation testing — 6603 (a limited key reaches no further than its owner's role on that unit)

Run 2026-11-10 against `main` @ `f28c0091e`, Stryker.NET 4.16.0 under the x64 SDK. Gate is 80 % on the lines this
fix changed. Config: `stryker.6603.backend.json` (mutates `EffectiveAccess.cs`, runs `EffectiveAccessTest`).

| scope | tested | killed | survived | score |
| --- | --- | --- | --- | --- |
| Changed lines (`EffectiveAccess.cs` 100–101, 142–147) | 10 | 9 | 1 | **90.00 %** |
| Whole file, `EffectiveAccessTest` only | 59 | | | 79.41 % |

The whole-file figure runs only the value object's own tests, so mutants that the service and acceptance tests kill
elsewhere count against it here; the changed-line score is the gate.

## Accepted survivor

| Where | Mutation | Why it cannot be killed meaningfully |
| --- | --- | --- |
| `EffectiveAccess.cs:101` | `ownerGives > 0` to `ownerGives >= 0` | It only differs when the owner's role gives nothing on the unit and the key's own role gives nothing there either (a key row of a role foreign to that unit). Such a row is then kept, but it grants nothing on the unit, so no read or write answer changes. Equivalent. |

Frontend: N/A, because no frontend file changed.
