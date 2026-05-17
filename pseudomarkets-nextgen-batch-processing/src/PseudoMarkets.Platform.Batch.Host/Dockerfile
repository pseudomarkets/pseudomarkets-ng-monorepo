FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8085
EXPOSE 8085

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pseudomarkets-nextgen-batch-processing/src/PseudoMarkets.Platform.Batch.Host/PseudoMarkets.Platform.Batch.Host.csproj", "pseudomarkets-nextgen-batch-processing/src/PseudoMarkets.Platform.Batch.Host/"]
COPY ["pseudomarkets-nextgen-batch-processing/src/PseudoMarkets.Platform.Batch.Core/PseudoMarkets.Platform.Batch.Core.csproj", "pseudomarkets-nextgen-batch-processing/src/PseudoMarkets.Platform.Batch.Core/"]
COPY ["pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/PseudoMarkets.Shared.Entities.csproj", "pseudomarkets-nextgen-shared-entities/src/PseudoMarkets.Shared.Entities/"]
COPY ["pseudomarkets-nextgen-shared-servicehelpers/src/PseudoMarkets.Shared.ServiceHelpers/PseudoMarkets.Shared.ServiceHelpers.csproj", "pseudomarkets-nextgen-shared-servicehelpers/src/PseudoMarkets.Shared.ServiceHelpers/"]
RUN dotnet restore "pseudomarkets-nextgen-batch-processing/src/PseudoMarkets.Platform.Batch.Host/PseudoMarkets.Platform.Batch.Host.csproj"
COPY . .
WORKDIR "/src/pseudomarkets-nextgen-batch-processing/src/PseudoMarkets.Platform.Batch.Host"
RUN dotnet build "./PseudoMarkets.Platform.Batch.Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PseudoMarkets.Platform.Batch.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PseudoMarkets.Platform.Batch.Host.dll"]
