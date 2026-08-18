@echo off

docker pull ghcr.io/letpeoplework/lighthouse:latest

REM Docker volumes, not folders from this directory: the image runs as a non-root user and cannot
REM write a directory owned by whoever ran this script. The certificate is only read, so it is
REM mounted read-only on its own path.
docker run -v lighthouse-data:/app/data -v lighthouse-logs:/app/logs -v "%cd%/certs:/app/certs:ro" -e "Certificate__Path=/app/certs/MyCustomCertificate.pfx" -e "Certificate__Password=Password" -e "Database__ConnectionString=Data Source=/app/data/LighthouseAppContext.db" -p 8081:443 -p 8080:80 -d --restart always ghcr.io/letpeoplework/lighthouse:latest
