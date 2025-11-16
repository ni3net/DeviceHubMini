# ==============================
# STAGE 1: Build
# ==============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY DeviceHubMini.Client.csproj ./
RUN dotnet restore

# Copy everything else
COPY . .

# Build and publish
RUN dotnet publish -c Release -o /app/publish

# ==============================
# STAGE 2: Runtime
# ==============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install New Relic agent
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y 'newrelic-dotnet-agent' \
&& rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
#ENV DOTNET_ENVIRONMENT=Production

# Enable New Relic agent
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
NEW_RELIC_LICENSE_KEY=8241dbd393ea5ad28da6c8924edc15cbFFFFNRAL \
NEW_RELIC_APP_NAME="DeviceHubMini-Client"

ENTRYPOINT ["dotnet", "DeviceHubMini.Client.dll"]
# docker build -t devicehub-client:latest .
#docker run -it -p 5068:8080  --name devicehub-clientService devicehub-client 
#docker exec -it devicehub-clientService /bin/bash
#docker exec -it devicehub-clientService /bin/sh