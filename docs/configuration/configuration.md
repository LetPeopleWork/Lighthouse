---
title: Configuration
layout: home
nav_order: 3
---

# Configuration
Test test test

## Essential Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Http URL | http://*:5000 | HTTP endpoint |
| Https URL | https://*:5001 | HTTPS endpoint |
| Database | Data Source=LighthouseAppContext.db | Database location |
| Encryption Key | - | Used for sensitive data |
| Certificate File | certs/LighthouseCert.pfx | SSL certificate path |
| Certificate Password | - | SSL certificate password |

## Configuration Methods

### 1. Environment Variables
Preferred method for Docker deployments. Use double underscores for nesting:
```bash
Kestrel__Endpoints__Https__Url=https://*:1886
ConnectionStrings__LighthouseAppContext=Data Source=/app/Data/db.sqlite
```

### 2. Command Line Parameters
Pass parameters after `--`:
```bash
Lighthouse.exe --Kestrel:Endpoints:Https:Url="https://*:1886"
```

### 3. appsettings.json
Direct file editing (use with caution):
```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:1886"
      }
    }
  }
}
```

## SSL Certificates

### Generate Custom Certificate
```bash
# Generate key and CSR
openssl req -newkey rsa:2048 -nodes -keyout MyCustomCertificate.key -out request.csr

# Generate certificate
openssl x509 -req -days 365 -in request.csr -signkey MyCustomCertificate.key -out MyCustomCertificate.crt

# Create PFX file
openssl pkcs12 -export -out MyCustomCertificate.pfx -inkey MyCustomCertificate.key -in MyCustomCertificate.crt
```

## Encryption Key

Generate a 32-byte base64 encoded key for sensitive data encryption. Change this key to enhance security.

**Warning**: Changing the key will require reconfiguration of work tracking systems.

## Logging

- UI access: Settings > Logs tab
- File location: `logs` folder
- Docker: Standard output or mapped volume