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
```text
D:\Clips\
├── Fortnite\
│   ├── clip1.mp4
│   └── clip2.mp4
├── Valorant\
│   └── ace.mp4
└── Rainbow Six Siege\
    └── clutch.mp4
```

Medal can only read clips from a **flat folder** (no subfolders). So none of your clips show up if you just add `D:\Clips` to Medal.

## ✅ The Solution

MedalSync watches your NVIDIA clips folder and automatically creates **hardlinks** in a separate, flat sync folder that Medal can easily read:

```text
D:\MedalSync\
├── Fortnite_clip1.mp4          → D:\Clips\Fortnite\clip1.mp4
├── Fortnite_clip2.mp4          → D:\Clips\Fortnite\clip2.mp4
├── Valorant_ace.mp4            → D:\Clips\Valorant\ace.mp4
└── Rainbow Six Siege_clutch.mp4 → D:\Clips\Rainbow Six Siege\clutch.mp4
```

- **Zero extra disk space** — hardlinks point to the same data on disk.
- **Zero CPU usage** when idle — uses OS-level file system notifications.
- **Real-time sync** — new clips appear instantly.
- **No admin rights required** — hardlinks don't need elevated privileges.
- **Runs silently** in the system tray.

---

## 📥 Download

Go to [**Releases**](../../releases/latest) and download `MedalSync-Setup.exe`.

> **Note:** The `.exe` is self-contained — no .NET installation is required!

---

## 🚀 Quick Setup Guide

### 1. Start MedalSync
Run `MedalSync.exe`. 
On your first startup, a prompt will appear asking you to configure your folders. Afterwards, a gold **M** icon will appear in your system tray (bottom right corner of your screen).

### 2. Configure Your Folders
In the setup window:
1. Select your **NVIDIA Clips Folder** (e.g. `D:\Videos\Clips`). This is where GeForce Experience/ShadowPlay saves your recordings.
2. By default, MedalSync will automatically create a `MedalSync` folder right next to your NVIDIA clips folder (e.g., `D:\Videos\MedalSync`).
3. *(Optional)* If you want a custom sync folder location, check **Use custom sync folder** and select your desired path.
4. Click **Save**.

> ⚠️ **Important:** Both the NVIDIA clips folder and the Medal sync folder must be on the **same drive** (this is a hard requirement for Windows hardlinks).

### 3. Add the Sync Folder to Medal
Now you need to tell Medal to read the clips from the newly created sync folder.
1. Open the **Medal** app.
2. Go to **Settings** (gear icon) → **Recorder** (or **Clips & Recording** depending on your version).
3. Scroll down to **Capture Folder Location** or **Additional Clips Folders**.
4. Click **Add Folder** and select the **Medal Sync Folder** you created in Step 2 (e.g. `D:\Videos\MedalSync`).

### 4. Done! 🎉
All your existing clips are synced immediately, and new ones will sync in real-time. You can now see and upload all your ShadowPlay clips directly from Medal.

---

## 🖱️ Tray Menu

Right-click the gold **M** icon in your system tray to access the menu:

| Option | Description |
|--------|-------------|
| ⏸ Pause / ▶ Resume | Pause or resume file watching |
| 🔄 Resync | Force a full resync of all clips |
| 📂 Open sync folder | Open the flat Medal sync folder in Explorer |
| 📂 Open clips folder | Open your NVIDIA clips folder in Explorer |
| ⚙ Settings | Change your source/sync folder paths |
| ☑ Start with Windows | Toggle automatic startup when your PC boots |
| 🌐 Language | Switch the app language between English and Deutsch |
| ❌ Exit | Close MedalSync |

**Double-click** the tray icon to quickly open the sync folder.

---

## ❓ FAQ

### How does it work?
MedalSync uses **NTFS hard links** — a second directory entry pointing to the exact same file data on your hard drive. This means:
- No extra disk space is used.
- If you delete a clip from the sync folder (e.g., in Medal), MedalSync will also delete the original clip.
- If you delete the original clip, MedalSync removes the sync link.

### Does it use my PC's resources?
Virtually none. It uses Windows' built-in `FileSystemWatcher` (`ReadDirectoryChangesW`) which is an OS-level notification — **0% CPU when idle**. It only wakes up for a split second when a new clip is saved.

### What about files with the same name?
Clips are automatically prefixed with their game folder name (e.g. `Fortnite_clip1.mp4`, `Valorant_clip2.mp4`). This completely prevents naming conflicts when combining them into a single folder.

### Can I use different drives for my clips and the sync folder?
No. The source and sync folder must be on the **same NTFS drive**. Hard links are a filesystem feature that only works within the same volume. The settings dialog will warn you if you try to use different drives.

### Is it safe?
Yes. MedalSync only creates hardlinks in the sync folder and removes them when the original file is deleted. If you delete a clip from the sync folder, MedalSync will delete the original clip as well.

---

## 🛠️ Build from Source

```bash
# Clone
git clone https://github.com/Reskeyo/MedalSync.git
cd MedalSync

# Build
dotnet build MedalSync/MedalSync.csproj

# Publish self-contained .exe
dotnet publish MedalSync/MedalSync.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

### 📦 Installer (Inno Setup)
Build a simple installer that runs the app after setup and can optionally add autostart:

```bash
# Install Inno Setup, then run:
iscc installer\MedalSync.iss
```

The installer will be written to `dist\MedalSync-Setup.exe`.

> **Note:** Without a code-signing certificate, Windows SmartScreen may still show a warning.

---

## 📄 License

MIT License — do whatever you want with it.
