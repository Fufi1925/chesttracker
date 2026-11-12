# Third-party components in the Windows package

- Tesseract .NET wrapper 5.2.0, Charles Weld: https://github.com/charlesw/tesseract — Apache License 2.0.
- Tesseract OCR engine: https://github.com/tesseract-ocr/tesseract — Apache License 2.0.
- Official `tessdata_fast` German/English models, revision `87416418657359cb625c412a48b6e1d6d41c29bd`: https://github.com/tesseract-ocr/tessdata_fast — Apache License 2.0.
- Leptonica: http://www.leptonica.org/about-the-license.html — BSD-style license.
- Microsoft Visual C++ runtime app-local redistributable components. See https://learn.microsoft.com/cpp/windows/redistributing-visual-cpp-files and https://learn.microsoft.com/visualstudio/releases/2022/redistribution . These remain Microsoft-licensed components, not project source code.
- Microsoft .NET runtime and Windows SDK projections: Microsoft licenses provided by their respective runtime distributions.

The build downloads OCR models from the official pinned revision and verifies SHA-256. No game graphics or game files are included.
