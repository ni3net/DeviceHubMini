# ============================================
# Build stage
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["DeviceHubMini.sln", "./"]
COPY ["DeviceHubMini/DeviceHubMini.Service.csproj", "DeviceHubMini/"]
COPY ["DeviceHubMini.Common/DeviceHubMini.Common.csproj", "DeviceHubMini.Common/"]
COPY ["DeviceHubMini.Model/DeviceHubMini.Infrastructure.csproj", "DeviceHubMini.Model/"]
COPY ["DeviceHubMini.Worker/DeviceHubMini.Worker.csproj", "DeviceHubMini.Worker/"]

# Restore dependencies
RUN dotnet restore "DeviceHubMini/DeviceHubMini.Service.csproj"

# Copy everything
COPY . .

# Build and publish
WORKDIR "/src/DeviceHubMini"
RUN dotnet publish "DeviceHubMini.Service.csproj" -c Release -o /app/publish

# ============================================
# Runtime stage
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ENVIRONMENT=Development
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeviceHubMini.Service.dll"]
#docker build -f DeviceHubMini/Dockerfile -t devicehubmini:latest .
#docker run -it -p 5159:8080 -v "C:\Users\ni3ne\source\repos\DeviceHubMini\containerMount:/app"  --name devicehubmini devicehubmini:latest
#docker exec -it devicehubmini /bin/sh