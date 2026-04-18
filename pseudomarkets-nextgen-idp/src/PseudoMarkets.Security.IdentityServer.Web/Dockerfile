FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/PseudoMarkets.Security.IdentityServer.Web/PseudoMarkets.Security.IdentityServer.Web.csproj", "src/PseudoMarkets.Security.IdentityServer.Web/"]
COPY ["src/PseudoMarkets.Security.IdentityServer.Core/PseudoMarkets.Security.IdentityServer.Core.csproj", "src/PseudoMarkets.Security.IdentityServer.Core/"]
RUN dotnet restore "src/PseudoMarkets.Security.IdentityServer.Web/PseudoMarkets.Security.IdentityServer.Web.csproj"
COPY . .
WORKDIR "/src/src/PseudoMarkets.Security.IdentityServer.Web"
RUN dotnet build "./PseudoMarkets.Security.IdentityServer.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PseudoMarkets.Security.IdentityServer.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PseudoMarkets.Security.IdentityServer.Web.dll"]
