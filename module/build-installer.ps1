$ErrorActionPreference = 'Stop'
$module = Split-Path -Parent $MyInvocation.MyCommand.Path
$app = 'C:\Program Files\Aronium'
$installer = Join-Path $module 'Installer'
$output = Join-Path $module 'Install-Aronium-UOM.exe'
$dll = Join-Path $module 'Aronium.Pos.MultiUom.dll'
$idLang = Join-Path $module 'Lang\id.lang'
$enLang = Join-Path $module 'Lang\en.lang'
$migration = Join-Path $app 'Migrations\Lite\Aronium.Pos.MultiUom\1_0__Add_MultiUom_Transaction_Details.sql'
$readme = Join-Path $app 'Tools\README_MultiUOM.txt'
$receiptPatched = Join-Path $installer 'Payloads\Aronium.Pos.ReceiptPrinter.patched.payload'
foreach ($file in @($dll,$idLang,$enLang,$migration,$readme,$receiptPatched)) {
    if (!(Test-Path -LiteralPath $file)) { throw "Payload installer tidak ditemukan: $file" }
}
$receiptHash = (Get-FileHash -LiteralPath $receiptPatched -Algorithm SHA256).Hash
if ($receiptHash -ne 'EE06C72209FCF759695321E31026CA75C5E7804AA2ACEB1CEFF5C4D4BFF59E58') {
    throw "Hash payload ReceiptPrinter tidak valid: $receiptHash"
}
$sources = Get-ChildItem $installer -Filter '*.cs' | ForEach-Object { $_.FullName }
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe /platform:anycpu "/out:$output" "/win32manifest:$(Join-Path $installer 'App.manifest')" "/resource:$dll,UomPayload.Aronium.Pos.MultiUom.dll" "/resource:$idLang,UomPayload.id.lang" "/resource:$enLang,UomPayload.en.lang" "/resource:$migration,UomPayload.1_0__Add_MultiUom_Transaction_Details.sql" "/resource:$readme,UomPayload.README_MultiUOM.txt" "/resource:$receiptPatched,ReceiptPatchPayload.Aronium.Pos.ReceiptPrinter.dll" /reference:System.Xml.dll $sources
if ($LASTEXITCODE -ne 0) { throw "Build installer gagal: $LASTEXITCODE" }
$hash = (Get-FileHash $output -Algorithm SHA256).Hash
$manifest = [ordered]@{
    module = 'Aronium.Pos.MultiUom'
    installerVersion = '1.0.5'
    requiredAroniumVersion = '1.51.1.0'
    payloadSha256 = $hash
    receiptPrinterOriginalSha256 = '74556BE1B965D1679619FD060D29F5ACD40C24B66935C77A1C223483971621A0'
    receiptPrinterPreviousPatchedSha256 = '2AC1C0458802AFB297FADDC156CFB92FCAADFF7AB033F592222C3DF9F22DDFF9'
    receiptPrinterPatchedSha256 = $receiptHash
}
$manifest | ConvertTo-Json | Set-Content (Join-Path $installer 'payload-manifest.json') -Encoding UTF8
$toolsOutput = Join-Path $app 'Tools\Install-Aronium-UOM.exe'
Copy-Item -LiteralPath $output -Destination $toolsOutput -Force
Write-Output "Built $output"
Write-Output "SHA256 $hash"
Write-Output "ReceiptPrinter patched SHA256 $receiptHash"
