# Product brief: bug\-6697\-zero\-size\-features\-on\-teams

## Request

```text
Features whose default size works out to 0 still get a 0/0 work row for every team (or the Owning Team / Feature Owner team), so they show on each team's Features tab although no team has work for them. They should not appear on any team; they should still appear on the Portfolio itself.
```

## Work

S

```text
A team's Features tab lists only Features that carry work for that team
```

### Rework

None.

## Promises

### P1

Outcome

```text
A Feature whose default size works out to 0 is not shown on any Team, and still shows on its Portfolio
```

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
After a Portfolio refresh, an open Feature without child Work Items whose size works out to 0 (no estimate, default Feature size 0, default sizing on) carries no work row for any Team (not for the fallback teams, not for the Portfolio's Owning Team, not for teams named in the Feature Owner field), so it is not listed on any Team's Features; it is still marked as sized by the default and still belongs to the Portfolio
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.
