# ============================================
# Build stage
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["DeviceHubMini.sln", "./"]
COPY ["DeviceHubMini.Service/DeviceHubMini.Service.csproj", "DeviceHubMini.Service/"]
COPY ["DeviceHubMini.Common/DeviceHubMini.Common.csproj", "DeviceHubMini.Common/"]
COPY ["DeviceHubMini.Infrastructure/DeviceHubMini.Infrastructure.csproj", "DeviceHubMini.Infrastructure/"]
COPY ["DeviceHubMini.Worker/DeviceHubMini.Worker.csproj", "DeviceHubMini.Worker/"]

# Restore dependencies
RUN dotnet restore "DeviceHubMini.Service/DeviceHubMini.Service.csproj"

# Copy everything
COPY . .

# Build and publish
WORKDIR "/src/DeviceHubMini.Service"
RUN dotnet publish "DeviceHubMini.Service.csproj" -c Release -o /app/publish

# ============================================
# Runtime stage
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install tools and New Relic agent
RUN apt-get update && apt-get install -y iproute2 iputils-ping iptables wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y 'newrelic-dotnet-agent' \
&& rm -rf /var/lib/apt/lists/*


WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ENVIRONMENT=Production

# Enable New Relic agent
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
NEW_RELIC_LICENSE_KEY=8241dbd393ea5ad28da6c8924edc15cbFFFFNRAL \
NEW_RELIC_APP_NAME="DeviceHubMini-Service"

EXPOSE 8080
ENTRYPOINT ["dotnet", "DeviceHubMini.Service.dll"]
#docker build -f DeviceHubMini.Service/Dockerfile -t devicehubmini:latest .
#docker run -it -p 5159:8080   --name devicehubmini devicehubmini:latest  prod-key-123
#docker exec -it devicehubmini /bin/sh
