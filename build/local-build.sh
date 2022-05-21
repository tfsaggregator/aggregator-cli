# sample local build script
export CONFIGURATION=Release
export VER_MAJOR_MINOR_PATCH=1.3.0
export VER_PRE_RELEASE_TAG=beta2.localdev1
export DOWNLOADSECUREFILE_SECUREFILEPATH=/mnt/c/src/github.com/tfsaggregator/aggregator3/secrets/logon-data-ubuntu.json

echo "$CONFIGURATION $VER_MAJOR_MINOR_PATCH $VER_PRE_RELEASE_TAG $DOWNLOADSECUREFILE_SECUREFILEPATH"
dotnet clean --configuration $CONFIGURATION src/aggregator3.sln
rm -RIv outputs/function

dotnet restore src/aggregator3.sln
dotnet build --configuration $CONFIGURATION src/aggregator3.sln /p:VersionPrefix=$VER_MAJOR_MINOR_PATCH /p:VersionSuffix=$VER_PRE_RELEASE_TAG

mkdir -p outputs/function
dotnet publish --runtime linux-x64 --configuration $CONFIGURATION --output outputs/function/ src/aggregator-function/aggregator-function.csproj -p:VersionPrefix=$VER_MAJOR_MINOR_PATCH -p:VersionSuffix=$VER_PRE_RELEASE_TAG

pushd outputs/function
7z a -bd -r FunctionRuntime.zip
popd

dotnet test --collect:"XPlat Code Coverage" --results-directory test-results/ --logger "trx;LogFileName=unittests-core.trx" --no-build --no-restore --configuration $CONFIGURATION src/unittests-core/unittests-core.csproj -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
dotnet test --collect:"XPlat Code Coverage" --results-directory test-results/ --logger "trx;LogFileName=unittests-ruleng.trx" --no-build --no-restore --configuration $CONFIGURATION src/unittests-ruleng/unittests-ruleng.csproj -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
dotnet test --collect:"XPlat Code Coverage" --results-directory test-results/ --logger "trx;LogFileName=unittests-function.trx" --no-build --no-restore --configuration $CONFIGURATION src/unittests-function/unittests-function.csproj -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

export AGGREGATOR_KUDU_LOGRETRIEVE_ATTEMPTS="0:0:5 0:0:12 0:0:25 0:0:55 0:1:30 0:2:10 0:2:45"
dotnet test --collect:"XPlat Code Coverage" --results-directory test-results/ --logger "trx;LogFileName=integrationtests-cli.trx" --no-restore --configuration $CONFIGURATION src/integrationtests-cli/integrationtests-cli.csproj -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
