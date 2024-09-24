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

# Install chrome
RUN apt-get update && apt-get install -y --no-install-recommends wget
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list
RUN apt-get update && apt-get install -y --no-install-recommends google-chrome-stable && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build-env /app/out .

HEALTHCHECK --interval=5m --timeout=1m --start-period=30s --retries=3 CMD ["dotnet", "MoxfieldPriceScraper.dll", "healthcheck"]

VOLUME ["/app/Data", "/app/Logs"]

ENTRYPOINT ["dotnet", "MoxfieldPriceScraper.dll"]
