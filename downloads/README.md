# AutoRender Windows x64

Self-contained .NET 10 / WPF package built from source commit `85250101dd3e09cb845a9722438d5f0e9da9e37f`.

Download `AutoRender-win-x64.zip`, extract all files, then run `AutoRender.App.exe`. No .NET SDK or Python installation is required.

This is an inspection/development build. CapCut 9.6.0 Vietnamese export integration is not verified. The included UI profile is intentionally incomplete and prevents export until the target PC has been inspected and a working adapter/profile validated. Use the UI inspection instructions included in the ZIP.

Check `SHA256SUMS.txt` to verify the package.

Version 0.2.0 — window inspection v2. The old collector could select a 45x17 QML tooltip instead of the main CapCut window. This build enumerates top-level windows, excludes tooltips/tool/small windows, and records candidates and inspected window handles. The title must show “Khảo sát v2”. Collect a fresh home-screen JSON first. 26 core/profile/window selection tests passed; actual CapCut integration remains unverified.
