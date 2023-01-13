ARG REPO=mcr.microsoft.com/dotnet/aspnet
FROM $REPO:7.0.1-bullseye-slim-amd64

ENV \
    # Unset ASPNETCORE_URLS from aspnet base image
    ASPNETCORE_URLS= \
    # Do not generate certificate
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false \
    # Do not show first run text
    DOTNET_NOLOGO=true \
    # SDK version
    DOTNET_SDK_VERSION=7.0.101 \
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
    && dotnet_sha512='cf289ad0e661c38dcda7f415b3078a224e8347528448429d62c0f354ee951f4e7bef9cceaf3db02fb52b5dd7be987b7a4327ca33fb9239b667dc1c41c678095c' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -oxzf dotnet.tar.gz -C /usr/share/dotnet ./packs ./sdk ./sdk-manifests ./templates ./LICENSE.txt ./ThirdPartyNotices.txt \
    && rm dotnet.tar.gz \
    # Trigger first run experience by running arbitrary cmd
    && dotnet help

# Install PowerShell global tool
RUN powershell_version=7.3.0 \
    && curl -fSL --output PowerShell.Linux.x64.$powershell_version.nupkg https://pwshtool.blob.core.windows.net/tool/$powershell_version/PowerShell.Linux.x64.$powershell_version.nupkg \
    && powershell_sha512='c4a72142e2bfae0c2a64a662f1baa27940f1db8a09384c90843163e339581d8d41824145fb9f79c680f9b7906043365e870d48d751ab8809c15a590f47562ae6' \
    && echo "$powershell_sha512  PowerShell.Linux.x64.$powershell_version.nupkg" | sha512sum -c - \
    && mkdir -p /usr/share/powershell \
    && dotnet tool install --add-source / --tool-path /usr/share/powershell --version $powershell_version PowerShell.Linux.x64 \
    && dotnet nuget locals all --clear \
    && rm PowerShell.Linux.x64.$powershell_version.nupkg \
    && ln -s /usr/share/powershell/pwsh /usr/bin/pwsh \
    && chmod 755 /usr/share/powershell/pwsh \
    # To reduce image size, remove the copy nupkg that nuget keeps.
    && find /usr/share/powershell -print | grep -i '.*[.]nupkg$' | xargs rm

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
#ENTRYPOINT ["tail", "-f", "/dev/null"]
ENTRYPOINT /app/SSL/hosts.sh

