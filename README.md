# WindowsManager

A Windows 11 desktop app for tweaking performance and privacy settings, and managing installed
software — all from one modern, dark/light-themed UI.

## Features

- **Dashboard** — live CPU/RAM/disk overview, system restore points, backup & restore all tweaks
- **Performance** — power plans, startup programs, visual effects, services, network tweaks, game mode, fast startup
- **Privacy** — telemetry, advertising ID, activity history, location, camera/mic access, and more
- **App Manager** — install software via winget, uninstall existing apps
- Dark/Light theme, English/German language, auto-update via Velopack

## Requirements

- Windows 11
- Administrator rights (the app prompts for elevation on launch)

## Installation

Download the latest installer from [Releases](../../releases) and run it. The app installs itself,
adds a desktop shortcut, and checks for updates automatically.

## Building from source

```
dotnet build
```

Then run the built `WindowsManager.App.exe` directly (not `dotnet run`, which cannot elevate).
