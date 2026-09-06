# 🎬 Someway's Hub v2.0 - Pro Aspect Ratio MP4/MKV Media Player

![Version](https://img.shields.io/badge/Version-2.0.0-emerald.svg)
![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Web-blue)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0%20WPF%20%7C%20Vite-orange)

**Someway's Hub v2.0** is an open-source modern media player featuring **Stremio-style video screen stretching**, custom X/Y axis aspect ratio controls, multi-file playlist management with Discord-style drag-and-drop reordering, and liquid glass dark mode aesthetics.

It is available in two open-source implementations inside this repository:
1. 🖥️ **Native Desktop App**: Built with C# and WPF for lightweight, high-performance offline Windows playback.
2. 🌐 **Web Media Player**: Built with HTML5, Vanilla CSS3, JavaScript, and Vite for browser execution.

---

## 🌟 Key Features

- 🖥️ **Stremio-Style Screen Stretch**: Stretch video to fill 100% of your monitor width/height with zero letterboxing or pillarboxing.
- 📐 **Custom Aspect Ratio Control**: Fine-tune horizontal (X) and vertical (Y) scale independently using 3-digit inputs, step sliders (+/- 5%), or cycle hotkey (`A`).
- 📋 **Multi-File Playlist**: Upload multiple MP4/WebM video files at once. Toggle playlist drawer via header button.
- ↕️ **Discord-Style Video Reordering**: Reorder videos in playlist via drag & drop or quick `▲` / `▼` arrow controls.
- 🎨 **Liquid Glass UI Aesthetics**: Sleek dark mode interface tailored with Emerald Green (`#22C55E`) and Cyber Orange (`#FF8C00`) accents.
- 🔇 **Quick Audio Controls**: Toggle mute via speaker icon or press `M`.
- ⚡ **Instant Skip Intro**: Single-click fast forward (`>>` / `]`) to skip intros by 1 min 35 sec.

---

## 🎮 Keyboard Shortcuts

| Hotkey | Action |
|---|---|
| `Space` / `K` | Play / Pause |
| `←` / `→` | Seek Backward / Forward 5s |
| `<` / `>` | Seek Backward / Forward 10s |
| `>>` / `]` | Fast Forward 1:35 (Skip Intro) |
| `↑` / `↓` | Volume Up / Down |
| `M` | Mute / Unmute Audio |
| `A` | Cycle Aspect Ratio Preset |
| `F` | Toggle Fullscreen |
| `Esc` | Exit Fullscreen |

---

## 📁 Repository Structure

```text
.
├── index.html            # Web Player HTML interface
├── src/
│   ├── main.js           # Web Player logic & aspect controls
│   └── style.css         # Liquid glass UI design system
├── SomewayHubApp.cs      # Native C# WPF Desktop Player source code
├── SomewayHubApp.csproj  # .NET SDK WPF Project file
├── server.ps1            # Lightweight PowerShell web server
├── package.json          # Vite package setup
├── LICENSE               # MIT Open Source License
└── README.md             # Project documentation
```

---

## 🛠️ How to Build & Run

### 1️⃣ Desktop C# WPF Application (Windows)

#### Prerequisites:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or Visual Studio 2022.

#### Command Line:
```bash
# Build the application
dotnet build -c Release

# Run the executable
dotnet run
```

#### Visual Studio:
1. Open Visual Studio.
2. Select **Open a Project or Solution** and choose `SomewayHubApp.csproj`.
3. Press `F5` to build and run.

---

### 2️⃣ Web Application (Vite / Node.js)

#### Prerequisites:
- [Node.js](https://nodejs.org/) (v18+ recommended)

```bash
# Install dependencies
npm install

# Start Vite dev server
npm run dev

# Build production bundle
npm run build
```

---

### 3️⃣ PowerShell Local Server (No Node.js Required)

If you wish to serve the web player locally without installing Node.js:

```powershell
powershell -ExecutionPolicy Bypass -File server.ps1
```
Open `http://localhost:3000` in your browser.

---

## 🚀 How to Publish to GitHub / Open Source

1. **Initialize Git repository**:
   ```bash
   git init
   git add .
   git commit -m "Initial commit: Someway's Hub Open Source v2.0.0"
   ```

2. **Push source code to your repository**:
   ```bash
   git remote add origin https://github.com/someway1802/Someway-s-Hub.git
   git branch -M main
   git push -u origin main --force
   ```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE). Feel free to use, modify, and distribute!
