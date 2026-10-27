# AutoRender Windows x64 — OCR preview v0.3.2

Source: `3149e0e121fe0738a21550101f8d160640331815`. Extract the complete ZIP into a new folder and run `AutoRender.App.exe`. Windows 10 version 2004+ / Windows 11 x64 and a Windows OCR language are required. No Python or .NET SDK installation is needed.

Version 0.3.2 supports both grid and list layouts. It fixes the reported case where OCR read `1027` but the locator rejected it: the actual project list has the name, file size, and duration in separate columns on the same row, and the Vietnamese section heading was read as `DIJ ån`. The new list target stays on that row; the old grid-only offset could point at a different project above it. The locator reads the current viewport before scrolling and records why a recognized name was rejected.

Use `profiles/capcut-9.6.vi.visual.json`. Import a single short project first, select its row, and click **Xem vị trí project (không bấm)**. Confirm the pointer is in the correct row, then use **Chạy / Tiếp tục**. **Kiểm tra OCR** reads text only.

73 logic tests passed and the Windows build/publish completed. The regression tests use positions/text from the user's log with representative text boxes. Real end-to-end export still needs validation on the target PC. Details: `docs/VISUAL-OCR.md`.

SHA-256: `3e5a625664be91366f7d2cd0bdb5349c9e7b45d1dc062e66151df994226d7e8f`
