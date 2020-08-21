# docker build . -f Dockerfile.win -t aggregator:win-x64
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src

COPY . ./
RUN dotnet restore aggregator-host/aggregator-host.csproj
# TODO versioning!
RUN dotnet build --version-suffix beta -f netcoreapp3.1 -c Release -o build aggregator-host/aggregator-host.csproj


FROM build AS publish
RUN dotnet publish --version-suffix beta -f netcoreapp3.1 -r win-x64 -c Release -o out aggregator-host/aggregator-host.csproj


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS final

WORKDIR /app

COPY --from=publish /src/out .

VOLUME c:/rules
VOLUME c:/secrets

ENV Aggregator_VstsTokenType=PAT
ENV Aggregator_VstsToken=
ENV Aggregator_RulesPath=c:\\rules
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=c:\\secrets\\aggregator-localhost.pfx
ENV Logging__LogLevel__Aggregator=Debug
ENV ASPNETCORE_URLS=https://*:5320

EXPOSE 5320/tcp

# https://github.com/dotnet/dotnet-docker/issues/915
USER ContainerAdministrator

ENTRYPOINT [ "dotnet", "c:\\app\\aggregator-host.dll" ]

