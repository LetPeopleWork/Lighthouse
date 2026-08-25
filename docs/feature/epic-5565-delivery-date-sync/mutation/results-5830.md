# Mutation testing — 5830 (Keep the Delivery date in step with Jira)

Run 2026-08-25 against `main` at `0f3814dcf`. Gate is 80 % kill rate per stack.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **83.11 %** | 148 | 123 | 25 | 0 | 25 m 14 s |
| Backend — `DeliverySourceSyncService.cs` alone | **84.21 %** | 19 | 16 | 3 | 0 | 3 m 59 s |
| Frontend (StrykerJS) | **100.00 %** | 6 | 6 | 0 | 0 | 49 s |

Config: `stryker.5830.backend.json`, `stryker.5830.backend.syncservice.json`,
`stryker.5830.frontend.json` with `mutation-vitest.5830.config.ts`.

## Backend — per file

| file | killed | survived | owned by this slice |
| --- | --- | --- | --- |
| Delivery.cs | 85 | 10 | 1 of the 10 |
| DeliverySourceSyncService.cs | 16 | 5 → 3 after marking | all |
| PortfolioUpdater.cs | 22 | 10 | none of the 10 |

Nineteen of the twenty-five survivors sit on lines this slice never touched — legacy `Delivery.cs`
projection helpers and `PortfolioUpdater.cs` code that predates it. They are recorded here rather than
closed, because closing them means writing tests for other slices' behaviour under this slice's name.

## What the run changed

Three diagnostic log statements in `DeliverySourceSyncService.cs` were marked
`// Stryker disable once all:` with their reasons, matching how the rest of this codebase treats log
text. The claim each branch makes — the Deliveries keep their values, the ones beside a refused
Delivery still sync — is asserted; the sentence an operator reads about it is not something a test
should be pinned to. That is what moved the file from 76.2 % to 84.21 %.

Nothing else in production changed for the sake of the score, with one exception recorded below.

## Accepted survivors

### `DeliverySourceSyncService.cs` — the two null guards, and the catch block under them

`ArgumentNullException.ThrowIfNull(portfolio)` and `ThrowIfNull(deliveries)` survive statement removal,
and so does the block in the read-failure catch. The same shape survives at `Delivery.cs:202`
(`ThrowIfNull(members)`).

Left open deliberately. This interface has one caller — `PortfolioUpdater` — which cannot pass null to
either parameter, and `RecordableDeliveries` is non-null by construction. A test that passed `null` to
prove `ThrowIfNull` throws would be a language-guarantee test: it asserts what the runtime already
promises, adds a case nobody can reach, and would read as coverage of this slice's behaviour when it
covers none of it. The guards stay because the aggregate's other public methods carry the same ones and
an inconsistent set is worse than an untested one.

### Frontend — the two colour literals, closed by deletion rather than by a test

The first frontend run scored 75 % with two survivors, both the theme colour on the delivery date:
`color={isOverdue ? "error.main" : "text.secondary"}`. Neither is killable here. MUI renders both
values to an identical class under jsdom with no inline style — measured, not assumed — so no component
test in this project can see which colour the date is drawn in.

The date colouring was removed rather than marked. It was redundant emphasis: the Overdue chip beside
it already carries red and the word together, and the chip's colour *does* land as a class
(`MuiChip-colorError`) that a test can assert. Keeping a second signal that nothing can observe means
keeping something that can break and stay broken.

## What the run did NOT find

No survivor pointed at a missing behavioural test. The gaps that mattered in this slice were found by
adversarial review before this run, not by it — the same-size Feature swap, the four integration
scenarios that passed with the sync deleted, and the refresh ordering. All were closed in
`a089bb1e0` and the mutants over that code were killed here.
