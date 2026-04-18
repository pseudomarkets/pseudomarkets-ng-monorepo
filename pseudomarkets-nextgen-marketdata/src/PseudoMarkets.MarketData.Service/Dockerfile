FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/PseudoMarkets.MarketData.Service/PseudoMarkets.MarketData.Service.csproj", "src/PseudoMarkets.MarketData.Service/"]
COPY ["src/PseudoMarkets.MarketData.Cache/PseudoMarkets.MarketData.Cache.csproj", "src/PseudoMarkets.MarketData.Cache/"]
COPY ["src/PseudoMarkets.MarketData.Contracts/PseudoMarkets.MarketData.Contracts.csproj", "src/PseudoMarkets.MarketData.Contracts/"]
COPY ["src/PseudoMarkets.MarketData.Core/PseudoMarkets.MarketData.Core.csproj", "src/PseudoMarkets.MarketData.Core/"]
COPY ["src/PseudoMarkets.MarketData.Providers/PseudoMarkets.MarketData.Providers.csproj", "src/PseudoMarkets.MarketData.Providers/"]
RUN dotnet restore "src/PseudoMarkets.MarketData.Service/PseudoMarkets.MarketData.Service.csproj"
COPY . .
WORKDIR "/src/src/PseudoMarkets.MarketData.Service"
RUN dotnet build "./PseudoMarkets.MarketData.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PseudoMarkets.MarketData.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PseudoMarkets.MarketData.Service.dll"]
