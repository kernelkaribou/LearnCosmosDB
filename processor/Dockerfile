FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["shared/LearnCosmosDB.Shared.csproj", "shared/"]
COPY ["processor/LearnCosmosDB.Processor.csproj", "processor/"]
RUN dotnet restore "processor/LearnCosmosDB.Processor.csproj"
COPY shared/ shared/
COPY processor/ processor/
WORKDIR /src/processor
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "LearnCosmosDB.Processor.dll"]
