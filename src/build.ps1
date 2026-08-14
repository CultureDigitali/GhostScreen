# ============================================================
# GhostScreen 95 - build script
# Compiles the single-file GhostScreen.exe (no dependencies
# beyond the built-in .NET Framework 4.x on Windows).
#
# Usage:  powershell -ExecutionPolicy Bypass -File build.ps1
# ============================================================
$ErrorActionPreference = 'Stop'

$srcDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $srcDir
$drvDir  = Join-Path $rootDir 'drivers'
$assetDir = Join-Path $rootDir 'assets'
$relDir  = Join-Path $rootDir 'releases'
New-Item -ItemType Directory -Force -Path $relDir | Out-Null

$csc = "$env:windir\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { $csc = "$env:windir\Microsoft.NET\Framework\v4.0.30319\csc.exe" }

$out = Join-Path $relDir 'GhostScreen-1.0.0.exe'

Write-Host "Compilo GhostScreen 95 -> $out"
& $csc /nologo /target:winexe /optimize+ /platform:anycpu `
    /win32manifest:$(Join-Path $srcDir 'app.manifest') `
    /win32icon:$(Join-Path $assetDir 'icon.ico') `
    /out:$out `
    /resource:$(Join-Path $drvDir 'mttvdd.inf'),Res.mttvdd.inf `
    /resource:$(Join-Path $drvDir 'MttVDD.cat'),Res.MttVDD.cat `
    /resource:$(Join-Path $drvDir 'MttVDD.dll'),Res.MttVDD.dll `
    /resource:$(Join-Path $drvDir 'vdd_settings.xml'),Res.vdd_settings.xml `
    /resource:$(Join-Path $drvDir 'devcon.exe'),Res.devcon.exe `
    /resource:$(Join-Path $drvDir 'copy_settings.cmd'),Res.copy_settings.cmd `
    /resource:$(Join-Path $assetDir 'banner.png'),Res.banner.png `
    /resource:$(Join-Path $assetDir 'logo.png'),Res.logo.png `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll `
    $(Join-Path $srcDir 'GhostScreen.cs')

if ($LASTEXITCODE -ne 0) { throw "Compilazione fallita (exit $LASTEXITCODE)" }
"OK: $out ($((Get-Item $out).Length) bytes)"