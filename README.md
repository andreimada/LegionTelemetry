# Legion Telemetry — compact overlay

A single-line always-on-top Windows overlay displaying:

`CPU 38% / 72°C | GPU 91% / 68°C | RAM 18.2 GB`

Values are live, not fixed. RAM is **used physical memory**, in GB.

## Build and run

1. Open `LegionTelemetry/LegionTelemetry.slnx` in **Visual Studio 2026 (18.x)** with the **.NET desktop development** workload and **.NET 10 SDK** installed. The project targets x64.
2. Install the signed, official [PawnIO driver](https://pawnio.eu/) for CPU temperature access. Restore NuGet packages, then run the project and accept its administrator prompt. Restart the app after installing the driver. For debugging, start Visual Studio as administrator.
3. The overlay opens in the top-right corner of the Windows work area.
4. Drag it with the left mouse button. Use its system tray icon's right-click menu for opacity, click-through, and exit.

## Libraries and drivers used

| Dependency | Version | Purpose | Installation |
| --- | --- | --- | --- |
| [LLT.LibreHardwareMonitorLib](https://github.com/LenovoLegionToolkit-Team/LibreHardwareMonitor) | 1.0.2 | Reads CPU/GPU usage and temperature, and used RAM. This is the Lenovo Legion Toolkit team's fork of LibreHardwareMonitor. | Restored automatically through NuGet. |
| [PawnIO](https://pawnio.eu/) | 2.2.0.0 verified locally | Provides the low-level hardware access needed for CPU temperature readings. | Install the official signed edition separately; NuGet restore does not install the driver. |
| WPF | .NET 10 | Renders the transparent, topmost overlay. | Included with the Windows desktop framework. |
| Windows Forms / System.Drawing | .NET 10 | Provides the tray icon and its context menu. | Enabled through `UseWindowsForms` in the project file. |

NuGet restores the hardware library's transitive dependencies automatically; no additional app plugins need to be installed manually.

Download PawnIO through its official website above and use the signed edition. The installer and installed utilities were verified with valid signatures from `namazso.eu`; the installed driver was verified through its catalog signature from **Microsoft Windows Hardware Compatibility Publisher**. The unsigned unrestricted edition is not required.

The application requests administrator access at launch. After installing PawnIO, restart the application so the hardware library can initialize the driver.

## Notes

- Uses LLT.LibreHardwareMonitorLib to poll hardware once per second.
- Selects the NVIDIA GPU ahead of integrated graphics, when available.
- CPU temperature requires PawnIO and administrator access. The overlay displays "Install PawnIO" when the driver is missing. Other unavailable sensor values show N/A.
- Works on the desktop and over many borderless-windowed games. Ordinary WPF topmost windows cannot guarantee visibility over exclusive fullscreen games.
- No Windows Service is needed; this is a tray/desktop application.
- Verified with `dotnet build LegionTelemetry/LegionTelemetry.slnx` using the .NET 10 SDK.

## Framework

Targets **.NET 10 LTS** (`net10.0-windows`) and Visual Studio 2026 with the .NET desktop development workload.
