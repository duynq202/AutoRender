# RenderBatch — template matching rebuild

Source: `5098898ee333cf8df81718710be1580f8fe6a2c1` on rebuild/rendercapcut-template. Extract the complete ZIP and run **RenderBatch.Windows.exe**. The .NET runtime and OpenCV native image matching library are included. This is the new application, not the former OCR prototype.

**Required first step:** teach templates from the actual CapCut 9.6 UI on your PC using Học ảnh / điểm bấm. Use Tìm ảnh, chỉ trỏ chuột to verify before automation. Templates are NOT preconfigured; the English images from the reference executable were not reused as production settings. See the included docs/TEACHING.md.

23 tests passed, including native OpenCV matching and queue failure checks. The Windows build/publish succeeded. Real Windows GUI calibration and end-to-end CapCut export are not yet verified. Use one short project first.

SHA-256: `861f295f40a92943c7a192934d1e4112b17c96f6247c38a42bf3df30bebdb372`
