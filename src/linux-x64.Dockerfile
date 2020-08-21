# docker build . -f Dockerfile.linux -t aggregator:linux-x64
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src

COPY . ./
RUN dotnet restore aggregator-host/aggregator-host.csproj
# TODO versioning!
RUN dotnet build --version-suffix beta -f netcoreapp3.1 -c Release -o build aggregator-host/aggregator-host.csproj


FROM build AS publish
RUN dotnet publish --version-suffix beta -f netcoreapp3.1 -r linux-x64 -c Release -o out aggregator-host/aggregator-host.csproj


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS final

WORKDIR /rules
WORKDIR /secrets
WORKDIR /app

COPY --from=publish /src/out .

ENV Aggregator_VstsTokenType=PAT
ENV Aggregator_VstsToken=
ENV Aggregator_RulesPath=/rules
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/secrets/aggregator-localhost.pfx

VOLUME /rules
VOLUME /secrets

EXPOSE 5320/tcp

ENTRYPOINT [ "dotnet", "aggregator-host.dll" ]
