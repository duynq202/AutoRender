# AutoRender Windows x64 — OCR preview v0.3.1

Self-contained .NET 10/WPF build from source `727dda15a43a3ac04a2a0e136b6b13fa3e829755`. Extract the complete ZIP into a new folder and run `AutoRender.App.exe`. Windows 10 version 2004+ / Windows 11 x64 and a Windows OCR language pack are required; no Python or .NET SDK is needed.

This release fixes the first-step click on an AI content card reported by the user. It removes the click used before scrolling and removes guessed search-icon coordinates. Project names are now located by OCR while scrolling in grid view. Before export, use **Xem vị trí project (không bấm)** to move the pointer to the planned thumbnail without opening a project. The title must show **OCR thử nghiệm v0.3.1**.

Use `profiles/capcut-9.6.vi.visual.json`. Follow `docs/VISUAL-OCR.md`. 53 logic tests passed and the Windows package built successfully. Real CapCut export remains unverified; first check the pointer preview and then run one short project.

SHA-256: `c5f02b01565f49f0413baeca691a650c5253c7f8c401bd61e0925ce3576a04bd`
