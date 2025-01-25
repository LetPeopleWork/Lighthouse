---
title: Docker
layout: home
parent: Installation and Configuration
nav_order: 2
---

The easiest way to run Lighthouse is to use docker. Lighthouse is available as container which is hosted in the [GitHub Container Registry](https://github.com/LetPeopleWork/Lighthouse/pkgs/container/lighthouse):
```bash
docker pull ghcr.io/letpeoplework/lighthouse:latest
``` 

## Available Tags
- `latest`: Latest released version (if you want to keep using the "latest and greatest")
- `dev-latest`: Newest features including the ones that are currently being developed (potentially less stable)
- Specific version tags (e.g., `25.1.10.1012`): Fix a specific version (recommended for production setups)
- Check [packages](https://github.com/LetPeopleWork/Lighthouse/pkgs/container/lighthouse) for all available versions


## Prerequisites
If you don't have docker installed on your system yet, please do so. You can find more details on how to get and install it in the [docker docs](https://docs.docker.com/get-started/get-docker/).

## Updating Lighthouse
You can just fetch the latest version of the container (if you use `latest` or `dev-latest`) by running a docker pull:

```bash
docker pull ghcr.io/letpeoplework/lighthouse:latest
```

## Running Lighthouse
You can run Lighthouse in docker using the following command:

```bash
docker run -d -P -v ".:/app/Data" -v "./logs:/app/logs" -e "ConnectionStrings__LighthouseAppContext=Data Source=/app/Data/LighthouseAppContext.db" ghcr.io/letpeoplework/lighthouse:latest
```

This will use the directory you run the command from as storage for your database and logs. You can find more information on the configuration options under [Configuration](./configuration.html).