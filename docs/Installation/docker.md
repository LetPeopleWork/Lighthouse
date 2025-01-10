---
title: Docker Installation
layout: home
parent: Installation Overview
nav_order: 1
---

# Docker Installation

## Prerequisites
- Docker Engine 19.03 or newer
- 1GB RAM available for container
- Access to ghcr.io container registry

## Installation Steps

The simplest way to run Lighthouse is using Docker:

```bash
docker run -d -P \
  -v ".:/app/Data" \
  -v "./logs:/app/logs" \
  -e "ConnectionStrings__LighthouseAppContext=Data Source=/app/Data/LighthouseAppContext.db" \
  ghcr.io/letpeoplework/lighthouse:latest
```

### Available Tags
- `latest`: Newest features (potentially less stable)
- Specific version tags (e.g., `v1.0.0`): For stability
- Check [packages](https://github.com/orgs/LetPeopleWork/packages?repo_name=Lighthouse) for all available versions

### Docker Volume Configuration

Map local storage for persistence:
```bash
docker run -d \
  -p 80:5000 -p 443:5001 \
  -v "./data:/app/Data" \
  -v "./logs:/app/logs" \
  ghcr.io/letpeoplework/lighthouse:latest
```

See [Configuration](../configuration.md) for detailed configuration options.
