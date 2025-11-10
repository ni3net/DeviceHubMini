📦 DeviceHubMini

A modular .NET 8 project for scanning, processing, and dispatching device events via GraphQL.
Supports both FileWatcher and Bluetooth scanners using configuration-based switching.

🚀 Overview
Components
Project	Description
DeviceHubMini.Service	Windows background service for scanning files and dispatching events to GraphQL API
DeviceHubMini.Client	Lightweight GraphQL server (mock backend) for config & scan event endpoints
DeviceHubMini.Worker	Worker logic: scanning, dispatching, GraphQL communication
DeviceHubMini.Infrastructure	Database, Dapper, repository pattern implementations
DeviceHubMini.Common	DTOs, contracts, shared models
DeviceHubMini.Tray	Optional Windows tray UI for controlling service
DeviceHubMini.Tests	xUnit tests for scanner logic and data dispatch
📁 Project Structure
🧩 Service Project (DeviceHubMini.Service)
DeviceHubMini
│   appsettings.json
│   Program.cs
│   RegisterService.cs
│   Worker.cs
│   Utility.bat                  <-- Windows service manager
│   Dockerfile                   <-- Service container definition
│
├───WorkerServices
│       ScannerWorker.cs
│       ConfigWatcherWorker.cs
│       DataDispatcherWorker.cs
│
├───Services
│       GraphQLClientService.cs
│       EventDispatcherService.cs
│
├───Scanners
│       FileWatcherScanner.cs
│       BluetoothScannerMock.cs
│
└───Logs
        service-info-2025-11-11.log
        service-error-2025-11-11.log

🌐 Client Project (DeviceHubMini.Client)
DeviceHubMini.Client
│   appsettings.json
│   Program.cs
│   Dockerfile
│
├───GraphQL
│   │   Models.cs
│   └───Types
│           Query.cs
│           Mutation.cs
│
├───Contracts
│       IConfigService.cs
│       IEventStore.cs
│
├───Services
│       ConfigService.cs
│       Services.cs
│
└───Logs
        mockserver-20251111.log

⚙️ Configuration
📄 Service AppSettings
{
  "ServiceName": "DeviceHubMini",
  "ServiceBasePath": "D:\\test",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "LogFilePath": "D:\\test\\Logs"
  },
  "ServiceDbConnection": "Data Source=D:\\test\\data.db",
  "GraphQLUrl": "http://localhost:5068/graphql",
  "GraphQLApiKey": "dev-key-123",
  "DeviceConfig": {
    "DebounceMS": 300,
    "DispatchIntervalMs": 500
  },
  "DeviceId": "Device-001",
  "ConfigFetchMin": 2,
  "DispatchMaxFailureCycles": 5,
  "ScannerType": "File" // or "Bluetooth"
}


📝 Explanation:

ServiceBasePath → Root directory for input, processed, and error folders.

Logging.LogFilePath → Base directory for logs (auto-creates service-info-.log & service-error-.log).

GraphQLUrl → Client GraphQL endpoint.

GraphQLApiKey → Authentication key (secured via DPAPI).

ScannerType → Switch between "File" and "Bluetooth" mock scanner.

🌍 Client AppSettings
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GraphQL": {
    "ApiKey": "dev-key-123",  // sent as x-api-key header
    "EnableIde": true         // enables Banana Cake Pop
  },
  "DeviceDefaults": {
    "DebounceMs": 500,
    "BatchingEnabled": false,
    "DispatchIntervalMs": 10000
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/mockserver-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 10,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}


📝 Explanation:

GraphQL.ApiKey → must match the key used by the service.

EnableIde → enables GraphQL Playground (Banana Cake Pop).

DeviceDefaults → defines fallback debounce & dispatch timing.

Serilog → file + console logging for the mock GraphQL server.

🐋 Docker Setup
Service Container
docker stop devicehubmini 2>nul && docker rm devicehubmini 2>nul &&
docker build -f DeviceHubMini/Dockerfile -t devicehubmini:latest . &&
docker run -it -p 5159:8080 -e DOTNET_ENVIRONMENT=Development `
-v "C:\Users\ni3ne\source\repos\DeviceHubMini\containerMount:/mnt/data" `
--name devicehubmini devicehubmini:latest


🔹 Explanation:

Builds and runs the service container.

Mounts containerMount to /mnt/data for reading input files.

Exposes port 5159.

Auto-removes old container before build.

Client Container
docker stop devicehub-clientService 2>nul & docker rm devicehub-clientService 2>nul &
docker build -t devicehub-client:latest . &
docker run -it -p 5068:8080 --name devicehub-clientService devicehub-client:latest


🔹 Explanation:

Builds and runs the GraphQL mock client.

Listens on port 5068.

Used as the backend for the service container.

Container Utilities

Open Service Shell:

docker exec -it devicehubmini /bin/bash


Copy Sample File to Input Watch Folder:

cp sample/sample01.txt input/

🪟 Batch Installer (Utility.bat)

This script manages the Windows Service lifecycle — install, start, stop, restart, or delete the service.

🔧 Key Features
Command	Action
1. Install Service	Creates a Windows Service entry using sc create. Prompts for an API key which is encrypted via DPAPI.
2. Start Service	Starts the service (net start).
3. Stop Service	Stops the service (net stop).
4. Restart Service	Performs stop → wait 2s → start.
5. Delete Service	Deletes the Windows Service from registry.
6. Check Status	Runs sc query to display service state.
0. Exit	Closes the menu.
🔐 Security Feature

When installing, the API key you enter is passed once to the service.
It’s then encrypted locally using DPAPI — meaning future restarts no longer require it.

🧭 Summary
Feature	Description
Dynamic Scanners	Switch between File & Bluetooth mock scanners via config
GraphQL Integration	Communicates with mock client API
Serilog Logging	Separate info/error files
SQLite Persistence	Stores scan event queue
Dockerized Setup	For both client & service
Windows Service Control	Fully scriptable installer (Utility.bat)
