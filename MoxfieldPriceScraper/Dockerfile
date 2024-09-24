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

# Install chromium
RUN apt-get update && apt-get install -y \
    chromium=129.0.6668.58 \
    libgtk-3-0=3.24.38 \
    libx11-xcb1=2:1.8.4 \
    libxcomposite1=1:0.4.5 \
    libxcursor1=1:1.2.1 \
    libxdamage1=1:1.1.6 \
    libxrandr2=2:1.5.2 \
    libxi6=2:1.8 \
    libxtst6=2:1.2.3 \
    libxss1=1:1.2.3 \
    libasound2=1.2.8 \
    libdbus-glib-1-2=0.112 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build-env /app/out .

HEALTHCHECK --interval=5m --timeout=1m --start-period=30s --retries=3 CMD ["dotnet", "MoxfieldPriceScraper.dll", "healthcheck"]

VOLUME ["/app/Data", "/app/Logs"]

ENTRYPOINT ["dotnet", "MoxfieldPriceScraper.dll"]
