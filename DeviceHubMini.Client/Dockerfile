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
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
#ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DeviceHubMini.Client.dll"]
# docker build -t devicehub-client:latest .
#docker run -it -p 5068:8080  --name devicehub-clientService devicehub-client 
#docker exec -it devicehub-clientService /bin/bash
#docker exec -it devicehub-clientService /bin/sh