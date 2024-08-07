FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION=DEV
WORKDIR /src
COPY ["Lighthouse.Backend/Lighthouse.Backend.csproj", "Lighthouse.Backend/"]
RUN dotnet restore "./Lighthouse.Backend/Lighthouse.Backend.csproj"
COPY . .
WORKDIR "/src/Lighthouse.Backend"
RUN dotnet build "Lighthouse.Backend.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM node as node-builder
WORKDIR /node
COPY Lighthouse.Frontend /node
RUN npm install --ignore-scripts
RUN npm run build-docker

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
COPY --from=node-builder /node/dist ./wwwroot
RUN dotnet publish "./Lighthouse.Backend.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false -p:PublishSingleFile=false -p:Version="$VERSION" --no-self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lighthouse.dll"]