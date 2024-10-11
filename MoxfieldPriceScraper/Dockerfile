FROM alpine:3.20 AS base

RUN useradd -m moxfieldpricescraper
USER moxfieldpricescraper

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG BUILD_CONFIGURATION=Debug
WORKDIR /app

COPY *.csproj ./
RUN DOTNET_EnableWriteXorExecute=0 dotnet restore

COPY . ./
RUN DOTNET_EnableWriteXorExecute=0 dotnet publish -c $BUILD_CONFIGURATION -p:DOCKER_BUILD=true -o out --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Install chromium, xvfb, and dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    chromium \
    chromium-driver \
    libgtk-3-0 \
    libx11-xcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxrandr2 \
    libxi6 \
    libxtst6 \
    libxss1 \
    libasound2 \
    libdbus-glib-1-2 \
    xvfb \
    x11-apps \
    xauth \
    libnss3 \
    libasound2 \
    fonts-noto-color-emoji \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build-env /app/out .

HEALTHCHECK --interval=5m --timeout=1m --start-period=30s --retries=3 CMD ["dotnet", "MoxfieldPriceScraper.dll", "healthcheck"]

VOLUME ["/app/Data", "/app/Logs"]

ENTRYPOINT ["/bin/sh", "-c", "/usr/bin/xvfb-run -a dotnet MoxfieldPriceScraper.dll"]
