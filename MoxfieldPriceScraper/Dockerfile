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

ENTRYPOINT ["dotnet", "MoxfieldPriceScraper.dll"]
