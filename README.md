# Fluent Terminal

[![Build status](https://ci.appveyor.com/api/projects/status/github/jumptrading/FluentTerminal?branch=master&svg=true)](https://ci.appveyor.com/project/daviesalex/fluentterminal)

A Terminal Emulator based on UWP and web technologies.

## Features

- Terminal for PowerShell, CMD, WSL or custom shells
- Supports tabs and multiple windows
- Theming and appearance configuration
- Import/Export themes
- Import iTerm themes
- Fullscreen mode
- Editable keybindings
- Search function
- Configure shell profiles to quickly switch between different shells
- Explorer context menu integration (Installation script can be found [here](https://github.com/felixse/FluentTerminal/tree/master/Explorer%20Context%20Menu%20Integration))

## Screenshots

![Terminal window](Screenshots/terminal.jpg)
![Settings window](Screenshots/settings.jpg)

## Up Next

- ~~Launch shell profile with a custom defined keybinding~~
- Copy&Paste options
- Improved tabs
- Split screen support

## How to install (as an end-user)

### Prerequisite
- You need to update to Fall Creators Update or later.

### Chocolatey package manager installation

- Install [Chocolatey](https://chocolatey.org/)
- From an elevated/admin shell, execute `choco install fluent-terminal`

### Bundled install script

- Download and extract the latest [release](https://github.com/felixse/FluentTerminal/releases).
- If not already present, download [`Install.ps1`](Install.ps1) to the extracted directory.
- Right-click on `Install.ps1`, and choose "Run with Powershell".
- The installer script will walk you through the rest of the process.

### Manual install

- Download the latest [release](https://github.com/felixse/FluentTerminal/releases)
- [Enable sideloading apps](https://www.windowscentral.com/how-enable-windows-10-sideload-apps-outside-store)
  - Alternatively, [enable developer mode](https://docs.microsoft.com/en-US/windows/uwp/get-started/enable-your-device-for-development) if you plan to do UWP app development. **For most users that do not intend to do UWP app development, developer mode will not be necessary.**
- Install the *.cer file into `Local Machine` -> `Trusted Root Certification Authorities`
  - This will require administrator elevation. If you installed the certificate and did not have to elevate, you likely installed it into an incorrect certificate store.

![Right-Click then choose Install Certificate](Screenshots/right-click_install-certificate.png)

![Install Certificate into Local Machine](Screenshots/install-certificate_local-machine.png)

- double click the *.appxbundle to sideload the app with `App Installer` or execute in PowerShell with admin rights:
```powershell
Add-AppxPackage -Path FluentTerminal.App_x.x.xx.0_x86_x64.appxbundle
```
- **Optional:** Install Context menu integration from [here](https://github.com/felixse/FluentTerminal/tree/master/Explorer%20Context%20Menu%20Integration)

## How to set up a development environment
Please refer to [this Wiki page](https://github.com/felixse/FluentTerminal/wiki/How-to-set-up-a-development-environment)
