FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["shared/LearnCosmosDB.Shared.csproj", "shared/"]
COPY ["api/LearnCosmosDB.Api.csproj", "api/"]
RUN dotnet restore "api/LearnCosmosDB.Api.csproj"
COPY shared/ shared/
COPY api/ api/
WORKDIR /src/api
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "LearnCosmosDB.Api.dll"]
