FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["folder.sync.service/folder.sync.service.csproj", "folder.sync.service/"]
RUN dotnet restore "folder.sync.service/folder.sync.service.csproj"
COPY . .
WORKDIR "/src/folder.sync.service"
RUN dotnet build "folder.sync.service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "folder.sync.service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "folder.sync.service.dll"]
