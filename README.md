# PdfToMarkdown

將 PDF（以台灣上市櫃公司合併財務報告為主要對象）轉換為 Markdown 的主控台程式。
.NET Framework 4.8，x64。

## 功能

- **文字頁**：依座標重建行與段落，處理中文字距、頁碼、跨頁段落、標題層級（「一、」「(一)」）與條列清單
- **表格**：
  - 無框線的會計格式表格：以垂直空白走廊與欄位底線切欄，合併多列表頭與折行儲存格
  - 有完整格線的附表（A3 橫式）：依格線切欄，跨欄表頭展開到每個欄位
  - 數值欄靠右對齊；金額保留原文，只整理括號內側空白
- **掃描頁**（會計師核閱報告、四大報表）：
  - 合成並轉正掃描影像，輸出為 `<檔名>_images/pNN.jpg` 並在 Markdown 中引用
  - 以 Tesseract OCR 辨識後，與文字頁走相同的段落、表格流程
  - 每頁註記平均辨識信心與低信心字詞數

## 建置

需要 .NET SDK（用來建置 net48 專案）。

```bash
dotnet build PdfToMarkdown.sln -c Release
```

執行檔位於 `src/PdfToMarkdown/bin/x64/Release/net48/PdfToMarkdown.exe`。
Tesseract 原生程式庫需要 Visual C++ 2015–2022 可轉散發套件（x64）。

## OCR 語言資料

掃描頁 OCR 需要繁體中文與英文的語言資料（約 27 MB，不納入版控）：

```bash
powershell -ExecutionPolicy Bypass -File tools/get-tessdata.ps1
```

預設下載 `tessdata_best`（較準確），下載到 `tessdata/`。若速度不足可加 `-Variant tessdata_fast`。
程式依序尋找：`--tessdata` 指定的資料夾、`TESSDATA_PREFIX` 環境變數、執行檔旁的 `tessdata`、目前目錄的 `tessdata`。
找不到時會略過 OCR，掃描頁只輸出影像。

## 使用方式

```bash
PdfToMarkdown.exe doc/202601_1582.pdf -o out/202601_1582.md
```

```bash
PdfToMarkdown.exe doc -o out
```

| 選項 | 說明 |
|---|---|
| `-o, --output <path>` | 輸出 Markdown 檔；輸入為資料夾時為輸出資料夾 |
| `--tessdata <dir>` | Tesseract 語言資料資料夾 |
| `--no-ocr` | 不對掃描頁進行 OCR（較快） |
| `--dump-lines` | 改為輸出每頁的行、座標與格線（調整版面分析門檻用） |
| `-h, --help` / `-v, --version` | 說明／版本 |

結束代碼：0 成功、1 參數錯誤、2 找不到輸入、3 轉換失敗（批次中任一檔失敗）。

## 開發

```bash
dotnet test PdfToMarkdown.sln
```

- `tests/golden/*.md` 是 `doc/` 樣本的預期輸出（不含 OCR，以保持結果穩定）。
  轉換邏輯有意變更時，執行 `tools/update-golden.ps1` 更新，並在 commit 前檢視差異。
- OCR 冒煙測試在沒有 `tessdata/` 時會略過。

## 已知限制

- OCR 品質受掃描影像影響：部分數字仍會辨識錯誤（例如漏位數、錯字），請對照輸出的影像核對。
  每份樣本 OCR 約需 70–90 秒。
- 分散對齊且字距等於走廊門檻的項目名稱，偶爾會被切成兩欄（例如外幣表的「新 | 台幣」）。
- 表格跨頁（「接次頁／承前頁」）時輸出為兩個表格，不會合併。
- 版面規則以樣本財報調校，其他類型的 PDF 可能需要調整 `Layout/` 中的門檻。
