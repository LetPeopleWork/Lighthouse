# Lighthouse
![Latest Release](https://img.shields.io/github/v/release/letpeoplework/lighthouse?sort=semver&display_name=release&label=latest&color=rgb(48%2C%2087%2C%2078)&link=https%3A%2F%2Fgithub.com%2FLetPeopleWork%2FLighthouse%2Freleases%2Flatest)

![CI Workflow](https://github.com/letpeoplework/Lighthouse/actions/workflows/ci.yml/badge.svg) [![Frontend Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse_Frontend&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse_Frontend) [![Backend Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=LetPeopleWork_Lighthouse&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=LetPeopleWork_Lighthouse)

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

### Installation/Update Scripts
In [Scripts](https://github.com/LetPeopleWork/Lighthouse/tree/main/Scripts) you can find 3 scripts to download the latest version of Lighthouse for [Linux](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_linux.sh), [Mac](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_mac.sh) and [Windows](https://github.com/LetPeopleWork/Lighthouse/blob/main/Scripts/update_windows.ps1).

The scripts will download the latest version in the folder they are executed from and will replace all existing files in the folder.
**Important:** The database will not be replaced and the new version will work against the same database.

### Start Lighthouse
Once extracted, you can run the the `Lighthouse` application (for example: `Lighthouse.exe` on Windows). A terminal will open and you should see a window similar to this:

![Starting Lighthouse](https://github.com/user-attachments/assets/9bd034a9-0b5d-48fe-897f-3cc749402b24)

By default, Lighthouse will start running on the system on port 5001. If everything worked as expected, you can open the app now in your browser via [https://localhost:5001](https://localhost:5001).
You should see the (empty) landing page:
![Landing Page](https://github.com/user-attachments/assets/06cf29cd-d9a8-4a93-84aa-2335747d8699)

## Configuration Parameters
Following are the configuration options for Lighthouse, together with their respective default values, description, and override options. For a more detailed description on how the values can be overriden, please see the next chapter.


| Name                | Description                                      | Default                     | Command Line                                      | Environment Variable                          |
|---------------------|--------------------------------------------------|-----------------------------|--------------------------------------------------|-----------------------------------------------|
| Http URL            | The URL for HTTP endpoint                        | http://*:5000               | --Kestrel:Endpoints:Http:Url     | Kestrel__Endpoints__Http__Url                |
| Https URL           | The URL for HTTPS endpoint                       | https://*:5001              | --Kestrel:Endpoints:Https:Url   | Kestrel__Endpoints__Https__Url               |
| Database            | The connection string for the database           | Data Source=LighthouseAppContext.db | --ConnectionStrings:LighthouseAppContext | ConnectionStrings__LighthouseAppContext      |
| Encryption Key      | The key used for encryption                      |                             | --Encryption:Key           | Encryption__Key                               |
| Certificate File    | The path to the SSL certificate file that shall be used for the secure connection             | certs/LighthouseCert.pfx    | --Certificate:Path | Certificate__Path         |
| Certificate Password| The password for the SSL certificate file        |                        | --Certificate:Password | Certificate__Password     |

### Http & Https URL
By default, Lighthouse will listen on Ports 5000 (http) and 5001 (https). You might want to override this, for example if you want to expose Lighthouse on the default ports (80/443), or need to adjust it to whatever makes sense in your environment.

#### Docker
If you're using docker, you can simply adjust the port bindings instead of overriding this value:
`docker run -p 80:5000 -p 443:5001 ghcr.io/letpeoplework/lighthouse:latest`

### Database
Lighthouse uses an [SQLite](https://www.sqlite.org/) database to store the data. The database is stored in a single file. By default, that file is next to the executable and called *LighthouseAppContext.db*. If you want to store it in a subfolder called *data* and name it *MyDatabase.db*, you can provide the following value: `Data Source=data/MyDatabase.db`. You can also specify an absolute pat: `Data Source=C:/data/MyDatabase.db`

**Note:** The folder you specify must exist - if it's not existing, the startup will fail.

#### Docker
On docker, you probably want to map the database to a file on your system, as otherwise you'll lose the data if your container gets removed. You can do so by specifying a volume and adjust the connection string to point to this volume:

`docker run -v ".:/app/Data" -e "ConnectionStrings__LighthouseAppContext=Data Source=/app/Data/LighthouseAppContext.db" ghcr.io/letpeoplework/lighthouse:latest`

This will create a volume in the local folder (`-v ".:/app/Data`) and then overwrites the configuration via environment variable to point to this volume (`-e "ConnectionStrings__LighthouseAppContext=Data Source=/app/Data/LighthouseAppContext.db"`). This will result in the file *LighthouseAppContext.db* to be created in the local folder where you run the container from.

### Encryption Key
In order to connect to Jira, Azure DevOps, etc., sensitive information (tokens) are needed. While we need to store them (as otherwise the continuous updating will not work), we don't want to keep those values in clear text. Sensitive data is encrypted using the encryption key that is specified. While there is a default key provided, you should adjust this to be unique for your setup.

Lighthouse will still start if you change your key, but the sensitive data will not be properly decrypted, meaning you have to reconfigure your work tracking systems to enable any updates.

You have to specify a base64 encoded key that is 32 bytes long. You can generate a new random key via https://generate.plus/en/base64.  
**Important:** Set the length to 32 as otherwise it will not work.

### Certificate
In order to run Lighthouse via secure https connection, we need to specify a certificate. There is a default certificate delivered with the app, however, this is not tailored for your environment and you must trust it first.

If you want to create a new certificate, you can do so via [OpenSSL](https://www.openssl.org/).
You can provide your own certificate and the respective password (if any), so that the Lighthouse can be trusted when exposed to your users. Assuming you have openssl installed, you can run the following commands which will guide you through the creation of a new "MyCustomCertificate.pfx" (during this process, openssl will ask you to provide certain information about - just follow along).

1. `openssl req -newkey rsa:2048 -nodes -keyout MyCustomCertificate.key -out request.csr`
2. `openssl x509 -req -days 365 -in request.csr -signkey MyCustomCertificate.key -out MyCustomCertificate.crt`
3. `openssl pkcs12 -export -out MyCustomCertificate.pfx -inkey MyCustomCertificate.key -in MyCustomCertificate.crt`

*Note:* In step 3, you will be asked to provide a password. If you do, you have to specify it as well to Lighthouse.

You know have a new file `MyCustomCertificate.pfx` that you could use instead of the default:
`Lighthouse.exe --Certificate:Path="MyCustomCertificate.pfx" --Certificate:Password="Password"`

If you then navigate to the Lighthouse URL, your browser might ask you to trust the certificate first. You can also inspect it and it should show the data you provided during the creation process.

#### Docker
To provide the custom certificate to you instance running in docker, you can map a volume and specify the path through that volume. In the following example, we assume that *MyCustomCertificate.pfx* is in the local folder: `docker run -v ".:/app/Data" -e "Certificate__Path=/app/Data/MyCustomCertificate.pfx" -e "Certificate__Password=Password" ghcr.io/letpeoplework/lighthouse:latest`

## Overriding Configuration Options
Lighthouse is using the file `appsettings.json` for it's configuration. You can either override/provide your own file, or use command line parameters or environment variables to override individual values.

For details about the values and commandline, respectively environment variables, check the table with the configuration options above.

### Environment Variables
You can use environment variables to override the default configuration options specified in the `appsettings.json` file. For this, simply set a variable with the value you'd like to have. As an example, if you want to adjust the _Https Endpoint Url_ to be listening on port **1886**, you can do so by setting the variable `Kestrel__Endpoints__Https__Url` to `https://*:1886`

Environment variables are the preferred way to provide configuration if you run Lighthouse via docker.

**Note:** You can read the required names for the environment variables in the table above.

### Commandline
Instead of environment variables, you can also specify parameters on startup. Simply pass your overrides on the commandline after a `--`.
`Lighthouse.exe --Kestrel:Endpoints:Https:Url="https://*:1886"`

**Note:** You can read the required names for the command line options in the table above.

### App Settings
The file `appsettings.json` can be opened in any text editor, it's a simple text based file in the json format. You can adjust the settings as you like.  

**Note:** Please only use this option if you know what you're doing. If you don't provide a valid json file, Lighthouse will not start. Using commandline parameters or environment variables is recommended over adjusting directly in the appsettings.json.

## Questions & Problems
The documentation for Lighthouse is built into the application itself. On most pages you will find a "?" help icon in the upper right corner that should guide you through the usage.

If you struggle with something, have an open question, or would like to report a problem, please don't hesitate to open an issue on [github](https://github.com/LetPeopleWork/Lighthouse/issues).

### Logs
You can find the logs in the Lighthouse Settings in the *Logs* tab. There you can search them, as well as donwload the latest one. On the host machine, they are also stored in the *logs* folder next to the executable. If you run it via docker, you can see the logs in the standard output. If you like the log files directly, you can map a volume to the logs folder when running docker: `docker run -v "%cd%/logs:/app/logs" ghcr.io/letpeoplework/lighthouse:latest`

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
