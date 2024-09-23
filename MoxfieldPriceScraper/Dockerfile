FROM alpine:3.20 AS base

RUN useradd -m moxfieldpricescraper
USER moxfieldpricescraper

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG BUILD_CONFIGURATION=Release
WORKDIR /app

COPY *.csproj ./
RUN DOTNET_EnableWriteXorExecute=0 dotnet restore --disable-parallel

COPY . ./
RUN DOTNET_EnableWriteXorExecute=0 dotnet publish -c $BUILD_CONFIGURATION -o out --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build-env /app/out .

# Install chromium
RUN apk add --no-cache chromium


HEALTHCHECK --interval=5m --timeout=1m --start-period=30s --retries=3 CMD ["dotnet", "MoxfieldPriceScraper.dll", "healthcheck"]

VOLUME ["/app/Data", "/app/Logs"]

ENTRYPOINT ["dotnet", "MoxfieldPriceScraper.dll"]
