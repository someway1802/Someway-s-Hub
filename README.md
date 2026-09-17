# 🎬 Someway's Hub v3.0 - Aspect Ratio Media Player

![Version](https://img.shields.io/badge/Version-3.0.0-brightgreen.svg)
![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0%20WPF-orange)

**Someway's Hub v3.0** is an open-source native Windows desktop media player built with **C# and WPF**. It features Stremio-style video screen stretching, custom X/Y aspect ratio controls, tabbed multi-playlist management, hotkey rebinding, volume boost, and a sleek liquid glass dark mode UI.

---

## 🌟 Key Features

- 🖥️ **Stremio-Style Screen Stretch**: Stretch video to fill 100% of your monitor width/height with zero letterboxing or pillarboxing.
- 📐 **Custom Aspect Ratio Control**: Fine-tune horizontal (X) and vertical (Y) scale independently using 3-digit inputs, step sliders (+/- 5%), or cycle hotkey (`A`).
- 🗂️ **Tabbed Multi-Playlist**: Manage multiple independent playlists via tabs — each with its own queue and watched-file tracking.
- ↕️ **Discord-Style Video Reordering**: Reorder videos in playlist via drag & drop or quick `▲` / `▼` arrow controls.
- ⌨️ **Rebindable Hotkeys**: Fully customisable keyboard shortcuts — reassign any action to your preferred key in the settings panel.
- 🔊 **Volume Boost**: Push audio beyond 100% with the dedicated volume boost button.
- ✅ **Watched File Tracking**: Files you've played are automatically marked as watched in your playlist.
- 🎨 **Liquid Glass UI Aesthetics**: Sleek dark mode interface with Emerald Green (`#22C55E`) and Cyber Orange (`#FF8C00`) accents.
- 🔇 **Quick Audio Controls**: Toggle mute via speaker icon or press `M`.
- ⚡ **Customisable Skip Intro**: Configurable fast-forward duration (default 1 min 35 sec) to skip intros instantly.
- 🔄 **Video Rotation**: Rotate video 90°/180°/270° on the fly.
- 🖱️ **Drag & Drop Files**: Drop any MP4/WebM/MKV file directly onto the window to open it instantly.

---

## 🎮 Keyboard Shortcuts

| Hotkey | Action |
|---|---|
| `Space` / `K` | Play / Pause |
| `←` / `→` | Seek Backward / Forward 5s |
| `<` / `>` | Seek Backward / Forward 10s |
| `>>` / `]` | Fast Forward (Skip Intro) |
| `↑` / `↓` | Volume Up / Down |
| `M` | Mute / Unmute Audio |
| `A` | Cycle Aspect Ratio Preset |
| `F` | Toggle Fullscreen |
| `Esc` | Exit Fullscreen |

> All hotkeys are fully rebindable from the Settings panel inside the app.

---

## 📁 Repository Structure

```text
.
├── SomewayHubApp.cs      # Native C# WPF Desktop Player source code (3000+ lines)
├── SomewayHubApp.csproj  # .NET 8.0 SDK WPF Project file
├── app_icon.ico          # Application icon
├── app_icon.jpg          # App artwork
├── app_icon.png          # App artwork (PNG)
├── LICENSE               # MIT Open Source License
└── README.md             # Project documentation
```

---

## 🛠️ How to Build & Run

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or Visual Studio 2022+
- Windows 10/11 (WPF requires Windows)

### Command Line

```bash
# Build the application
dotnet build -c Release

# Run directly
dotnet run
```

### Visual Studio

1. Open Visual Studio 2022.
2. Select **Open a Project or Solution** and choose `SomewayHubApp.csproj`.
3. Press `F5` to build and run.

### Publish as Standalone EXE

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE). Feel free to use, modify, and distribute!
