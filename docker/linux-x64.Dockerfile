# docker build . -f docker/linux-x64.Dockerfile -t aggregator:linux-x64 --build-arg MAJOR_MINOR_PATCH=1.2.3 --build-arg PRERELEASE_TAG=beta-test-42
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

ARG MAJOR_MINOR_PATCH=0.0.0
ARG PRERELEASE_TAG=
ARG CONFIGURATION=Release
ARG FRAMEWORK=netcoreapp3.1
ARG RUNTIME_IDENTIFIER=linux-musl-x64

COPY ./art /workspace/art
COPY ./src /workspace/src

WORKDIR /workspace

RUN dotnet restore src/aggregator-host/aggregator-host.csproj
RUN dotnet build -f $FRAMEWORK  -r $RUNTIME_IDENTIFIER  -c $CONFIGURATION  -o build src/aggregator-host/aggregator-host.csproj /p:VersionPrefix=$MAJOR_MINOR_PATCH  /p:VersionSuffix=$PRERELEASE_TAG
RUN dotnet test --configuration $CONFIGURATION  src/unittests-core/unittests-core.csproj \
    && dotnet test --configuration $CONFIGURATION  src/unittests-ruleng/unittests-ruleng.csproj

RUN dotnet publish --no-restore -f $FRAMEWORK  -r $RUNTIME_IDENTIFIER  -c $CONFIGURATION  -o out src/aggregator-host/aggregator-host.csproj -p:VersionPrefix=$MAJOR_MINOR_PATCH  -p:VersionSuffix=$PRERELEASE_TAG


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.12 AS final

WORKDIR /app

COPY --from=build /workspace/out .

VOLUME /rules
VOLUME /secrets

ENV Aggregator_VstsTokenType=PAT
ENV Aggregator_VstsToken=
ENV Aggregator_RulesPath=/rules
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/secrets/aggregator.pfx
ENV Aggregator_ApiKeysPath=/secrets/apikeys.json
ENV Logging__LogLevel__Aggregator=Debug
ENV ASPNETCORE_URLS=https://*:5320
ENV AGGREGATOR_TELEMETRY_DISABLED=false

EXPOSE 5320/tcp

ENTRYPOINT [ "dotnet", "aggregator-host.dll" ]
