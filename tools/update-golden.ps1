# 重新產生 tests/golden/*.md。
# 轉換邏輯有意變更時執行，並在 commit 前檢視 golden 檔的差異。
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$env:UPDATE_GOLDEN = '1'
try
{
    dotnet test (Join-Path $root 'PdfToMarkdown.sln') --filter 'FullyQualifiedName~GoldenFileTests'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally
{
    Remove-Item Env:UPDATE_GOLDEN
}

git -C $root diff --stat -- tests/golden
