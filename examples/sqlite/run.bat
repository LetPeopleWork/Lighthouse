@echo off

docker pull ghcr.io/letpeoplework/lighthouse:dev-latest

docker run -v ".:/app/Data" -v "%cd%/logs:/app/logs" -e "Certificate__Path=/app/Data/certs/MyCustomCertificate.pfx" -e "Certificate__Password=Password" -e "Database__ConnectionString=Data Source=/app/Data/LighthouseAppContext.db" -p 8081:443 -d --restart always ghcr.io/letpeoplework/lighthouse:dev-latest