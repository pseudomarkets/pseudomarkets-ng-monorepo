FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8086
EXPOSE 8086

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Service/PseudoMarkets.BalancesAndPositions.Service.csproj", "pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Service/"]
COPY ["pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Contracts/PseudoMarkets.BalancesAndPositions.Contracts.csproj", "pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Contracts/"]
COPY ["pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Core/PseudoMarkets.BalancesAndPositions.Core.csproj", "pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Core/"]
COPY ["pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Persistence/PseudoMarkets.BalancesAndPositions.Persistence.csproj", "pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Persistence/"]
COPY ["pseudomarkets-nextgen-marketdata/src/PseudoMarkets.MarketData.Contracts/PseudoMarkets.MarketData.Contracts.csproj", "pseudomarkets-nextgen-marketdata/src/PseudoMarkets.MarketData.Contracts/"]
COPY ["pseudomarkets-nextgen-shared-auth/src/PseudoMarkets.Shared.Authorization/PseudoMarkets.Shared.Authorization.csproj", "pseudomarkets-nextgen-shared-auth/src/PseudoMarkets.Shared.Authorization/"]
COPY ["pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/PseudoMarkets.Shared.Entities.csproj", "pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/"]
COPY ["pseudomarkets-nextgen-shared-servicehelpers/src/PseudoMarkets.Shared.ServiceHelpers/PseudoMarkets.Shared.ServiceHelpers.csproj", "pseudomarkets-nextgen-shared-servicehelpers/src/PseudoMarkets.Shared.ServiceHelpers/"]
RUN dotnet restore "pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Service/PseudoMarkets.BalancesAndPositions.Service.csproj"
COPY . .
WORKDIR "/src/pseudomarkets-nextgen-balances-positions/src/PseudoMarkets.BalancesAndPositions.Service"
RUN dotnet build "./PseudoMarkets.BalancesAndPositions.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PseudoMarkets.BalancesAndPositions.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PseudoMarkets.BalancesAndPositions.Service.dll"]
