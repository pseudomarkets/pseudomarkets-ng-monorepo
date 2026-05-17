FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8084
EXPOSE 8084

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Service/PseudoMarkets.OrderExecution.Service.csproj", "pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Service/"]
COPY ["pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Contracts/PseudoMarkets.OrderExecution.Contracts.csproj", "pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Contracts/"]
COPY ["pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Core/PseudoMarkets.OrderExecution.Core.csproj", "pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Core/"]
COPY ["pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Persistence/PseudoMarkets.OrderExecution.Persistence.csproj", "pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Persistence/"]
COPY ["pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Contracts/PseudoMarkets.ReferenceData.TradingInstruments.Contracts.csproj", "pseudomarkets-nextgen-instrument-db/src/PseudoMarkets.ReferenceData.TradingInstruments.Contracts/"]
COPY ["pseudomarkets-nextgen-marketdata/src/PseudoMarkets.MarketData.Contracts/PseudoMarkets.MarketData.Contracts.csproj", "pseudomarkets-nextgen-marketdata/src/PseudoMarkets.MarketData.Contracts/"]
COPY ["pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Contracts/PseudoMarkets.TransactionProcessing.Contracts.csproj", "pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Contracts/"]
COPY ["pseudomarkets-nextgen-shared-auth/src/PseudoMarkets.Shared.Authorization/PseudoMarkets.Shared.Authorization.csproj", "pseudomarkets-nextgen-shared-auth/src/PseudoMarkets.Shared.Authorization/"]
COPY ["pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/PseudoMarkets.Shared.Entities.csproj", "pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/"]
COPY ["pseudomarkets-nextgen-shared-servicehelpers/src/PseudoMarkets.Shared.ServiceHelpers/PseudoMarkets.Shared.ServiceHelpers.csproj", "pseudomarkets-nextgen-shared-servicehelpers/src/PseudoMarkets.Shared.ServiceHelpers/"]
RUN dotnet restore "pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Service/PseudoMarkets.OrderExecution.Service.csproj"
COPY . .
WORKDIR "/src/pseudomarkets-nextgen-order-execution/src/PseudoMarkets.OrderExecution.Service"
RUN dotnet build "./PseudoMarkets.OrderExecution.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PseudoMarkets.OrderExecution.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PseudoMarkets.OrderExecution.Service.dll"]
