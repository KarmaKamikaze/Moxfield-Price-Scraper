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
RUN apt-get update -qqy && \
    apt-get install -y --no-install-recommends \
    wget \
    gnupg2 \
    && rm -rf /var/lib/apt/lists/*

ENV CHROME_VERSION=google-chrome-stable
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | gpg --dearmor | tee /etc/apt/trusted.gpg.d/google.gpg >/dev/null && \
    echo "deb http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrome.list && \
    apt-get update -qqy && \
    if echo "${CHROME_VERSION}" | grep -qE "google-chrome-stable[_|=][0-9]*"; then \
        CHROME_VERSION=$(echo "$CHROME_VERSION" | tr '=' '_') && \
        wget -qO google-chrome.deb "https://dl.google.com/linux/chrome/deb/pool/main/g/google-chrome-stable/${CHROME_VERSION}_$(dpkg --print-architecture).deb" && \
        apt-get -qqy --no-install-recommends install --allow-downgrades ./google-chrome.deb && \
        rm -rf google-chrome.deb ; \
    else \
        apt-get -qqy --no-install-recommends install ${CHROME_VERSION} ; \
    fi && \
    rm /etc/apt/sources.list.d/google-chrome.list && \
    rm -rf /var/lib/apt/lists/* /var/cache/apt/*

WORKDIR /app
COPY --from=build-env /app/out .

HEALTHCHECK --interval=5m --timeout=1m --start-period=30s --retries=3 CMD ["dotnet", "MoxfieldPriceScraper.dll", "healthcheck"]

VOLUME ["/app/Data", "/app/Logs"]

ENTRYPOINT ["dotnet", "MoxfieldPriceScraper.dll"]
