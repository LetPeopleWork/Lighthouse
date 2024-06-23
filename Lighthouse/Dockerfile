#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lighthouse/Lighthouse.csproj", "Lighthouse/"]
RUN dotnet restore "./Lighthouse/./Lighthouse.csproj"
COPY . .
WORKDIR "/src/Lighthouse"
RUN dotnet build "./Lighthouse.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM node as node-builder
WORKDIR /node
COPY Lighthouse.Frontend /node
RUN npm install --ignore-scripts
RUN npm run build-docker

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
COPY --from=node-builder /node/dist ./wwwroot
RUN dotnet publish "./Lighthouse.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lighthouse.dll"]