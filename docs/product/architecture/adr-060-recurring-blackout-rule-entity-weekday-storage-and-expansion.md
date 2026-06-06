# ADR-060: `RecurringBlackoutRule` entity — weekday set stored as a JSON-converted `List<DayOfWeek>`, with a pure interval-anchored expansion

## Status

Accepted — 2026-06-06 (DESIGN wave, feature `recurring-blackout-events`, ADO Epic 4577). Interaction mode = PROPOSE. Cross-references **ADR-059** (unified evaluation via materialization — this entity's expansion feeds it) and **ADR-058** (#4974 shift the expanded days flow into).

## Context

Locked decision **D3** fixes v1 recurrence to: a **weekday set** + an **every-X-weeks interval** + a **concrete start date** + an **optional open-ended end** (empty = forever). Monthly / nth-weekday-of-month / broader RRULE is explicitly OUT. The entity must mirror the shipped one-off `BlackoutPeriod` stack (model + DTO + service + controller + repository) per **D2** (extend existing patterns), and its days must materialize into `BlackoutPeriod` instances for ADR-059's union.

Two sub-decisions: (1) how to store the weekday set in EF; (2) the expansion algorithm + its anchoring rule + its home.

### Grounding reads

- `BlackoutPeriod` is `{ int Id; DateOnly Start; DateOnly End; string Description; }` implementing `IEntity`, keyed `ValueGeneratedOnAdd`, persisted via `DbSet<BlackoutPeriod>`. `DateOnly` is already used directly as an EF property type here — no converter needed for dates.
- `LighthouseAppContext` already has the **collection-as-JSON-string value-converter precedent**: `Team.StateMappings` / `Portfolio.StateMappings` are `List<StateMapping>` persisted via a `ValueConverter<List<StateMapping>, string>` (JSON serialize) **plus a `ValueComparer`** (required so EF change-tracking sees mutations) — `LighthouseAppContext.cs:332-354`. Also a `Dictionary<int,string?>` converter at `:280`.
- The one-off `BlackoutPeriodService.ValidateDateRange` throws `ArgumentException("Start date must be on or before end date.")`; the controller maps `ArgumentException → 400`, `KeyNotFoundException → 404`. This is the validation/error-mapping pattern to mirror.

## Decision

### 1. Entity shape

```csharp
public class RecurringBlackoutRule : IEntity
{
    public int Id { get; set; }
    public List<DayOfWeek> Weekdays { get; set; } = [];   // ≥1 entry; the days the rule repeats on
    public int IntervalWeeks { get; set; } = 1;            // ≥1; "every X weeks"
    public DateOnly Start { get; set; }                    // concrete anchor (also the interval anchor week)
    public DateOnly? End { get; set; }                     // null = open-ended (forever)
    public string Description { get; set; } = string.Empty;
}
```

`Weekdays` is the **domain-meaningful** representation (a set of `DayOfWeek`). It is mutable `List<DayOfWeek>` only because EF + the StateMappings converter precedent require a settable collection property for change-tracking; the service layer treats it as a set (de-dups, requires ≥1). `IntervalWeeks` defaults to 1 (D3: interval 1 reproduces plain weekly, US-02 AC4). `End` nullable `DateOnly?` is the open-ended flag — no sentinel date.

### 2. Weekday-set storage — JSON-converted `List<DayOfWeek>` + `ValueComparer` (chosen)

Persist `Weekdays` exactly as `StateMappings` is persisted: a `ValueConverter<List<DayOfWeek>, string>` (`JsonSerializer.Serialize`/`Deserialize`) with a paired `ValueComparer<List<DayOfWeek>>`, registered in `LighthouseAppContext.OnModelCreating` beside the existing `BlackoutPeriod` key config. `DayOfWeek` serializes as its int (Sun=0…Sat=6) — stable, culture-independent. `Start`/`End` are stored as native `DateOnly`/`DateOnly?` (no converter — the provider already maps `DateOnly` for `BlackoutPeriod`).

**Alternatives considered for weekday storage:**

- **A `[Flags] enum Weekdays` (bitmask, one int column).** Compact, no converter, trivially comparable. **Rejected as the primary** because it diverges from the codebase's established collection-persistence idiom (the StateMappings JSON-converter pattern), is less self-describing in the DB, and `DayOfWeek` is the natural domain type the DTO/expansion already speak — a bitmask forces a translation layer at the boundary. (Viable; would be the choice in a codebase without the JSON-converter precedent.)
- **A delimited string with a bespoke converter** (e.g. `"1,2,5"`). Functionally identical to JSON but **reinvents** serialization the StateMappings converter already standardizes; JSON via the existing pattern is DRY and proven (including the `ValueComparer` gotcha that a naive string converter would re-trip). **Rejected.**
- **A child table `RecurringBlackoutRuleWeekday` (one row per weekday).** Normalized, queryable. **Rejected as over-normalized** for a ≤7-element value set that is always read whole with its parent and never queried independently — it adds a join, a second entity, and migration weight for no query benefit. The set is a *value object*, not an entity.

Chosen: **JSON-converted `List<DayOfWeek>` + `ValueComparer`**, matching `Team.StateMappings`. (CI note: the `ValueComparer` is **mandatory** — omitting it makes EF miss in-place mutations of the list, the exact trap the StateMappings precedent already solved. Match that block.)

### 3. Expansion — a pure function, anchoring pinned

```
RecurringBlackoutRuleExtensions.ExpandToBlackoutDays(
    this RecurringBlackoutRule rule, DateOnly windowStart, DateOnly windowEnd)
    → IEnumerable<BlackoutPeriod>      // one single-day BlackoutPeriod { Start = End = d } per matching day
```

A day `d` matches the rule **iff** all hold:

1. **In the bounded window of the rule itself**: `d >= rule.Start` AND (`rule.End is null` OR `d <= rule.End`).
2. **In the consumer's evaluation window**: `windowStart <= d <= windowEnd` (the iteration range; an open-ended rule is bounded here, never expanded to infinity).
3. **Weekday match**: `rule.Weekdays.Contains(d.DayOfWeek)`.
4. **Interval anchoring**: the ISO-week distance from the rule's start week to `d`'s week is a whole multiple of `IntervalWeeks`. Precisely — anchor on the **Monday of `rule.Start`'s week** (`anchorMonday = rule.Start.AddDays(-(int)((rule.Start.DayOfWeek + 6) % 7))`, i.e. ISO Monday=start-of-week), and the **Monday of `d`'s week** (`dMonday`); the week index is `weeksBetween = (dMonday - anchorMonday).Days / 7`; the day matches the interval iff `weeksBetween % rule.IntervalWeeks == 0` (and `weeksBetween >= 0`).

**Worked check against US-02 AC1** (Fri, every 4 weeks, start 2026-06-12):
- `rule.Start = 2026-06-12` (a Friday). Its week's Monday = `2026-06-08`.
- `2026-06-12` (Fri): same week, `weeksBetween = 0`, `0 % 4 == 0` ✓ → blackout.
- `2026-06-19` (off-week Fri): Monday `2026-06-15`, `weeksBetween = 1`, `1 % 4 != 0` ✗ → working day (AC1 "NOT 06-19").
- `2026-07-10` (Fri): Monday `2026-07-06`, `weeksBetween = 4`, `4 % 4 == 0` ✓ → blackout.
- `2026-08-07` (Fri): Monday `2026-08-03`, `weeksBetween = 8`, `8 % 4 == 0` ✓ → blackout.
- `2026-06-05` (before start) → fails rule (1); `2027-01-08` (after `end = 2026-12-31`) → fails rule (1) → both working days (US-02 AC2). ✓
- **Interval 1 reproduces plain weekly** (US-02 AC4): `weeksBetween % 1 == 0` always true ⇒ every selected weekday in `[Start, End]∩window` matches ⇒ identical to US-01's Sat+Sun-forever behaviour. ✓

The `Description` of each synthetic `BlackoutPeriod` is the rule's human-readable summary (so chart overlays / debugging show provenance), but downstream evaluation never reads `Description` — D4 indistinguishability holds regardless.

### 4. Home of the expansion

`ExpandToBlackoutDays` is an **extension method on `RecurringBlackoutRule`** in a new `RecurringBlackoutRuleExtensions` static class — mirroring `BlackoutDaysExtensions` as the established "pure blackout math lives in a static extensions class" home (D7 spirit, ADR-058 Option-C stance). It is **not** a method on `BlackoutDaysExtensions` (that class is the *one-off-period* math over `IEnumerable<BlackoutPeriod>`; keeping recurring-expansion in its own file preserves the single-responsibility split and lets ArchUnitNET pin each as pure independently). It is **not** an instance method on the entity that does I/O (the entity stays a persistence projection with no service/repo dependency — Models ↛ Repositories, ArchUnitNET-guarded). The clock is a passed-in `windowStart`/`windowEnd`; the function is pure.

### 5. DTO + validation

`RecurringBlackoutRuleDto { int? Id; List<DayOfWeek> Weekdays; int IntervalWeeks; DateOnly Start; DateOnly? End; string Description; }` with `[JsonRequired]` on the non-optional fields (matching `BlackoutPeriodDto`'s `[JsonRequired]` on `Start`/`End`). `RecurringBlackoutRuleService.Validate(dto)` throws `ArgumentException` (→ 400 in the controller, mirroring `BlackoutPeriodsController`) when:

- `Weekdays` is empty → `"Select at least one weekday for the rule to repeat on."` (US-04 AC3)
- `IntervalWeeks < 1` → `"Repeat interval must be at least 1 week."` (US-04 AC4)
- `End is not null && End < Start` → `"End date must be on or after the start date."` (US-04 AC5; parallels the one-off `ValidateDateRange` message style)

## Consequences

**Positive**
- Entity mirrors `BlackoutPeriod` 1:1 in shape and stack (D2); the weekday set reuses the proven StateMappings converter+comparer idiom (no new persistence technique).
- Expansion is one pure, mutation-testable function; the anchoring rule is pinned by a worked example matching every US-02 AC; interval-1 = weekly is a property of `% 1`, not a special case (no regression risk, US-02 AC4).
- Open-ended rules are safe by construction — expansion is always bounded by the consumer's window (ADR-059), never infinite.
- Validation reuses the one-off `ArgumentException → 400` mapping; messages match the journey error paths verbatim.

**Negative / accepted**
- A new EF migration is required (the new table). Generate via the existing **`CreateMigration` PowerShell script** (CLAUDE.md — never `dotnet ef migrations add` directly), across all providers. New-table-only; no data migration (existing one-off weekend periods are NOT auto-converted — out of scope).
- `Weekdays` is a mutable `List<DayOfWeek>` (EF change-tracking requirement). The service treats it as a set on the boundary (de-dup, ≥1). The general immutability convention (CLAUDE.md) yields here to the same EF constraint the StateMappings property already lives under.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `RecurringBlackoutRule` (Models) depends on NO repository/service | ArchUnitNET: `Models.*` ↛ `Services.Interfaces.Repositories`/`Services.Interfaces` (extends the existing suite; same rule that guards `BlackoutPeriod`/`Feature`/`Delivery`) |
| `ExpandToBlackoutDays` is pure (no `IRepository<>`, `DbContext`, `HttpClient`, `ILogger`, `DateTime.UtcNow`/`Today`) | NUnit static-inspection test |
| Anchoring correctness (US-02 AC1: 06-12/07-10/08-07 match, 06-19 does not; AC2 out-of-window excluded; AC4 interval-1 ≡ weekly) | NUnit table tests against the worked examples |
| Weekday set persists + round-trips (incl. in-place mutation seen by EF) | NUnit InMemory + a provider integration test; the `ValueComparer` is asserted present (the StateMappings trap) |
| Validation messages match the journey error paths verbatim | NUnit: each invalid DTO yields the exact message string |

## Premium-gating & clients verdict

Same as ADR-059: writes gated Premium + SystemAdmin, GET open, no new permission; new endpoint family ⇒ version-gated client method **if** clients wrap blackout config, else deferred-and-recorded. Evaluation path inherits no premium gate.
