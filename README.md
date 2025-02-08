# Lighthouse
![Latest Release](https://img.shields.io/github/v/release/letpeoplework/lighthouse?sort=semver&display_name=release&label=latest&color=rgb(48%2C%2087%2C%2078)&link=https%3A%2F%2Fgithub.com%2FLetPeopleWork%2FLighthouse%2Freleases%2Flatest)

![CI Workflow](https://github.com/letpeoplework/Lighthouse/actions/workflows/ci.yml/badge.svg) [![Frontend Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse_Frontend&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse_Frontend) [![Backend Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse)

Lighthouse is a tool that helps you run probabilistic forecasts using Monte Carlo Simulations in a continuous and simple way.
It connects to your Work Tracking Tool (currently Jira and Azure DevOps are supported) and will automatically update your team's Throughput and your project's forecasted delivery dates.

You can use it with a single team for doing manual "When" and "How Many" forecasts, as well as for tracking projects with one or multiple teams.

Lighthouse is provided free of charge as open-source software by [LetPeopleWork](https://letpeople.work). If you want to learn more about the tool, what we can offer you and your company, or just want to chat, please reach out.

# Documentation
You can find the documentation including how to download and get started in the dedicated documentation page: [https://docs.lighthouse.letpeople.work](https://docs.lighthouse.letpeople.work)

# Questions & Problems
If the [Documentation](#documentation) is not answering your question, you can get support in our [Slack Channel](https://join.slack.com/t/let-people-work/shared_invite/zt-2y0zfim85-qhbgt8N0yw90G1P~JWXvlg).

# Contribution
See our [Contributions Page](https://docs.lighthouse.letpeople.work/contributions/contributions.html) for more details on how you can contribute.

# Build and Run from Sources
To build and run the sources locally, follow these instructions.

Lighthouse is built with Aspnet Core WebAPI as a backend and a React frontend.

## Prerequisites
Make sure that you have:
- [Latest AspNet Core SDK](https://dotnet.microsoft.com/en-us/download/dotnet/latest)
- [Node](https://nodejs.org/en)

## Backend
After cloning the sources, you find the *Lighthouse.sln* solution in the root folder. Open it in Visual Studio and you can build and run it locally. Once it's running, you can hit the endpoints at the exposed ports.

## Frontend
The frontend is using [Vite](https://vitejs.dev/) as development server. After cloning, you find the folder *Lighthouse.Frontend* in your root directory. Inside this folder you find a node project. First install the dependencies:
```
npm install
```

After you have installed all the dependencies you can run the frontend in various ways.

### Start Frontend Using Mock Data
If you want to simply see the UI and not connect to a live backend, you can start up vite using a mock data service using the following command:
```
npm run dev-demo
```

This can be useful if you want to adjust the UI without having to star the backend (for example if you are designing something or refactoring).

### Start Frontend Connecting to Real Backend
If you want to test the end to end connection, you can run the following command:
```
npm run dev
```

This will run the frontend and set the backend url to `VITE_API_BASE_URL=http://localhost:5169/api` as defined in the *package.json* file. If you run your backend on a different port, adjust this accordingly.

### Run Tests
Tests are run using vitest, you can run all the tests using `npm tests`.