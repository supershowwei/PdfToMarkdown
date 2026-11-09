# 下載 Tesseract 語言資料（繁體中文、英文）到 tessdata/。
# 來源：https://github.com/tesseract-ocr/tessdata_best（較準確；若速度不足可改用 tessdata_fast）
param(
    [string]$Destination = (Join-Path (Split-Path -Parent $PSScriptRoot) 'tessdata'),
    [ValidateSet('tessdata_best', 'tessdata_fast')]
    [string]$Variant = 'tessdata_best'
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

foreach ($language in 'chi_tra', 'eng')
{
    $target = Join-Path $Destination "$language.traineddata"
    if (Test-Path $target)
    {
        Write-Host "已存在：$target"
        continue
    }

    $url = "https://github.com/tesseract-ocr/$Variant/raw/main/$language.traineddata"
    Write-Host "下載 $url"
    Invoke-WebRequest -Uri $url -OutFile $target -UseBasicParsing
    Write-Host ("完成：{0}（{1:N1} MB）" -f $target, ((Get-Item $target).Length / 1MB))
}
