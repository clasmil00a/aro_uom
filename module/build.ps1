$ErrorActionPreference = 'Stop'
$module = Split-Path -Parent $MyInvocation.MyCommand.Path
$app = 'C:\Program Files\Aronium'
$out = Join-Path $module 'Aronium.Pos.MultiUom.dll'
$sources = Get-ChildItem (Join-Path $module 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }
$refs = @(
  "$app\Aronium.Modularity.dll",
  "$app\Aronium.Pos.Core.dll",
  "$app\Aronium.Pos.Facade.dll",
  "$app\Aronium.Pos.Common.dll",
  "$app\log4net.dll",
  'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll',
  'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationFramework.dll',
  'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll',
  'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll',
  'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Data.dll',
  'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Core.dll'
)
$args = @('/nologo','/target:library',"/out:$out") + ($refs | ForEach-Object { "/reference:$_" }) + $sources
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' $args
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
Write-Output "Built $out"
