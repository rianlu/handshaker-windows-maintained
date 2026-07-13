$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$payload = Join-Path $root "build\HandShaker.Payload.Setup.exe"
$project = Join-Path $root "src\HandShaker.Setup\HandShaker.Setup.csproj"
$uninstallerProject = Join-Path $root "src\HandShaker.Uninstaller\HandShaker.Uninstaller.csproj"
$uninstaller = Join-Path $root "build\HandShakerUninst.exe"
$output = Join-Path $root "dist\HandShaker-Windows-Maintained-Offline-Setup.exe"
$cache = Join-Path $env:LOCALAPPDATA "HandShakerBuildCache"
$package = Join-Path $cache "microsoft.netframework.referenceassemblies.net40.1.0.3.nupkg"
$references = Join-Path $cache "net40\build\.NETFramework\v4.0"
$packageUrl = "https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net40/1.0.3/microsoft.netframework.referenceassemblies.net40.1.0.3.nupkg"
$msbuild = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

if (-not (Test-Path $payload)) {
    throw "缺少 build\HandShaker.Payload.Setup.exe, 请先运行 tools/build_payload.sh."
}

if (-not (Test-Path $references)) {
    New-Item -ItemType Directory -Force -Path $cache | Out-Null
    if (-not (Test-Path $package) -or (Get-Item $package).Length -lt 1MB) {
        $download = "$package.download"
        Remove-Item -Force -ErrorAction SilentlyContinue $download
        (New-Object System.Net.WebClient).DownloadFile($packageUrl, $download)
        Move-Item -Force $download $package
    }
    $referenceRoot = Join-Path $cache "net40"
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $referenceRoot
    New-Item -ItemType Directory -Force -Path $referenceRoot | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($package, $referenceRoot)
}

& $msbuild $uninstallerProject /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU "/p:FrameworkPathOverride=$references" /v:minimal
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$builtUninstaller = Join-Path $root "src\HandShaker.Uninstaller\bin\Release\HandShakerUninst.exe"
Copy-Item -Force $builtUninstaller $uninstaller

& $msbuild $project /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU "/p:FrameworkPathOverride=$references" /v:minimal
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$built = Join-Path $root "src\HandShaker.Setup\bin\Release\HandShaker-Windows-Maintained-Offline-Setup.exe"
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $output) | Out-Null
Copy-Item -Force $built $output
Get-FileHash -Algorithm SHA256 $output
