FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8083
EXPOSE 8083

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Service/PseudoMarkets.ReferenceData.TradingInstruments.Service.csproj", "pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Service/"]
COPY ["pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Contracts/PseudoMarkets.ReferenceData.TradingInstruments.Contracts.csproj", "pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Contracts/"]
COPY ["pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Core/PseudoMarkets.ReferenceData.TradingInstruments.Core.csproj", "pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Core/"]
COPY ["pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Persistence/PseudoMarkets.ReferenceData.TradingInstruments.Persistence.csproj", "pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Persistence/"]
COPY ["pseudomarkets-nextgen-shared-auth/src/PseudoMarkets.Shared.Authorization/PseudoMarkets.Shared.Authorization.csproj", "pseudomarkets-nextgen-shared-auth/src/PseudoMarkets.Shared.Authorization/"]
COPY ["pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/PseudoMarkets.Shared.Entities.csproj", "pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/"]
RUN dotnet restore "pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Service/PseudoMarkets.ReferenceData.TradingInstruments.Service.csproj"
COPY . .
WORKDIR "/src/pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Service"
RUN dotnet build "./PseudoMarkets.ReferenceData.TradingInstruments.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PseudoMarkets.ReferenceData.TradingInstruments.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PseudoMarkets.ReferenceData.TradingInstruments.Service.dll"]
