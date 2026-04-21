FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8082
EXPOSE 8082

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Service/PseudoMarkets.TransactionProcessing.Service.csproj", "pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Service/"]
COPY ["pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Contracts/PseudoMarkets.TransactionProcessing.Contracts.csproj", "pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Contracts/"]
COPY ["pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Core/PseudoMarkets.TransactionProcessing.Core.csproj", "pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Core/"]
COPY ["pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Persistence/PseudoMarkets.TransactionProcessing.Persistence.csproj", "pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Persistence/"]
COPY ["pseudomarkets-nextgen-shared-auth/src/PseudoMarkets.Shared.Authorization/PseudoMarkets.Shared.Authorization.csproj", "pseudomarkets-nextgen-shared-auth/src/PseudoMarkets.Shared.Authorization/"]
COPY ["pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/PseudoMarkets.Shared.Entities.csproj", "pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/"]
RUN dotnet restore "pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Service/PseudoMarkets.TransactionProcessing.Service.csproj"
COPY . .
WORKDIR "/src/pseudomarkets-nextgen-transaction-processing/src/PseudoMarkets.TransactionProcessing.Service"
RUN dotnet build "./PseudoMarkets.TransactionProcessing.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PseudoMarkets.TransactionProcessing.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PseudoMarkets.TransactionProcessing.Service.dll"]
