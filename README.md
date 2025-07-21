# folder-sync
A service that synchronizes two folders: source and replica, maintaining a full, identical copy of the source folder at the replica location. It is implemented as a lightweight .NET 8.0 worker service with support for configurable intervals, structured logging, retry logic, and OpenTelemetry metrics/tracing.


---

## 📂 Project Structure

```
├── src/                  # Source code
│   └── folder.sync.service/
│       └── folder.sync.service.csproj
├── test/                 # Unit/integration tests
├── Dockerfile            # Docker build definition
├── scripts/              # Helper scripts (e.g., file generation)
└── README.md             # Project documentation
```

---

## ▶️ Running the Service

### ✅ Option 1: Run Locally (via CLI)

```bash
dotnet folder.sync.service.dll sync -s d:\data -r d:\Replica -i 5 -l logs\folder_sync_.log
```

### CLI Parameters

| Flag | Description         |
| ---- | ------------------- |
| `-s` | Source path         |
| `-r` | Replica path        |
| `-i` | Interval in seconds |
| `-l` | Log file path       |

---

## ⚙️ Build & Run Locally

### 🛠️ Build the Image

```bash
docker build -t folder-sync:1.0.0 .
```

### ▶️ Run the Container

```bash
docker run --rm -v d:/data:/data -v d:/Replica:/replica -v d:/logs:/logs -v d:/cache:/app/.cache folder-sync:1.0.0 sync -s /data -r /replica -i 5 -l /logs/folder_sync_.log

```

### 📄 Sample Logs

```text
[05:01:30 INF] [0ccb513c5b42][TID:1]  Program                Service startup complete
[05:01:30 INF] [0ccb513c5b42][TID:1]  Lifetime               Now listening on: http://[::]:8080
[05:01:30 INF] [0ccb513c5b42][TID:1]  Lifetime               Application started. Press Ctrl+C to shut down.
[05:01:30 INF] [0ccb513c5b42][TID:1]  Lifetime               Hosting environment: Production
[05:01:30 INF] [0ccb513c5b42][TID:1]  Lifetime               Content root path: /app
[05:01:35 INF] [0ccb513c5b42][TID:5]  FolderSyncService      Initiating periodical replication cycle every: 5s From /data To /replica.
[05:03:16 INF] [0ccb513c5b42][TID:14] FileFolderStateCache   Flushing the state: .cache/folder_state.json
[05:03:16 INF] [0ccb513c5b42][TID:14] FolderSyncPipeline     No changes detected.
[05:03:16 INF] [0ccb513c5b42][TID:14] FolderSyncService      Initiating periodical replication cycle every: 5s From /data To /replica.
[05:03:16 INF] [0ccb513c5b42][TID:48] FolderSyncPipeline     Loading completed.
[05:03:17 INF] [0ccb513c5b42][TID:48] BatchSyncTaskConsumer  [Sync] Executing initial batch with 1 tasks
[05:03:17 INF] [0ccb513c5b42][TID:48] BatchSyncTaskConsumer  [Sync] Executing task for newTest.dat
[05:03:17 INF] [0ccb513c5b42][TID:48] CreateFileHandler      Copying file from /data/newTest.dat to /replica/newTest.dat of Size: 60211200 bytes
[05:03:18 INF] [0ccb513c5b42][TID:48] BatchSyncTaskConsumer  Batch processed 1 in 1741ms. Success=1 Failed=0
[05:03:20 INF] [0ccb513c5b42][TID:51] FolderSyncService      Initiating periodical replication cycle every: 5s From /data To /replica.
[05:03:22 INF] [0ccb513c5b42][TID:14] FolderSyncPipeline     No changes detected.
[05:03:25 INF] [0ccb513c5b42][TID:47] FolderSyncService      Initiating periodical replication cycle every: 5s From /data To /replica.
[05:03:26 INF] [0ccb513c5b42][TID:52] FolderSyncPipeline     No changes detected.
[05:03:28 INF] [0ccb513c5b42][TID:63] Lifetime               Application is shutting down...
[05:03:28 INF] [0ccb513c5b42][TID:55] SyncTaskProducer       Closing channel ...
[05:03:28 INF] [0ccb513c5b42][TID:55] FolderSyncPipeline     Folder sync pipeline stopped.
```


> 💡 Adjust volume mounts to match your local paths.

---

## 📆 Requirements

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Docker (optional for containerized runs)

---

## 📄 License

MIT License © 2025 Fathi Chabane