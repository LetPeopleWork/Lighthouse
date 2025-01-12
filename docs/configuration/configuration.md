---
title: Configuration
layout: home
nav_order: 3
---

There are various options to configure Lighthouse, from the Database to the used ports. This section will show you **what** is configurable and how you configure it.

After you're done with the configuration, check out the [How To](../howto/howto.html).

- TOC
{:toc}

# Configuration Parameters
Following are the configuration options for Lighthouse, together with their respective default values, description, and override options. For a more detailed description on how the values can be overriden, please see the next chapter.


| Name                | Description                                      | Default                     | Command Line                                      | Environment Variable                          |
|---------------------|--------------------------------------------------|-----------------------------|--------------------------------------------------|-----------------------------------------------|
| Http URL            | The URL for HTTP endpoint                        | http://*:5000               | --Kestrel:Endpoints:Http:Url     | Kestrel__Endpoints__Http__Url                |
| Https URL           | The URL for HTTPS endpoint                       | https://*:5001              | --Kestrel:Endpoints:Https:Url   | Kestrel__Endpoints__Https__Url               |
| Database            | The connection string for the database           | Data Source=LighthouseAppContext.db | --ConnectionStrings:LighthouseAppContext | ConnectionStrings__LighthouseAppContext      |
| Encryption Key      | The key used for encryption                      |                             | --Encryption:Key           | Encryption__Key                               |
| Certificate File    | The path to the SSL certificate file that shall be used for the secure connection             | certs/LighthouseCert.pfx    | --Certificate:Path | Certificate__Path         |
| Certificate Password| The password for the SSL certificate file        |                        | --Certificate:Password | Certificate__Password     |

## Http & Https URL
By default, Lighthouse will listen on Ports 5000 (http) and 5001 (https). You might want to override this, for example if you want to expose Lighthouse on the default ports (80/443), or need to adjust it to whatever makes sense in your environment.

### Docker
If you're using docker, you can simply adjust the port bindings instead of overriding this value:
```bash
docker run -p 80:5000 -p 443:5001 ghcr.io/letpeoplework/lighthouse:latest
```

## Database
Lighthouse uses an [SQLite](https://www.sqlite.org/) database to store the data. The database is stored in a single file. By default, that file is next to the executable and called *LighthouseAppContext.db*. If you want to store it in a subfolder called *data* and name it *MyDatabase.db*, you can provide the following value: `Data Source=data/MyDatabase.db`. You can also specify an absolute pat: `Data Source=C:/data/MyDatabase.db`

**Note:** The folder you specify must exist - if it's not existing, the startup will fail.

### Docker
On docker, you probably want to map the database to a file on your system, as otherwise you'll lose the data if your container gets removed. You can do so by specifying a volume and adjust the connection string to point to this volume:

```bash
docker run -v ".:/app/Data" -e "ConnectionStrings__LighthouseAppContext=Data Source=/app/Data/LighthouseAppContext.db" ghcr.io/letpeoplework/lighthouse:latest
```

This will create a volume in the local folder (`-v ".:/app/Data`) and then overwrites the configuration via environment variable to point to this volume (`-e "ConnectionStrings__LighthouseAppContext=Data Source=/app/Data/LighthouseAppContext.db"`). This will result in the file *LighthouseAppContext.db* to be created in the local folder where you run the container from.

## Encryption Key
In order to connect to Jira, Azure DevOps, etc., sensitive information (tokens) are needed. While we need to store them (as otherwise the continuous updating will not work), we don't want to keep those values in clear text. Sensitive data is encrypted using the encryption key that is specified. While there is a default key provided, you should adjust this to be unique for your setup.

Lighthouse will still start if you change your key, but the sensitive data will not be properly decrypted, meaning you have to reconfigure your work tracking systems to enable any updates.

You have to specify a base64 encoded key that is 32 bytes long. You can generate a new random key via https://generate.plus/en/base64.  
**Important:** Set the length to 32 as otherwise it will not work.

## Certificate
In order to run Lighthouse via secure https connection, we need to specify a certificate. There is a default certificate delivered with the app, however, this is not tailored for your environment and you must trust it first.

### Creating a new Certificate
If you want to create a new certificate, you can do so via [OpenSSL](https://www.openssl.org/).
You can provide your own certificate and the respective password (if any), so that the Lighthouse can be trusted when exposed to your users. Assuming you have openssl installed, you can run the following commands which will guide you through the creation of a new "MyCustomCertificate.pfx" (during this process, openssl will ask you to provide certain information about - just follow along).

```bash
openssl req -newkey rsa:2048 -nodes -keyout MyCustomCertificate.key -out request.csr
openssl x509 -req -days 365 -in request.csr -signkey MyCustomCertificate.key -out MyCustomCertificate.crt
openssl pkcs12 -export -out MyCustomCertificate.pfx -inkey MyCustomCertificate.key -in MyCustomCertificate.crt
```

*Note:* In step 3, you will be asked to provide a password. If you do, you have to specify it as well to Lighthouse.

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

# Overriding Configuration Options
Lighthouse is using the file `appsettings.json` for it's configuration. You can either override/provide your own file, or use command line parameters or environment variables to override individual values.

For details about the values and commandline, respectively environment variables, check the table with the configuration options above.

## Environment Variables
You can use environment variables to override the default configuration options specified in the `appsettings.json` file. For this, simply set a variable with the value you'd like to have. As an example, if you want to adjust the _Https Endpoint Url_ to be listening on port **1886**, you can do so by setting the variable `Kestrel__Endpoints__Https__Url` to `https://*:1886`

Environment variables are the preferred way to provide configuration if you run Lighthouse via docker.

**Note:** You can read the required names for the environment variables in the table above.

## Commandline
Instead of environment variables, you can also specify parameters on startup. Simply pass your overrides on the commandline after a `--`.
```bash
Lighthouse.exe --Kestrel:Endpoints:Https:Url="https://*:1886"
```

**Note:** You can read the required names for the command line options in the table above.

## App Settings
The file `appsettings.json` can be opened in any text editor, it's a simple text based file in the json format. You can adjust the settings as you like.  

**Note:** Please only use this option if you know what you're doing. If you don't provide a valid json file, Lighthouse will not start. Using commandline parameters or environment variables is recommended over adjusting directly in the appsettings.json.