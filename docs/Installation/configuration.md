---
title: Configuration
layout: home
parent: Installation and Configuration
nav_order: 3
---

There are various options to configure Lighthouse, from the Database to the used ports. This section will show you **what** is configurable and how you configure it.

After you're done with the configuration, check out the [Concepts](../concepts/concepts.html) to see how Lighthouse works.

- TOC
{:toc}

# Overriding Configuration Options
Lighthouse is using the file `appsettings.json` for it's configuration. You can either override/provide your own file, or use command line parameters or environment variables to override individual values.

## Environment Variables
You can use environment variables to override the default configuration options specified in the `appsettings.json` file. For this, simply set a variable with the value you'd like to have. As an example, if you want to adjust the _Https Endpoint Url_ to be listening on port **1886**, you can do so by setting the variable `Kestrel__Endpoints__Https__Url` to `https://*:1886`

Environment variables are the preferred way to provide configuration if you run Lighthouse via docker.

## Commandline
Instead of environment variables, you can also specify parameters on startup. Simply pass your overrides on the commandline after a `--`.
```bash
Lighthouse.exe --Kestrel:Endpoints:Https:Url="https://*:1886"
```

## App Settings
The file `appsettings.json` can be opened in any text editor, it's a simple text based file in the json format. You can adjust the settings as you like.  

{: .note}
Please only use this option if you know what you're doing. If you don't provide a valid json file, Lighthouse will not start. Using commandline parameters or environment variables is recommended over adjusting directly in the appsettings.json.

# Configuration Options

## Http & Https URL
By default, Lighthouse will listen on Ports 5000 (http) and 5001 (https). You might want to override this, for example if you want to expose Lighthouse on the default ports (80/443), or need to adjust it to whatever makes sense in your environment.

**Default Values:**
- Http URL: `http://*:5000`
- Https URL: `https://*:5001`

**Override Options:**
- Command Line: 
  - `--Kestrel:Endpoints:Http:Url`
  - `--Kestrel:Endpoints:Https:Url`
- Environment Variables:
  - `Kestrel__Endpoints__Http__Url`
  - `Kestrel__Endpoints__Https__Url`

### Docker
If you're using docker, you can simply adjust the port bindings instead of overriding this value:
```bash
docker run -p 80:5000 -p 443:5001 ghcr.io/letpeoplework/lighthouse:latest
```

## Database
Lighthouse can work with different databases. Currently it supports [SQLite](https://www.sqlite.org/) and [postgresql](https://www.postgresql.org/) databases to store the data.

{: .recommendation}
More providers could be added upon request. Please reach out to us in such a case and we can discuss options.

As part of the configuration, you can specify the provider and the connection string.

**Default Values:** *sqlite* and *Data Source=LighthouseAppContext.db*

**Override Options:**
- Command Line: `--Database:Provider` and `--Database:ConnectionString`
- Environment Variable: `Database__Provider` and `Database__ConnectionString`

{: .recommendation}
If you just get started, and run Lighthouse on your local machine, we recommend to use **sqlite**. You don't need to install anything else or worry too much about setting up Database connections.  
We recommend using **Postgres** if you host Lighthouse for many users and the data stored for Lighthouse is increasing. Many cloud providers like AWS and Azure support hosting Postgres in their environments, so you can reuse their services and let Lighthouse store the data in a database hosted by those providers.

### SQLite
SQLite is the default provider. If used, the database is stored in a single file.

By default, that file is next to the executable and called *LighthouseAppContext.db*. If you want to store it in a subfolder called *data* and name it *MyDatabase.db*, you can provide the following value for the *Connection String*: `Data Source=data/MyDatabase.db`. You can also specify an absolute path: `Data Source=C:/data/MyDatabase.db`

{: .note}
The folder you specify must exist - if it's not existing, the startup will fail.

### Postgres [Experimental]
Postgres is an open-source, relational database. It's widely used and very powerful.  

{: .important}
Please be aware that Postgres support right now is still experimental. While it should work, we have not a ton of experience with it. Please reach out to us if you experience any issues with Postgres.

To configure Lighthouse to use postgres, set the *Database Provider* to *postgres*, an adjust the *Database ConnectionString* to a valid connection string for a postgres connection.

{: .note}
If you want to run Lighthouse with a connection to a Postgres database, you'll have to set up the database yourself. Please refer to the Postgres documentation on how to do this.


### Docker
Independent of which *provider* you are using, there are a few things to be aware of when running Lighthouse in Docker.

When using *sqlite*, you probably want to map the database to a file on your system, as otherwise you'll lose the data if your container gets removed. You can do so by specifying a volume and adjust the connection string to point to this volume:

```bash
docker run -v ".:/app/Data" -e "Database__ConnectionString=Data Source=/app/Data/LighthouseAppContext.db" ghcr.io/letpeoplework/lighthouse:latest
```

{: .note}
The environment variable `-e "Database__Provider=sqlite"` is omitted because it's the default value.

This will create a volume in the local folder (`-v ".:/app/Data`) and then overwrites the configuration via environment variable to point to this volume (`-e "Database__ConnectionString=Data Source=/app/Data/LighthouseAppContext.db"`). This will result in the file *LighthouseAppContext.db* to be created in the local folder where you run the container from. Check out the [docker sqlite example on GitHub](https://github.com/LetPeopleWork/Lighthouse/blob/main/examples/sqlite).

In a similar way, you can adjust the provider and connection string postgres. If you want to host your postgres database as well in a docker container, we provide a [docker-compose.yml](https://github.com/LetPeopleWork/Lighthouse/blob/main/examples/postgres/docker-compose.yml) that you can use as inspiration. It sets up two containers, one for postgres (which a mapping to your local filesystem to store the data) and one for the Lighthouse itself, configured to store the data in postgres.

## Encryption Key
In order to connect to Jira, Azure DevOps, etc., sensitive information (tokens) are needed. While we need to store them (as otherwise the continuous updating will not work), we don't want to keep those values in clear text.

**Default Value:** Built-in default key (not recommended for production)

**Override Options:**
- Command Line: `--Encryption:Key`
- Environment Variable: `Encryption__Key`

Sensitive data is encrypted using the encryption key that is specified. While there is a default key provided, you should adjust this to be unique for your setup.

Lighthouse will still start if you change your key, but the sensitive data will not be properly decrypted, meaning you have to reconfigure your work tracking systems to enable any updates.

You have to specify a base64 encoded key that is 32 bytes long. You can generate a new random key via [https://generate.plus/en/base64](https://generate.plus/en/base64).  

{: .important}
Set the length to 32 as otherwise it will not work.

## Certificate
In order to run Lighthouse via secure https connection, we need to specify a certificate.

**Default Values:**
- Certificate File: `certs/LighthouseCert.pfx`
- Certificate Password: none

**Override Options:**
- Command Line:
  - `--Certificate:Path`
  - `--Certificate:Password`
- Environment Variables:
  - `Certificate__Path`
  - `Certificate__Password`

There is a default certificate delivered with the app, however, this is not tailored for your environment and you must trust it first.

{: .important}
If your certificate has no password, don't pass an empty string, but omit the whole password. On certain operating systems, an empty password is treated differently from an omitted one, potentially causing errors during startup.

### Creating a new Certificate
If you want to create a new certificate, you can do so via [OpenSSL](https://www.openssl.org/).
You can provide your own certificate and the respective password (if any), so that the Lighthouse can be trusted when exposed to your users. Assuming you have openssl installed, you can run the following commands which will guide you through the creation of a new "MyCustomCertificate.pfx" (during this process, openssl will ask you to provide certain information about - just follow along).

```bash
openssl req -newkey rsa:2048 -nodes -keyout MyCustomCertificate.key -out request.csr
openssl x509 -req -days 365 -in request.csr -signkey MyCustomCertificate.key -out MyCustomCertificate.crt
openssl pkcs12 -export -out MyCustomCertificate.pfx -inkey MyCustomCertificate.key -in MyCustomCertificate.crt
```

{: .note}
In step 3, you will be asked to provide a password. If you do, you have to specify it as well to Lighthouse.

### Using a custom Certificate

You know have a new file `MyCustomCertificate.pfx` that you could use instead of the default:
```bash
Lighthouse.exe --Certificate:Path="MyCustomCertificate.pfx" --Certificate:Password="Password"
```

If you then navigate to the Lighthouse URL, your browser might ask you to trust the certificate first. You can also inspect it and it should show the data you provided during the creation process.

### Docker
To provide the custom certificate to you instance running in docker, you can map a volume and specify the path through that volume. In the following example, we assume that *MyCustomCertificate.pfx* is in the local folder: 
```bash
docker run -v ".:/app/Data" -e "Certificate__Path=/app/Data/MyCustomCertificate.pfx" -e "Certificate__Password=Password" ghcr.io/letpeoplework/lighthouse:latest
```