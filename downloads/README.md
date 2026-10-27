# AutoRender Windows x64 — OCR preview v0.3

Self-contained .NET 10/WPF build from source `f947260b0bdf3e0749fef16aaa109a3b3ac79390`. Download `AutoRender-win-x64.zip`, extract into a new folder, then run `AutoRender.App.exe`. No Python or .NET SDK installation is required. Windows 10 version 2004+ / Windows 11 x64 and a Windows OCR language pack are required.

The title must show **OCR thử nghiệm v0.3**. Use the included `profiles/capcut-9.6.vi.visual.json`. Click **Kiểm tra OCR** first, then test one short project (for example 1027) before a long queue. See the included `docs/VISUAL-OCR.md`.

This is an experimental adapter based on supplied CapCut 9.6 Vietnamese screenshots. It reads project names and controls with Windows OCR, sends input with Win32, verifies editable values by clipboard read-back, and uses UI Automation only for window metadata. 44 logic tests pass; actual Windows/CapCut export has not been verified. The earlier UIA template remains incomplete. If a field is read-only or a control cannot be recognized, the queue stops before export.

SHA-256: `f2bc1009fd1ed26e56bc06cc109c43dfee3ad76ceed0e9f3a4a29a09cbd07893`.
