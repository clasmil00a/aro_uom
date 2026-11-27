$ErrorActionPreference = 'Stop'
$folder = Split-Path -Parent $MyInvocation.MyCommand.Path
$payload = Join-Path (Split-Path -Parent $folder) 'Installer\Payloads\Aronium.Pos.ReceiptPrinter.patched.payload'
$output = Join-Path $folder 'Aronium-ReceiptPrinter-PaymentTypes-Patcher.exe'
if (!(Test-Path -LiteralPath $payload)) { throw "Payload tidak ditemukan: $payload" }
$expected = 'EE06C72209FCF759695321E31026CA75C5E7804AA2ACEB1CEFF5C4D4BFF59E58'
$hash = (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash
if ($hash -ne $expected) { throw "Hash payload tidak valid: $hash" }
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe /platform:anycpu "/out:$output" "/win32manifest:$(Join-Path $folder 'app.manifest')" "/resource:$payload,AroniumPatch.Aronium.Pos.ReceiptPrinter.dll" /reference:System.Core.dll /reference:System.Xml.dll (Join-Path $folder 'Program.cs')
if ($LASTEXITCODE -ne 0) { throw "Build patcher gagal: $LASTEXITCODE" }
Write-Output "Built $output"
Write-Output "SHA256 $((Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash)"
