# 建置 Release 版並發佈到 dist/：執行檔、相依組件、Tesseract 原生程式庫、
# VC++ 執行階段（app-local）與 OCR 語言資料（繁體中文、英文），複製整個資料夾即可在其他電腦執行。
param(
    [string]$Destination = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist'),
    [ValidateSet('tessdata_best', 'tessdata_fast')]
    [string]$Variant = 'tessdata_best'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'src\PdfToMarkdown\bin\x64\Release\net48'

Write-Host '== 建置 Release'
dotnet build (Join-Path $root 'PdfToMarkdown.sln') -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "建置失敗（結束代碼 $LASTEXITCODE）" }

Write-Host "== 複製程式到 $Destination"
if (Test-Path $Destination) { Remove-Item -Recurse -Force $Destination }
New-Item -ItemType Directory -Force -Path $Destination | Out-Null

Get-ChildItem $output -File |
    Where-Object { $_.Extension -in '.exe', '.dll', '.config' } |
    Copy-Item -Destination $Destination

# 程式固定以 x64 執行，只需要 x64 的原生程式庫
Copy-Item (Join-Path $output 'x64') -Destination $Destination -Recurse

# Tesseract 原生程式庫相依 VC++ 2015–2022 執行階段；隨附 app-local 版本，
# 讓沒有安裝可轉散發套件的電腦也能執行（UCRT 為 Windows 10 起內建）。
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$crt = $null
if (Test-Path $vswhere)
{
    $vs = & $vswhere -latest -products * -property installationPath
    if ($vs)
    {
        $crt = Get-ChildItem (Join-Path $vs 'VC\Redist\MSVC') -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Get-ChildItem (Join-Path $_.FullName 'x64') -Directory -Filter 'Microsoft.VC*.CRT' -ErrorAction SilentlyContinue } |
            Select-Object -First 1
    }
}

if ($crt)
{
    foreach ($dll in 'vcruntime140.dll', 'vcruntime140_1.dll', 'msvcp140.dll')
    {
        Copy-Item (Join-Path $crt.FullName $dll) -Destination $Destination
    }

    Write-Host "已隨附 VC++ 執行階段：$($crt.FullName)"
}
else
{
    Write-Warning '找不到 Visual Studio 的 VC++ 可轉散發檔，目標電腦需自行安裝 Visual C++ 2015–2022 可轉散發套件（x64）。'
}

Write-Host '== 準備 OCR 語言資料'
# 先下載到專案的 tessdata/ 當快取（已存在則略過），再複製到發佈資料夾
$cache = Join-Path $root 'tessdata'
& (Join-Path $PSScriptRoot 'get-tessdata.ps1') -Destination $cache -Variant $Variant
$tessdata = Join-Path $Destination 'tessdata'
New-Item -ItemType Directory -Force -Path $tessdata | Out-Null
foreach ($language in 'chi_tra', 'eng')
{
    Copy-Item (Join-Path $cache "$language.traineddata") -Destination $tessdata
}

Copy-Item (Join-Path $root 'README.md') -Destination $Destination

$version = (Get-Item (Join-Path $Destination 'PdfToMarkdown.exe')).VersionInfo.ProductVersion
$size = (Get-ChildItem $Destination -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("== 完成：PdfToMarkdown {0}，{1}（{2:N1} MB）" -f $version, $Destination, $size)
