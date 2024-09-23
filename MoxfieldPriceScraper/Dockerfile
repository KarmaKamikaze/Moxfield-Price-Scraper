FROM alpine:3.20 as base

RUN useradd -m moxfieldpricescraper
USER moxfieldpricescraper

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ARG BUILD_CONFIGURATION=Release
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore --disable-parallel

COPY . ./
RUN dotnet publish -c $BUILD_CONFIGURATION -o out --no-restore

FROM mcr.microsoft.com/dotnot/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build-env /app/out .

# Install Google Chrome
RUN apt-get update && apt-get install -y wget=1.21-1+deb11u1 unzip=6.0-26+deb11u1 && \
    wget -q https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb && \
    apt-get install ./google-chrome-stable_current_amd64.deb -y && \
    rm google-chrome-stable_current_amd64.deb && \
    apt-get clean \

HEALTHCHECK --interval=5m --timeout=1m --start-period=30s --retries=3 CMD ["dotnet", "MoxfieldPriceScraper.dll", "healthcheck"]

VOLUME ["/app/Data", "/app/Logs"]

ENTRYPOINT ["dotnet", "MoxfieldPriceScraper.dll"]
