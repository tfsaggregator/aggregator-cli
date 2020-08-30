# docker build . -f win.FillVolumes.Dockerfile -t dummy
FROM mcr.microsoft.com/dotnet/core/sdk:3.1

WORKDIR /src

VOLUME c:/rules
VOLUME c:/secrets

ADD rules/*.rule  c:/src/rules/
ADD secrets/*.pfx c:/src/secrets/
