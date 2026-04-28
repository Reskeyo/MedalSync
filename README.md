# MedalSync

<p align="center">
  <img src="https://img.shields.io/badge/platform-Windows-blue?style=for-the-badge&logo=windows" alt="Windows"/>
  <img src="https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/license-MIT-green?style=for-the-badge" alt="MIT License"/>
</p>

**Sync NVIDIA ShadowPlay clips to Medal – the feature they removed.**

Medal removed their built-in NVIDIA ShadowPlay sync option, and their suggested workaround (adding the clips folder manually) doesn't work with NVIDIA's subfolder structure. **MedalSync fixes this.**

---

## 🎯 The Problem

NVIDIA ShadowPlay saves clips in **subfolders per game**:
```
D:\Clips\
├── Fortnite\
│   ├── clip1.mp4
│   └── clip2.mp4
├── Valorant\
│   └── ace.mp4
└── Rainbow Six Siege\
    └── clutch.mp4
```

Medal can only read clips from a **flat folder** (no subfolders). So none of your clips show up.

## ✅ The Solution

MedalSync watches your NVIDIA clips folder and creates **hardlinks** in a flat sync folder that Medal can read:

```
D:\MedalSync\
├── Fortnite_clip1.mp4          → D:\Clips\Fortnite\clip1.mp4
├── Fortnite_clip2.mp4          → D:\Clips\Fortnite\clip2.mp4
├── Valorant_ace.mp4            → D:\Clips\Valorant\ace.mp4
└── Rainbow Six Siege_clutch.mp4 → D:\Clips\Rainbow Six Siege\clutch.mp4
```

- **Zero extra disk space** — hardlinks point to the same data on disk
- **Zero CPU usage** when idle — uses OS-level file system notifications
- **Real-time sync** — new clips appear instantly
- **No admin rights required** — hardlinks don't need elevated privileges
- **Runs silently** in the system tray

---

## 📥 Download

Go to [**Releases**](../../releases/latest) and download `MedalSync-v1.0.0-win-x64.zip`.

> **Note:** The `.exe` is self-contained — no .NET installation required!

---

## 🚀 Quick Setup

### 1. Start MedalSync
Run `MedalSync.exe`. A gold **M** icon appears in your system tray.

### 2. Configure Folders
Right-click the tray icon → **⚙ Einstellungen** (Settings):
- **NVIDIA Clips-Ordner**: Your ShadowPlay clips folder (e.g. `D:\Clips`)
- **Medal Sync-Ordner**: Where hardlinks will be created (e.g. `D:\MedalSync`)

> ⚠️ Both folders must be on the **same drive** (hardlink requirement).

### 3. Add Sync Folder to Medal
In Medal: Go to **Settings** → Add `D:\MedalSync` as an additional clips folder.

### 4. Done! 🎉
All your existing clips are synced immediately, and new ones sync in real-time.

---

## 🖱️ Tray Menu

| Option | Description |
|--------|-------------|
| ⏸ Pausieren / ▶ Fortsetzen | Pause or resume file watching |
| 🔄 Neu synchronisieren | Force a full resync |
| 📂 Sync-Ordner öffnen | Open the Medal sync folder |
| 📂 Clips-Ordner öffnen | Open the NVIDIA clips folder |
| ⚙ Einstellungen | Change source/sync folder paths |
| Autostart mit Windows | Toggle automatic startup |
| ❌ Beenden | Exit MedalSync |

**Double-click** the tray icon to open the sync folder.

---

## ❓ FAQ

### How does it work?
MedalSync uses **NTFS hard links** — a second directory entry pointing to the exact same file data. This means:
- No extra disk space used
- Deleting the hardlink does NOT delete your original clip
- Deleting the original does NOT affect the hardlink (data persists until all links are removed)

### Does it use resources?
Virtually none. It uses Windows' built-in `FileSystemWatcher` (`ReadDirectoryChangesW`) which is an OS-level notification — **0% CPU when idle**. It only wakes up when a new clip is saved.

### What about files with the same name?
Clips are prefixed with their game folder name: `Fortnite_clip1.mp4`, `Valorant_clip2.mp4`, etc. This prevents naming conflicts.

### Can I use different drives?
Source and sync folder must be on the **same NTFS drive**. Hard links are a filesystem feature that only works within the same volume. The settings dialog will warn you if you try to use different drives.

### Is it safe?
Yes. MedalSync never modifies or moves your original clips. It only creates additional directory entries (hardlinks) in the sync folder.

---

## 🛠️ Build from Source

```bash
# Clone
git clone https://github.com/YOUR_USERNAME/MedalSync.git
cd MedalSync

# Build
dotnet build MedalSync/MedalSync.csproj

# Publish self-contained .exe
dotnet publish MedalSync/MedalSync.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

---

## 📄 License

MIT License — do whatever you want with it.
