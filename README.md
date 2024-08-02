# Lighthouse ![CI Workflow](https://github.com/letpeoplework/Lighthouse/actions/workflows/ci.yml/badge.svg) [![Frontend Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse_Frontend&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse_Frontend) [![Backend Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse)

Lighthouse is a tool that helps you run probabilistic forecasts using Monte Carlo Simulations in a continuous and simple way.
It connects to your Work Tracking Tool (currently Jira and Azure DevOps are supported) and will automatically update your team's Throughput and your project's forecasted delivery dates.

You can use it with a single team for doing manual "When" and "How Many" forecasts, as well as for tracking projects with one or multiple teams.

Lighthouse is provided free of charge as open-source software by [LetPeopleWork](https://letpeople.work). If you want to learn more about the tool, what we can offer you and your company, or just want to chat, please reach out.

### Lighthouse.Frontend
[![Frontend Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse_Frontend&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse_Frontend) [![Frontend Coverage](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse_Frontend&metric=coverage)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse_Frontend) [![Frontend Code Smells](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse_Frontend&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse_Frontend) [![Frontend Bugs](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse_Frontend&metric=bugs)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse_Frontend)

### Lighthouse.Backend
[![Backend Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse) [![Backend Coverage](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse&metric=coverage)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse) [![Backend Code Smells](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse) [![Backend Bugs](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse&metric=bugs)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse)


# Installing and Configuring Lighthouse
Lighthouse is a web application, that is foreseen to run on a server where multiple people have access to it. You can however run it also on your local machine. This might be the preferred option for now, as there is no User Management, nor any authentication/authorization at this point.

## Docker
The easiest way to run Lighthouse is to use docker. You can either use the `latest` image which reflects the latest version (which might include new features, but might also be not that stable yet), or use one with a specific tag. Check out the [packages](https://github.com/orgs/LetPeopleWork/packages?repo_name=Lighthouse) section to see the images.

You can run Lighthouse in docker using the following command:
`docker run -d -P -v ".:/app/Data" -v "./logs:/app/logs" -e "ConnectionStrings__LighthouseAppContext=Data Source=/app/Data/LighthouseAppContext.db" ghcr.io/letpeoplework/lighthouse:latest`

This will use the directory you run the command from as storage for your database and logs.

## Regular Installation

### Prerequisites
The packages provided by Lighthouse have everything included you need to run it, so there are no prerequisites.

Lighthouse runs on Windows, MacOs, and Linux based systems.

### Download Lighthouse
Download the latest version of Lighthouse for your operating system from the [Releases](https://github.com/LetPeopleWork/Lighthouse/releases/latest).
Download the zip file, and extract it to the location you want to run the application from.

### Start Lighthouse
Once extracted, you can run the the `Lighthouse` application (for example: `Lighthouse.exe` on Windows). A terminal will open and you should see a window similar to this:

![Starting Lighthouse](https://github.com/user-attachments/assets/9bd034a9-0b5d-48fe-897f-3cc749402b24)

By default, Lighthouse will start running on the system on port 5000. If everything worked as expected, you can open the app now in your browser via [http://localhost:5000](http://localhost:5000).
You should see the (empty) landing page:
![Landing Page](https://github.com/user-attachments/assets/06cf29cd-d9a8-4a93-84aa-2335747d8699)

#### Running Lighthouse on a different Port
If you want to run Lighthouse on a different port, you can do so using the following approaches:

##### URLs Parameter
You can specify the urls the application should listen on during startup. You can also specify multiple URLs, separated by a *;*: 

`Lighthouse.exe --urls "http://0.0.0.0:80;https://0.0.0.0:443"`

##### Setting ASPNETCORE_URLS
You can set the environment variable *ASPNETCORE_URLS* and then it will automatically be picked up by Lighthouse:

```
set ASPNETCORE_URLS=https://0.0.0.0:443
Lighthouse.exe
```

## Questions & Problems
The documentation for Lighthouse is built into the application itself. On most pages you will find a "?" help icon in the upper right corner that should guide you through the usage.

If you struggle with something, have an open question, or would like to report a problem, please don't hesitate to open an issue on [github](https://github.com/LetPeopleWork/Lighthouse/issues).

# Contribution
See [Contribution](./CONTRIBUTING.md) for more details on how you can contribute.

# Running Locally
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
npm run dev-mockdata
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

### Lint
*eslint* is used for linting. You can run it via `npm run lint`.
