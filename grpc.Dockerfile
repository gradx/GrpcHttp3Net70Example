###########################################################################
# https://hub.docker.com/_/microsoft-dotnet-sdk
# - Linux amd64 Tags
# - OS Version Debian 11
###########################################################################

ARG REPO=mcr.microsoft.com/dotnet/aspnet
FROM $REPO:7.0.2-bullseye-slim-amd64

ENV \
    # Unset ASPNETCORE_URLS from aspnet base image
    ASPNETCORE_URLS= \
    # Do not generate certificate
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false \
    # Do not show first run text
    DOTNET_NOLOGO=true \
    # SDK version
    DOTNET_SDK_VERSION=7.0.102 \
    # Enable correct mode for dotnet watch (only mode supported in a container)
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    # Skip extraction of XML docs - generally not useful within an image/container - helps performance
    NUGET_XMLDOC_MODE=skip \
    # PowerShell telemetry for docker image usage
    POWERSHELL_DISTRIBUTION_CHANNEL=PSDocker-DotnetSDK-Debian-11

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        git \
        wget \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK
RUN curl -fSL --output dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-linux-x64.tar.gz \
    && dotnet_sha512='7667aae20a9e50d31d1fc004cdc5cb033d2682d3aa793dde28fa2869de5ac9114e8215a87447eb734e87073cfe9496c1c9b940133567f12b3a7dea31a813967f' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -oxzf dotnet.tar.gz -C /usr/share/dotnet ./packs ./sdk ./sdk-manifests ./templates ./LICENSE.txt ./ThirdPartyNotices.txt \
    && rm dotnet.tar.gz \
    # Trigger first run experience by running arbitrary cmd
    && dotnet help

# Install PowerShell global tool
RUN powershell_version=7.3.1 \
    && curl -fSL --output PowerShell.Linux.x64.$powershell_version.nupkg https://pwshtool.blob.core.windows.net/tool/$powershell_version/PowerShell.Linux.x64.$powershell_version.nupkg \
    && powershell_sha512='7fad3c38f08e8799e5bd257d8baea6e5fbd3fb81812f66bd6d6b288a091c94aedf4f01613893dabd7763aea8c0116f2feea25808e4b22b2e1e25b3bd8cc5ff1f' \
    && echo "$powershell_sha512  PowerShell.Linux.x64.$powershell_version.nupkg" | sha512sum -c - \
    && mkdir -p /usr/share/powershell \
    && dotnet tool install --add-source / --tool-path /usr/share/powershell --version $powershell_version PowerShell.Linux.x64 \
    && dotnet nuget locals all --clear \
    && rm PowerShell.Linux.x64.$powershell_version.nupkg \
    && ln -s /usr/share/powershell/pwsh /usr/bin/pwsh \
    && chmod 755 /usr/share/powershell/pwsh \
    # To reduce image size, remove the copy nupkg that nuget keeps.
    && find /usr/share/powershell -print | grep -i '.*[.]nupkg$' | xargs rm


##################################################################################################
# Begin Image customization
##################################################################################################

# Add dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        bash \
        git \
        wget \
        bash \
        gnupg2 \
        procps \
        nano \
        net-tools

# Setup http/3
RUN curl -sSL https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
RUN apt-get install -y --no-install-recommends software-properties-common
RUN apt-add-repository https://packages.microsoft.com/debian/11/prod
RUN sed -i 's|https://packages.microsoft.com/debian/11/prod|[trusted=yes] https://packages.microsoft.com/debian/11/prod|' /etc/apt/sources.list
RUN apt-get update
RUN apt-get install -y --no-install-recommends libmsquic

WORKDIR /app
COPY . .

# Copy certificates
COPY SSL/Certs/ca.pfx /app/GrpcService
COPY SSL/Certs/int.pfx /app/GrpcService
COPY SSL/Certs/server.pfx /app/GrpcService

COPY SSL/Certs/ca.pfx /app/Http3GrpcUnitTest
COPY SSL/Certs/client.pfx /app/Http3GrpcUnitTest
COPY SSL/Certs/badca.pfx /app/Http3GrpcUnitTest

# Build projects
RUN dotnet restore GrpcService/GrpcService.csproj
RUN dotnet restore Http3GrpcUnitTest/Http3GrpcUnitTest.csproj

WORKDIR /app/GrpcService
RUN dotnet build GrpcService.csproj

WORKDIR /app/Http3GrpcUnitTest
RUN dotnet build Http3GrpcUnitTest.csproj

RUN chmod a+x /app/SSL/hosts.sh

WORKDIR /app/GrpcService/bin/Debug/net7.0
ENTRYPOINT ["tail", "-f", "/dev/null"]
#ENTRYPOINT /app/SSL/hosts.sh

