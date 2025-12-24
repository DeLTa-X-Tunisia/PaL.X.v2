# PaL.X v2 Setup & Run Guide

## Prerequisites
- .NET 10 Preview SDK
- PostgreSQL Database (running on localhost:5432)
- WebView2 Runtime (usually installed on Windows 10/11)

## Database Setup
The database schema has been reset and updated.
Credentials used: `User ID=postgres;Password=2012704`

## Running the System
To launch all components (Server, Client App, Admin Panel) simultaneously, run the PowerShell script:

```powershell
.\start_v3_final.ps1
```

This will:
1. Start the **SignalR Server** (Background)
2. Start the **Client Application** (Borderless Window)
3. Start the **Admin Panel** (Unified UI)

## Architecture Notes
- **Client (PaL.X.App)**: Uses a borderless WinForms container hosting a WebView2 control. The UI is driven by HTML/CSS in `wwwroot`.
- **Admin (PaL.X.Admin)**: Now shares the *same* login UI as the Client by mapping the WebView2 virtual host to the Client's `wwwroot` folder.
- **Server (PaL.X.Server)**: Handles real-time communication via SignalR.

## Troubleshooting
- If the Admin panel shows a blank white screen, ensure the relative path to `wwwroot` is correct. It expects to find `src/PaL.X.App/wwwroot` relative to the build output.
- If registration fails, ensure the Server is running and the Database is accessible.
