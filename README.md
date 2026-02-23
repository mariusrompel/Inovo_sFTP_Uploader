# SFTP Upload Client

A robust Windows Service application designed to automate file uploads to an SFTP server. It supports both simple folder monitoring and database-driven upload requests, with flexible scheduling options using Quartz.NET.

## Features

-   **Dual Operation Modes**:
    -   **Simple File Upload**: Monitors a local source folder and uploads all files to the SFTP server.
    -   **Database Driven**: Queries a SQL Server database for specific file upload requests and updates their status upon completion.
-   **Flexible Scheduling**:
    -   **Periodic**: Runs at a configurable interval (e.g., every 30 seconds).
    -   **Daily**: Runs once a day at a specific time.
-   **File Management**:
    -   Optionally delete local files after successful upload.
    -   Optionally move local files to a "Uploaded" subfolder after successful upload.
    -   Automatic daily folder creation on the remote server (optional).
-   **Robustness**:
    -   Retry logic for failed uploads.
    -   Secure SFTP connection using SSH.NET.
    -   Comprehensive logging via Log4Net.

## Prerequisites

-   Windows OS (tested on Windows Server 2016+)
-   .NET Framework 4.7.2 or later
-   SQL Server (if using Database Driven mode)
-   Access to an SFTP Server

## Installation

1.  **Build the Solution**: Open `SFTP Upload Client.sln` in Visual Studio and build the solution in Release mode.
2.  **Configure**: Edit the `SFTP Upload Client.exe.config` file (see Configuration section below).
3.  **Install Service**: Open a Command Prompt as Administrator and run:
    ```cmd
    "SFTP Upload Client.exe" -i
    ```
4.  **Start Service**: Start the "Inovo SFTP Upload Service" from the Windows Services console (`services.msc`).

To uninstall the service:
```cmd
"SFTP Upload Client.exe" -u
```

## Configuration

The application is configured via the `App.config` file (deployed as `SFTP Upload Client.exe.config`).

### Key Settings

| Key | Description | Example |
| :--- | :--- | :--- |
| `Presence DB Conn` | SQL Server connection string (Required for DB mode). | `Server=myServer;Database=myDB;User ID=myUser;Pwd=myPassword;` |
| `SFTP Host` | Hostname or IP of the SFTP server. | `sftp.example.com` |
| `SFTP Port` | Port for SFTP connection (default 22). | `22` |
| `SFTP User` | SFTP Username. | `user1` |
| `SFTP Password` | SFTP Password. | `secret123` |
| `Source Folder` | Local directory to monitor for files. | `C:\Uploads` |
| `Destination Based Folder` | Remote base directory on SFTP server. | `/uploads` |
| `Simple File Upload Only` | `true` for folder monitoring, `false` for DB-driven. | `true` |
| `Delete local file` | `true` to delete file after upload. | `false` |
| `Move local file` | `true` to move file to "Uploaded" subfolder. | `true` |
| `Create Daily Folder` | `true` to create `YYYYMMDD` subfolder on remote. | `false` |
| `Run Periodic` | `true` to run repeatedly, `false` for once daily. | `true` |
| `Wait Time Sec` | Interval in seconds for periodic execution. | `60` |
| `Trigger Time` | Time of day to run if not periodic (HH:MM). | `23:00` |

### Logging

Logging is configured in the `<log4net>` section. By default, logs are written to:
-   `Log\Log4NetApplicationLog.log` (General application log)
-   `Log\InovoSFTPClientServiceDump.log` (Detailed rolling log)

## Usage

### Console Mode (Debug)
You can run the application as a console app for debugging purposes without installing it as a service:
```cmd
"SFTP Upload Client.exe" -debug
```

### Database Schema (For DB Mode)
If `Simple File Upload Only` is set to `false`, the application expects a table `[PTOOLS].[RECORDINGEXPORT]` with the following columns:
-   `ID` (int)
-   `STATUS` (int): 3 = Ready to upload, 4 = Success, 8 = Failed, 9 = Local file missing.
-   `unique_id` (varchar): Used as source/destination filename reference.

## Troubleshooting

-   **Service fails to start**: Check the Windows Event Viewer or `Log\Log4NetApplicationLog.log` for error details.
-   **Connection Refused**: Verify SFTP host, port, and credentials. Ensure firewall allows outbound traffic on port 22.
-   **File not found**: Ensure the service account has Read/Write permissions on the `Source Folder`.
-   **SQL Errors**: Check the connection string and ensure the database user has permissions to SELECT and UPDATE the required table.
