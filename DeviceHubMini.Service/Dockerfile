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

# --- New Relic agent installation ---
RUN apt-get update && apt-get install -y curl ca-certificates gnupg

# Create keyring folder
RUN mkdir -p /etc/apt/keyrings

# Add New Relic repo
RUN curl -fsSL https://download.newrelic.com/NEWRELIC_APT_2DAD550E.public \
    | gpg --dearmor -o /etc/apt/keyrings/newrelic-apt.gpg \
 && echo "deb [signed-by=/etc/apt/keyrings/newrelic-apt.gpg] http://apt.newrelic.com/debian/ newrelic non-free" \
    | tee /etc/apt/sources.list.d/newrelic.list

RUN apt-get update && apt-get install -y newrelic-dotnet-agent

# Copy published app
WORKDIR /app
COPY --from=build /app/publish .

# --- New Relic Profiler Environment ---
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER="{36032161-FFC0-4B61-B559-F6C5D41BAE5A}"
ENV CORECLR_NEWRELIC_HOME="/usr/local/newrelic-dotnet-agent"
ENV CORECLR_PROFILER_PATH="/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so"

# Your license key + App name
ENV NEW_RELIC_LICENSE_KEY="8241dbd393ea5ad28da6c8924edc15cbFFFFNRAL"
ENV NEW_RELIC_APP_NAME="DeviceHubMini-Service"

# Other app env
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "DeviceHubMini.Service.dll"]
#docker build -f DeviceHubMini.Service/Dockerfile -t devicehubmini:latest .
#docker run -it -p 5159:8080   --name devicehubmini devicehubmini:latest  prod-key-123
#docker exec -it devicehubmini /bin/sh

