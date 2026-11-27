$ErrorActionPreference = 'Stop'
$folder = Split-Path -Parent $MyInvocation.MyCommand.Path
$payload = Join-Path (Split-Path -Parent $folder) 'Installer\Payloads\Aronium.Pos.ReceiptPrinter.patched.payload'
$output = Join-Path $folder 'Aronium-ReceiptPrinter-PaymentTypes-Patcher.exe'
if (!(Test-Path -LiteralPath $payload)) { throw "Payload tidak ditemukan: $payload" }
$expected = '2AC1C0458802AFB297FADDC156CFB92FCAADFF7AB033F592222C3DF9F22DDFF9'
$hash = (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash
if ($hash -ne $expected) { throw "Hash payload tidak valid: $hash" }
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe /platform:anycpu "/out:$output" "/win32manifest:$(Join-Path $folder 'app.manifest')" "/resource:$payload,AroniumPatch.Aronium.Pos.ReceiptPrinter.dll" /reference:System.Core.dll /reference:System.Xml.dll (Join-Path $folder 'Program.cs')
if ($LASTEXITCODE -ne 0) { throw "Build patcher gagal: $LASTEXITCODE" }
Write-Output "Built $output"
Write-Output "SHA256 $((Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash)"
