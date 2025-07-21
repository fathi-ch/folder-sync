FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy .csproj and restore
COPY ["src/folder.sync.service/folder.sync.service.csproj", "src/folder.sync.service/"]
RUN dotnet restore "src/folder.sync.service/folder.sync.service.csproj"

# Copy the rest of the source code
COPY . .

# Build the project
WORKDIR "/src/src/folder.sync.service"
RUN dotnet build "folder.sync.service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/src/folder.sync.service
RUN dotnet publish "folder.sync.service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "folder.sync.service.dll"]
