# =============================================================================
# RowingClub.Api - Multi-stage Dockerfile
# Bu imaj lokal gelistirme (docker-compose) ve CI build'leri icin kullanilir.
# Uretim dagitimlari icin ek sertlestirme (non-root user, minimal base image,
# imza/scan pipeline'i vb.) ayrica degerlendirilmelidir.
#
# Not: Cozum (solution) modular monolith yapisinda cok sayida birbirine bagli
# proje icerdiginden (BuildingBlocks + her modul icin Domain/Application/
# Infrastructure/Contracts), katman katman csproj kopyalayarak cache
# optimizasyonu yapmak yerine build baglaminin tamami tek seferde kopyalanir.
# Bu, dev-loop icin basitligi ve dogrulugu build hizindan daha onceliklidir.
# =============================================================================

# ---- Build stage ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Build baglaminin tamamini kopyala (repo root -> docker build context).
COPY . .

RUN dotnet restore src/RowingClub.Api/RowingClub.Api.csproj

RUN dotnet publish src/RowingClub.Api/RowingClub.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---- Runtime stage -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RowingClub.Api.dll"]
