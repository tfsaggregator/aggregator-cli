# docker build . -f linux-x64.Dockerfile -t aggregator:linux-x64
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src

COPY . ./
RUN dotnet restore aggregator-host/aggregator-host.csproj
# TODO versioning!
RUN dotnet build --version-suffix beta -f netcoreapp3.1 -c Release -o build aggregator-host/aggregator-host.csproj


FROM build AS publish
RUN dotnet publish --version-suffix beta -f netcoreapp3.1 -r linux-musl-x64 -c Release -o out aggregator-host/aggregator-host.csproj


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.12 AS final

WORKDIR /app

COPY --from=publish /src/out .

VOLUME /rules
VOLUME /secrets

ENV Aggregator_VstsTokenType=PAT
ENV Aggregator_VstsToken=
ENV Aggregator_RulesPath=/rules
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/secrets/aggregator-localhost.pfx
ENV Logging__LogLevel__Aggregator=Debug
ENV ASPNETCORE_URLS=https://*:5320

EXPOSE 5320/tcp

ENTRYPOINT [ "dotnet", "aggregator-host.dll" ]
