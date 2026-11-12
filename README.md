# Industrial Edge Debugger

A local-first Windows desktop tool for bringing up and debugging industrial edge
devices. It connects over serial (USB-serial or Bluetooth SPP), parses incoming
frames into numeric channels, renders them as a live multi-channel waveform, and
persists everything to SQLite for later querying and export.

No cloud service, no account, no telemetry — the device log stays on the machine
that collected it.

## Features

- **Device connection** — serial, USB and Bluetooth devices behind a single
  `IDeviceComm` abstraction, with a scan/refresh device list and live status.
- **Serial configuration** — baud rate, data bits, stop bits and parity.
- **Real-time waveform rendering** — custom `DrawingContext` + `StreamGeometry`
  renderer, three channels by default, mouse-wheel zoom and drag-to-pan, per-channel
  visibility toggles.
- **Acquisition pipeline** — receive → parse → render → store, with a
  `BlockingCollection` buffer so background threads do the parsing work and the UI
  thread only renders.
- **SQLite logging** — start/stop recording to `device_logs.db`, paged queries,
  filtering by time range, device, data type and keyword, CSV export, and a detail
  view for a single record.
- **Configuration** — settings auto-saved as JSON and reloaded on startup.

## Status

Early but usable: the core data path (connect → stream → plot → record → query) is
implemented and runs. Known gaps:

- Firmware flashing is **not** implemented.
- USB and Bluetooth transports currently emit **simulated** sample data; the serial
  path talks to real hardware.
- The UI is in Chinese. Localization is on the roadmap.
- No automated test suite yet.

## Download

Grab `IndustrialEdgeDebugger-v0.1.0-win-x64.zip` from
[Releases](https://github.com/karlmoriaty/IndustrialEdgeDebugger/releases), unzip
and run `IndustrialEdgeDebugger.App.exe`. The build is self-contained (no .NET
runtime install required) and single-file.

Settings are written to `%APPDATA%\IndustrialEdgeDebugger\appsettings.json`; the
log database is created next to the executable as `device_logs.db`.

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/karlmoriaty/IndustrialEdgeDebugger
cd IndustrialEdgeDebugger
dotnet run --project IndustrialEdgeDebugger.App
```

To produce the portable single-file build:

```bash
dotnet publish IndustrialEdgeDebugger.App/IndustrialEdgeDebugger.App.csproj -c Release -o publish
```

## Usage

1. Pick a device from the list (refresh if it is not there yet), set the serial
   parameters, then **Connect**.
2. Incoming frames are parsed into channels and drawn live. Use the mouse wheel to
   zoom the time axis and drag to pan; the checkboxes to the right toggle channels.
   **Waveform Simulation** generates synthetic data when no hardware is attached.
3. **Start Recording** writes every parsed point to SQLite; **Stop Recording** pauses
   it.
4. **Log Query** opens the history window: filter, page through results, export CSV,
   or double-click a row for the raw record.
5. **Settings** edits the serial, waveform and window defaults.

## Project layout

```
IndustrialEdgeDebugger.sln
IndustrialEdgeDebugger.App/          WPF application (MVVM)
  Controls/WaveformRenderer.cs       custom waveform control
  ViewModels/                        main, channel, log query, settings
  Views/                             MainWindow, LogQueryWindow, SettingsWindow
  Services/                          JSON configuration service
IndustrialEdgeDebugger.Comm/         transport + storage library
  IDeviceComm.cs, DeviceCommFactory.cs
  SerialPortManager.cs, UsbDeviceManager.cs, BluetoothDeviceManager.cs
  Services/SqliteLogStorage.cs       log persistence
  Models/                            DeviceLog, AppSettings
```

## Roadmap

- Firmware flashing (needs a defined image format and protocol).
- Real USB HID / Bluetooth GATT transports to replace the simulated ones.
- Analysis aids: FFT, triggers, cursors.
- Additional export formats (Excel, MATLAB).
- English/Chinese UI localization.

## License

MIT
