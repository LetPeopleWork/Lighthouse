# Measurement harness (dev-only, never in CI)

Linux only: it reads the backend's cgroup `cpu.stat` and attaches `dotnet-counters`.

## Prerequisites

- `dotnet tool install -g dotnet-counters` (the harness looks in `~/.dotnet/tools`, or set `PERF_COUNTERS`).
- A built backend: `dotnet build Lighthouse.Backend/Lighthouse.Backend/Lighthouse.Backend.csproj`.
- A seeded instance (`scaled-set.spec.ts`, see its header). The harness starts and stops its own backend on
  `PERF_PORT` and works on a copy of a SQLite database, so the seed stays reusable.

## Run

```sh
PERF_SEEDED_DB=$PWD/perf/out/instances/sqlite-main-default/lighthouse.db pnpm perf readings
```

One run writes `perf/out/runs/<time>-<provider>-<version>/results.md` (and `results.json`, traces, logs,
counters). Nothing else is written.

| Variable                                  | Default                                        | Meaning                                                                                  |
| ----------------------------------------- | ---------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `PERF_PROVIDER`                           | `sqlite`                                       | `sqlite` or `postgres`                                                                   |
| `PERF_SEEDED_DB`                          | —                                              | seeded SQLite file (copied per run)                                                      |
| `PERF_CONNECTION_STRING`                  | —                                              | PostgreSQL connection string (used as is)                                                |
| `PERF_KEYS_DIR`                           | `keys/` beside the seeded file                 | key ring the seed was written with (copied)                                              |
| `PERF_VERSION`                            | `main`                                         | label for the build under test                                                           |
| `PERF_BACKEND_DIR`                        | this checkout's backend                        | content root (a worktree at the tag for the old build)                                   |
| `PERF_BACKEND_EXECUTABLE`                 | `<dir>/bin/Debug/net10.0/linux-x64/Lighthouse` | the build to start                                                                       |
| `PERF_PORT`                               | `5421`                                         | HTTP port; HTTPS is port + 1000                                                          |
| `PERF_SCALE` (or `PERF_SCALE_POINTS`)     | `100,400,920`                                  | each names the Portfolio holding exactly that many Features; one value restricts the run |
| `PERF_STATES`                             | `idle,computed,skipped`                        | empty skips the opens                                                                    |
| `PERF_OPENS`                              | `overview,settings,portfolio,team`             |                                                                                          |
| `PERF_TEAM`                               | the Team with most Features                    | Team for the Team tab                                                                    |
| `PERF_REPEATS` (or `PERF_REPETITIONS`)    | `5`                                            | measured opens per open and state                                                        |
| `PERF_WARMUPS`                            | `1`                                            | unmeasured opens before them                                                             |
| `PERF_WINDOWS`                            | `T,R,RT,C,C0`                                  | empty skips the windows                                                                  |
| `PERF_WINDOW_SECONDS`                     | `300`                                          | length of each window                                                                    |
| `PERF_WINDOW_SCALE`                       | largest scale point                            | Portfolio the windows' rounds and opens use                                              |
| `PERF_MIN_THREADS`                        | `64`                                           | pool minimum for C and C0                                                                |
| `PERF_PROCESSOR_COUNT`                    | `4`                                            | `DOTNET_PROCESSOR_COUNT` of the backend                                                  |
| `PERF_LAUNCH_PREFIX`                      | none                                           | e.g. `taskset -c 0-3` for a hard core limit                                              |
| `PERF_MAX_LOAD`, `PERF_LOAD_WAIT_SECONDS` | `6`, `600`                                     | wait for a quiet machine before each block                                               |
| `PERF_ROUND_DRAIN_SECONDS`                | `900`                                          | how long to let a running round finish                                                   |
| `PERF_FILLERS`                            | off                                            | recorded only: set it to what the seed was built with                                    |

PostgreSQL: start a throwaway `postgres:18-alpine` with `--rm` on a spare port, seed it, then pass
`PERF_PROVIDER=postgres PERF_CONNECTION_STRING=...`. Rounds change the database, so reseed between runs that
must start alike.
