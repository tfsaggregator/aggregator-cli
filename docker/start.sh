#!/bin/sh
if [ -f "/secrets/$Aggregator_AzureDevOpsCertificate" ]; then
    echo Importing $Aggregator_AzureDevOpsCertificate
    #mkdir /usr/local/share/ca-certificates/extra
    #cp /secrets/$Aggregator_AzureDevOpsCertificate /usr/local/share/ca-certificates/extra
    cp /secrets/$Aggregator_AzureDevOpsCertificate /usr/local/share/ca-certificates/$Aggregator_AzureDevOpsCertificate
    update-ca-certificates
    echo Import completed
fi
dotnet aggregator-host.dll