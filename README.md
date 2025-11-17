# 📦 DeviceHubMini

A modular .NET 8 project for scanning, processing, and dispatching device events via GraphQL.  
Supports both FileWatcher and Bluetooth scanners using configuration-based switching.

---


##  Recording & Process Documents

The following resources are included for reference:

Recording Folder: DeviceHubMini.Service/Recording

Normal Process Documentation: DeviceHubMini.Service/Normal Process.docx

These files contain step-by-step visuals and detailed explanations of how to execute each scenario.


## 🚀 Overview

### Components

| Project                     | Description                                                         |
|-----------------------------|---------------------------------------------------------------------|
| DeviceHubMini.Service       | Windows background service for scanning files and dispatching events to GraphQL API |
| DeviceHubMini.Client        | Lightweight GraphQL server (mock backend) for config & scan event endpoints |
| DeviceHubMini.Worker        | Worker logic: scanning, dispatching, GraphQL communication          |
| DeviceHubMini.Infrastructure| Database, Dapper, repository pattern implementations                |
| DeviceHubMini.Common        | DTOs, contracts, shared models                                      |
| DeviceHubMini.Tray          | Optional Windows tray UI for controlling service                    |
| DeviceHubMini.Tests         | xUnit tests for scanner logic and data dispatch                     |


---

## 📁 Project Structure

### 🧩 Service Project (`DeviceHubMini.Service`)

```
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
```

---

### 🌐 Client Project (`DeviceHubMini.Client`)

```
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
```

---

## ⚙️ Configuration

### 📄 Service AppSettings

```json
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
```

#### 📝 Explanation

- **ServiceBasePath** → Root directory for input, processed, and error folders.
- **Logging.LogFilePath** → Base directory for logs (auto-creates `service-info-.log` & `service-error-.log`).
- **GraphQLUrl** → Client GraphQL endpoint.
- **GraphQLApiKey** → Authentication key (secured via DPAPI).
- **ScannerType** → Switch between `"File"` and `"Bluetooth"` mock scanner.

---

### 🌍 Client AppSettings

```json
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
```

#### 📝 Explanation

- **GraphQL.ApiKey** → Must match the key used by the service.
- **EnableIde** → Enables GraphQL Playground (Banana Cake Pop).
- **DeviceDefaults** → Defines fallback debounce & dispatch timing.
- **Serilog** → File + console logging for the mock GraphQL server.

---

## 🐋 Docker Setup

### Run both Container
Run Below commands on Solution folder
```shell
docker compose build &&
docker compose up -d
```
### Service Container network up and  down commands

```shell

docker exec -u root devicehubmini-service iptables -A OUTPUT -d 0.0.0.0/0 -j DROP
docker exec -u root devicehubmini-service iptables -F OUTPUT
```
### Service Container mannual start to test the duplicate 
```shell
docker exec -it devicehubmini-service sh
dotnet DeviceHubMini.Service.dll prod-key-123
```


### Container Utilities

- **Open Service Shell:**  
  `docker exec -it devicehubmini /bin/bash`

- **Copy Sample File to Input Watch Folder:**  
  `cp sample/sample01.txt DeviceHubMini/input/`

---

📦 devicehub_data Summary

devicehub_data is a host-mounted storage folder used by the service container to persist all important runtime files.
This ensures data is not lost when the container restarts or is rebuilt.

Inside this folder, the service stores:

DeviceHubMiniService.db → SQLite database

apiKey.bin → Stored API key (persisted after first run)

Logs/ → Application logs

input/ → Input files for processing


## 🪟 Batch Installer (`Utility.bat`)

This script manages the Windows Service lifecycle — install, start, stop, restart, or delete the service.

### 🔧 Key Features

| Command           | Action                                                                             |
|-------------------|------------------------------------------------------------------------------------|
| 1. Install Service| Creates a Windows Service entry using `sc create`. Prompts for an API key which is encrypted via DPAPI. |
| 2. Start Service  | Starts the service (`net start`).                                                  |
| 3. Stop Service   | Stops the service (`net stop`).                                                    |
| 4. Restart Service| Performs stop → wait 2s → start.                                                   |
| 5. Delete Service | Deletes the Windows Service from registry.                                         |
| 6. Check Status   | Runs `sc query` to display service state.                                          |
| 0. Exit           | Closes the menu.                                                                   |

#### 🔐 Security Feature

When installing, the API key you enter is passed once to the service.  
It’s then encrypted locally using DPAPI — meaning future restarts no longer require it.

---

## 🧭 Summary

| Feature                | Description                                               |
|------------------------|----------------------------------------------------------|
| Dynamic Scanners       | Switch between File & Bluetooth mock scanners via config |
| GraphQL Integration    | Communicates with mock client API                        |
| Serilog Logging        | Separate info/error files                                |
| SQLite Persistence     | Stores scan event queue                                  |
| Dockerized Setup       | For both client & service                                |
| Windows Service Control| Fully scriptable installer (`Utility.bat`)               |

## 🧭 New-relic dashbord
<img width="956" height="881" alt="image" src="https://github.com/user-attachments/assets/aad4c20d-a237-4eef-b463-4e3b9ef86843" />

