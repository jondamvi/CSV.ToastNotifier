# CSV.ToastNotifier

A lightweight Windows toast notification utility for environments with PowerShell language mode restrictions.

## 🎯 Purpose

This tool enables Windows toast notifications in corporate environments where PowerShell is restricted to Constrained Language Mode, preventing access to Windows.UI.Notifications .NET objects. It provides a native executable solution that works without administrative privileges.

## ✨ Features

- **No Admin Required** - Runs under standard user privileges
- **Minimal Size** - ~20KB executable without embedded icon
- **Secure** - Input validation, path sanitization, XML injection prevention
- **Native Templates** - Uses proper Windows toast templates (ToastImageAndText01/02, ToastText01/02)
- **Customizable** - Title, message, icon, audio, and notification types
- **Windows 11 Ready** - Tested on Windows 11 x64 v23H2

## 📋 Requirements

### Runtime
- Windows 10/11 (tested on Windows 11 x64 v23H2)
- .NET Framework 4.8 (pre-installed on Windows 10/11)
- Standard user account (no admin privileges required)

### Build
- .NET Framework 4.8
- Windows SDK 10.0.26100.0 or compatible version
- 64-bit Windows OS

## 🚀 Usage

### Basic Notification
```cmd
CSV.ToastNotifier.exe -m "Hello World" -s "MyApp"
```

### With Title
```cmd
CSV.ToastNotifier.exe -m "Build completed" -s "BuildSystem" -t "Success"
```

### Custom Icon and Sound
```cmd
CSV.ToastNotifier.exe -m "Meeting starting" -s "Calendar" -t "Reminder" -i "C:\Icons\meeting.png" -a "C:\Sounds\chime.wav"
```

### Silent Notification
```cmd
CSV.ToastNotifier.exe -m "Background task finished" -s "Backup" --muted
```

### Long Reminder
```cmd
CSV.ToastNotifier.exe -m "Important deadline" -s "TaskManager" --type reminder
```

### Command Line Arguments

| Argument | Short | Description | Max Length |
|----------|-------|-------------|------------|
| `--message` | `-m` | Notification message (required) | 256 chars |
| `--source` | `-s` | Application name (required) | 32 chars |
| `--title` | `-t` | Notification title | 32 chars |
| `--icon` | `-i` | Path to PNG icon file | 259 chars |
| `--audio` | `-a` | Path to WAV audio file | 259 chars |
| `--muted` | | Disable notification sound | |
| `--type` | | Type: `default`, `reminder`, `alarm`, `incomingCall`, `urgent` | |
| `--help` | `-h`, `/?` | Show help message | |

## 🔨 Building

### Using **build.bat**:
> [!IMPORTANT]
> <u>Read before using</u> **build.bat**:
> DO NOT COPY **build.bat** to other project directory without testing and modification of **build.bat** as needed.
> Create a backup of larger project before using **build.bat** there.

> [!WARNING]
> **build.bat** will perform cleanup of the following directories:
> ```
> CSV.ToastNotifier\Source Files\bin
> CSV.ToastNotifier\Source Files\obj
> ```
> **build.bat** will perform cleanup of the following files:
> ```
> CSV.ToastNotifier\Release\CSV.ToastNotifier.pdb
> CSV.ToastNotifier\Source Files\Release\CSV.ToastNotifier.pdb
> ```
**build.bat** can perform 2 types of build: <u>LocalBuild</u> and <ins>ProjectBuild</ins>.
**build.bat** will choose build type automatically - based on location of **build.bat** file.
### <ins>Project Build</ins>:
If running **build.bat** from 'CSV.ToastNotifier\Build System' output will be in 'CSV.ToastNotifier\Release'.
### <ins>LocalBuild</ins>:
If running **build.bat** from 'CSV.ToastNotifier\Source Files' output will be in 'CSV.ToastNotifier\Source Files\Release'.

**build.bat** supports compiling project '.csproj' file with MSBuild dotnet and supports compiling directly with .NET Framework 4.8 csc.exe.

### No arguments - performs project '.csproj' compilation with MSBuild dotnet:
```cmd
Build.bat
```
### Explicit project '.csproj' compilation with MSBuild dotnet:
```cmd
Build.bat /usedotnet
```
### Explicit direct compilation with .NET Framework 4.8 csc.exe compiler:
```cmd
Build.bat /useframework
```
**build.bat** will automatically include 'CSV.ToastNotifier.manifest' and 'Icon.ico' if found near source file.

### Direct CSC Compilation
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /out:CSV.ToastNotifier.exe /platform:x64 ^
    /reference:"C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.26100.0\Windows.winmd" ^
    /reference:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.WindowsRuntime.dll" ^
    /reference:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.dll" ^
    /reference:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.InteropServices.WindowsRuntime.dll" ^
    /optimize+ /debug:pdbonly /win32manifest:CSV.ToastNotifier.manifest ^
    CSV.ToastNotifier.cs
```

### Using MSBuild
```cmd
msbuild CSV.ToastNotifier.csproj /p:Configuration=Release /p:Platform=x64
```

### Using dotnet CLI
```cmd
dotnet build CSV.ToastNotifier.csproj -c Release -o Release
dotnet publish CSV.ToastNotifier.csproj -c Release -o Release
```

## 📦 Dependencies

### Runtime
- .NET Framework 4.8
- Windows.UI.Notifications (Windows Runtime)

### Build
- Windows SDK 10.0.26100.0 or later
  - Provides Windows.winmd for Windows Runtime APIs
  - Download: https://developer.microsoft.com/windows/downloads/windows-sdk/

## 🔐 Code Signing

### With PFX Certificate
```cmd
signtool sign /f "certificate.pfx" /p "password" /t http://timestamp.digicert.com CSV.ToastNotifier.exe
```

### With Certificate Store
```cmd
signtool sign /n "Certificate Name" /t http://timestamp.digicert.com CSV.ToastNotifier.exe
```

### Using Build.bat
```cmd
Build.bat /sign /cert "certificate.pfx" /pass "password"
```

## 🔒 Security

- **Input Validation** - Length limits enforced on all text inputs
- **Path Security** - Regex validation prevents directory traversal
- **XML Safety** - Invalid XML characters removed
- **File Verification** - Binary signature validation for PNG/WAV
- **System Protection** - Blocks access to Windows system directories
- **Size Limits** - 1MB for PNG, 10MB for WAV files

## 📊 Binary Verification

### SHA-256 Hash
```
[To be updated with official release hash]
CSV.ToastNotifier.exe: ________________________________________________________________
```

Verify with:
```cmd
certutil -hashfile CSV.ToastNotifier.exe SHA256
```

## ⚙️ Technical Details

- **Language**: C# 5.0
- **Framework**: .NET Framework 4.8
- **Target**: x64 Windows
- **Templates**: ToastImageAndText01/02, ToastText01/02
- **Output Type**: WinExe (no console window)

## 📝 License

MIT License - Copyright © 2025 Jon Damvi

## ⚠️ Known Issues

- Icon shows small if Windows visual effects disabled (System → Performance → "Adjust for best appearance")
- Focus Assist blocks notifications when enabled
- Line 282 contains `iconPathValid = true;` debug code

## 🔧 Build.bat Updates Needed

Current Build.bat references Windows SDK path directly. Update line:
```batch
/reference:"C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.26100.0\Windows.winmd" ^
```
