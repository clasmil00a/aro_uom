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
foreach ($file in @($dll,$idLang,$enLang,$migration,$readme)) {
    if (!(Test-Path -LiteralPath $file)) { throw "Payload installer tidak ditemukan: $file" }
}
$sources = Get-ChildItem $installer -Filter '*.cs' | ForEach-Object { $_.FullName }
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe /platform:anycpu "/out:$output" "/win32manifest:$(Join-Path $installer 'App.manifest')" "/resource:$dll,UomPayload.Aronium.Pos.MultiUom.dll" "/resource:$idLang,UomPayload.id.lang" "/resource:$enLang,UomPayload.en.lang" "/resource:$migration,UomPayload.1_0__Add_MultiUom_Transaction_Details.sql" "/resource:$readme,UomPayload.README_MultiUOM.txt" $sources
if ($LASTEXITCODE -ne 0) { throw "Build installer gagal: $LASTEXITCODE" }
$hash = (Get-FileHash $output -Algorithm SHA256).Hash
$manifest = [ordered]@{ module='Aronium.Pos.MultiUom'; installerVersion='1.0.2'; requiredAroniumVersion='1.51.1.0'; payloadSha256=$hash }
$manifest | ConvertTo-Json | Set-Content (Join-Path $installer 'payload-manifest.json') -Encoding UTF8
$toolsOutput = Join-Path $app 'Tools\Install-Aronium-UOM.exe'
Copy-Item -LiteralPath $output -Destination $toolsOutput -Force
Write-Output "Built $output"
Write-Output "SHA256 $hash"
