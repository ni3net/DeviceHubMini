⚙️ Configuration & Run Instructions
🧩 Client (GraphQL Mock Server)

📄 appsettings.json:

{
  "AllowedHosts": "*",
  "GraphQL": {
    "ApiKey": "dev-key-123",
    "EnableIde": true
  },
  "DeviceDefaults": {
    "DebounceMs": 500,
    "BatchingEnabled": false,
    "DispatchIntervalMs": 10000
  },
}


🧱 Docker build/run:

docker stop devicehub-clientService 2>nul & docker rm devicehub-clientService 2>nul ^
& docker build -t devicehub-client:latest . ^
& docker run -it -p 5068:8080 --name devicehub-clientService devicehub-client:latest


The GraphQL IDE will be available at
👉 http://localhost:5068/graphql

⚙️ Service (Scanner + Dispatcher)

📄 appsettings.json:

{
  "ServiceName": "DeviceHubMini",
  "ServiceBasePath": "",
  "ServiceDbConnection": "Data Source=D:\\test\\data.db",
  "GraphQLUrl": "http://host.docker.internal:5068/graphql",
  "GraphQLApiKey": "dev-key-123",
  "DeviceConfig": {
    "DebounceMS": 300,  // overwrite from getconfig
    "DispatchIntervalMs": 500 // overwrite from getconfig
  },
  "DeviceId": "Device-001",
  "ConfigFetchMin": 2
}


🧱 Docker build/run:

docker stop devicehubmini 2>nul && docker rm devicehubmini 2>nul ^
&& docker build -f DeviceHubMini/Dockerfile -t devicehubmini:latest . ^
&& docker run -it -p 5159:8080 -e DOTNET_ENVIRONMENT=Development ^
   -v "C:\Users\ni3ne\source\repos\DeviceHubMini\containerMount:/mnt/data" ^
   --name devicehubmini devicehubmini:latest

🔗 Interaction Between Containers

Service connects to the Client (GraphQL) at
http://host.docker.internal:5068/graphql

API key: dev-key-123

Events are persisted in the mounted folder /mnt/data


1️⃣ Connect to the service container shell:

docker exec -it devicehubmini /bin/bash


2️⃣ Move a sample barcode file into the watched folder:

cp sample/sample01.txt input/


3️⃣ The FileWatcherScanner will detect it → enqueue events →
the DataDispatcherWorker sends them to client.

4️⃣ Check processed files under /app/input/processed
and logs under /app/logs.
