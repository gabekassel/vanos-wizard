# Kassel Performance — S54 VANOS Tester

A Windows desktop application for testing the **VANOS** system on the **BMW S54** engine
(MSS54 DME) and acquiring **live oil and coolant temperatures**, communicating with the car
through **EDIABAS**.

- Automatically finds the diagnostic **COM port** (ranks K+DCAN / FTDI / OBD cables first).
- Runs the VANOS function test and displays **all** returned results in a table.
- Streams **live oil + coolant temperature** to a chart and large readouts.

---

## Requirements

This is a **Windows-only** application — it cannot be built or run on macOS/Linux.

To **build** you need (on a Windows machine):
- [.NET SDK](https://dotnet.microsoft.com/download) (any recent version), **or** Visual Studio 2019/2022.
- The project targets **.NET Framework 4.8** and **x86**. x86 is mandatory because `api32.dll`
  (the EDIABAS runtime DLL, the counterpart of `ediabas.lib`) is 32-bit.

To **run** you also need:
- An **EDIABAS installation** on the machine. The app loads `api32.dll` at runtime; without
  EDIABAS you will get a `DllNotFoundException` when connecting.
- A **K+DCAN / OBD diagnostic cable** with its driver installed.
- The car's ignition switched to **KL15 (on)** before connecting.

---

## Do I need to compile it?

Yes — the project ships as C# source, not as a ready-to-run `.exe`. You compile it **once** on a
Windows machine to produce `S54VanosTester.exe`, then run that. You only recompile when the code
changes.

---

## Build

### Option A — command line
```
dotnet build S54VanosTester.csproj -c Release
```
Output: `bin\Release\net48\S54VanosTester.exe`

### Option B — Visual Studio
Open `S54VanosTester.csproj`, then:
- **Ctrl+Shift+B** to build, or
- **F5** to build and run.

> Build on a machine that has EDIABAS installed.

---

## Run

1. Plug in the diagnostic cable and switch the ignition to **KL15 (on)**.
2. Launch `S54VanosTester.exe`.
3. Click **Auto-Connect** (or pick a port and click **Connect**).
4. Click **Run VANOS Test** to run the test and view results.
5. Click **Start Live** to begin live oil/coolant temperature acquisition.

`S54VanosTester.exe` must stay alongside `appsettings.json` and `kp.ico` — the build copies both
into the output folder automatically.

---

## Configuration — `appsettings.json`

The ECU, job, and result names default to the commonly referenced MSS54 identifiers but are
externalised so they can be matched to your exact SGBD. If a VANOS result or a temperature comes
back empty, verify these names against your `MSS54.PRG` in EDIABAS **Tool32**.

| Key | Purpose | Default |
|---|---|---|
| `Ecu` | SGBD / ECU name (`MSS54`, sometimes `MSS54HP`) | `MSS54` |
| `EdiabasInterface` | EDIABAS interface | `STD:OBD` |
| `IdentJob` | Identification job used to confirm a COM port reaches the DME | `IDENT` |
| `VanosTestJob` | VANOS function/adjustment test job | `STEUERN_VANOS_TEST` |
| `VanosTestParameters` | Optional parameters for the test job | _(empty)_ |
| `VanosStatusJob` | Optional job to read back VANOS adjustment values | `STATUS_VANOS` |
| `TemperatureJob` | Job that returns coolant/oil temperatures | `STATUS_TEMPERATUR` |
| `CoolantResult` | Coolant temperature result name | `STAT_KUEHLMITTELTEMPERATUR_WERT` |
| `OilResult` | Oil temperature result name | `STAT_OELTEMPERATUR_WERT` |
| `LivePollIntervalMs` | Live polling interval (ms) | `500` |

---

## Project layout

| Path | Role |
|---|---|
| `Program.cs` | Application entry point |
| `AppSettings.cs` / `appsettings.json` | ECU/job/result configuration |
| `Ediabas/EdiabasApi.cs` | P/Invoke bindings for `api32.dll` |
| `Ediabas/EdiabasClient.cs` | Managed wrapper around the EDIABAS API |
| `Ediabas/EdiabasConfig.cs` | Locates/edits `EDIABAS.INI` for the chosen COM port |
| `Ediabas/ComPortFinder.cs` | Enumerates and ranks serial ports |
| `Vanos/VanosTester.cs` | Runs the VANOS test and builds the report |
| `Diagnostics/TemperatureReader.cs` | Reads coolant/oil temperature samples |
| `Diagnostics/DiagnosticsSession.cs` | Owns the EDIABAS worker thread |
| `UI/MainForm.*` | Main window |
| `UI/Branding.cs`, `UI/BrandHeader.cs`, `UI/AboutForm.cs`, `kp.ico` | Kassel Performance branding |

---

© Kassel Performance. For workshop diagnostic use.
