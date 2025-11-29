# Lighthouse — Flow metrics, predictability & Monte Carlo forecasting

![Latest Release](https://img.shields.io/github/v/release/letpeoplework/lighthouse?sort=semver&display_name=release&label=latest&color=rgb(48%2C%2087%2C%2078)&link=https%3A%2F%2Fgithub.com%2FLetPeopleWork%2FLighthouse%2Freleases%2Flatest)

Lighthouse is a free, open-source forecasting and Flow metrics platform designed for Agile and Kanban teams. By applying Monte Carlo simulations to your historical throughput, Lighthouse helps teams predict delivery dates, understand predictability, and keep systems stable with actionable Flow Metrics.

Try the Community version — no account or credit card required; runs on your infrastructure and ships with a demo mode for exploration.

![Team Metrics Overview](docs/assets/features/overview.png)

## Why Lighthouse?

- Monte Carlo simulation-based forecasts (How Many, When) — probability-driven delivery estimates
- Built for Kanban: in-depth flow metrics (WIP, Cycle Time, Throughput, Work Item Age) and widgets for teams and projects
- Integrations with Jira, Azure DevOps, CSV import and Linear (preview) to keep your existing toolchain
- Dashboards and Predictability Score to turn data into decisions and keep systems stable
- Free & open-source (MIT); runs on your infrastructure

## Built for Kanban & Stability

Lighthouse is optimized for teams that want to keep a stable delivery system. It focuses on indicators teams use to maintain flow and reduce variability — key Kanban principles.

Key capabilities to keep systems stable:

- WIP monitoring and historical WIP charts — detect overload and make pull-based decisions
- Throughput and run charts — measure team capacity and detect trends
- Cycle Time (percentiles & scatterplots) — identify outliers and flow breakdowns
- Work Item Age and Work Item Aging charts — find stuck work early and reduce rework
- Simplified CFD and Work Distribution — visualize state changes and where effort is spent
- Predictability Score — a single-number indicator measuring how stable your throughput is

## How teams use Lighthouse

- Keep WIP within limits and watch aging items so flow remains predictable
- Use Monte Carlo forecasts to discuss trade-offs (e.g., delivery vs. scope) and make probability-driven decisions
- Establish SLEs and targets using Cycle Time & Predictability metrics
- Run experiments — e.g., reduce batch size or enforce pull policies — and measure the impact

## Quick links

- Product: https://letpeople.work/lighthouse
- Docs: https://docs.lighthouse.letpeople.work (installation, features, configuration)
- Releases & container images: https://github.com/LetPeopleWork/Lighthouse/releases
- Slack: https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A
- Email: lighthouse@letpeople.work

## Quick Start

Docker (recommended):

```bash
docker pull ghcr.io/letpeoplework/lighthouse:latest
docker run -d -p 8081:443 -p 8080:80 -v ".:/app/Data" -v "./logs:/app/logs" -e "Database__ConnectionString=Data Source=/app/Data/LighthouseAppContext.db" ghcr.io/letpeoplework/lighthouse:latest
```

## Standard Installation (binary packages)

1. Download the platform-specific package from: https://github.com/LetPeopleWork/Lighthouse/releases/latest
2. Extract and run; `Lighthouse.exe` on Windows, `./Lighthouse` on Linux/macOS.

## Developer: Build & run from source

1. Clone the repo and build the backend and frontend locally

```bash
git clone https://github.com/LetPeopleWork/Lighthouse.git
cd Lighthouse
# Back-end: build and run
dotnet build Lighthouse.sln
dotnet run --project Lighthouse.Backend/Lighthouse.Backend/Lighthouse.Backend.csproj

# Front-end: inside Lighthouse.Frontend
cd Lighthouse.Frontend
pnpm install
pnpm run dev      # connect to the local backend or set VITE_API_BASE_URL
pnpm run dev-demo # run UI with demo/mock data
```

## Core features

- Monte Carlo forecasting (How Many / When)
- Flow metrics dashboards and widgets: WIP, Throughput, Cycle Time, Work Item Age, Feature Size
- Predictability Score (throughput percentiles) and SLEs
- Project & Team forecasts that respect backlog ordering and Feature WIP
- Integrations: Jira, Azure DevOps, CSV import, Linear (preview)
- Demo UI and docker images for rapid onboarding

## Screenshots & Examples

Core dashboards and widget screenshots are available in the docs:
- https://docs.lighthouse.letpeople.work/metrics/widgets.html

## Support, Contribution & Roadmap

- Join Slack for community support: https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A
- Open issues and feature requests on GitHub: https://github.com/LetPeopleWork/Lighthouse/issues
- Contribute code and docs: https://docs.lighthouse.letpeople.work/contributions/contributions.html

## License & Security

Lighthouse is MIT licensed — see the LICENSE file. The product runs fully on your infrastructure; data is not sent to third-party cloud providers by default.

## Known limitations

- Lighthouse does not model explicit dependencies between work items (backlog order and team focus determines forecasts).
- If several teams work on the same feature, probability calculations may be conservative (current approach shows later estimates per-silo).
- The Free Community edition imposes limits on the number of projects/teams; see docs for details.

Want more influence on your Kanban process? Use Lighthouse with Slack and let people work on improvements to WIP, SLEs, and cycle time reductions — we provide the tooling and the expertise.

---
