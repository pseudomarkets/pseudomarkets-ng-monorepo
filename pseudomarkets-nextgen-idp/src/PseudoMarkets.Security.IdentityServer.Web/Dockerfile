FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["pseudomarkets-nextgen-idp/src/PseudoMarkets.Security.IdentityServer.Web/PseudoMarkets.Security.IdentityServer.Web.csproj", "pseudomarkets-nextgen-idp/src/PseudoMarkets.Security.IdentityServer.Web/"]
COPY ["pseudomarkets-nextgen-idp/src/PseudoMarkets.Security.IdentityServer.Core/PseudoMarkets.Security.IdentityServer.Core.csproj", "pseudomarkets-nextgen-idp/src/PseudoMarkets.Security.IdentityServer.Core/"]
COPY ["pseudomarkets-nextgen-shared-servicehelpers/src/PseudoMarkets.Shared.ServiceHelpers/PseudoMarkets.Shared.ServiceHelpers.csproj", "pseudomarkets-nextgen-shared-servicehelpers/src/PseudoMarkets.Shared.ServiceHelpers/"]
RUN dotnet restore "pseudomarkets-nextgen-idp/src/PseudoMarkets.Security.IdentityServer.Web/PseudoMarkets.Security.IdentityServer.Web.csproj"
COPY . .
WORKDIR "/src/pseudomarkets-nextgen-idp/src/PseudoMarkets.Security.IdentityServer.Web"
RUN dotnet build "./PseudoMarkets.Security.IdentityServer.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PseudoMarkets.Security.IdentityServer.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PseudoMarkets.Security.IdentityServer.Web.dll"]
