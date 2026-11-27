<#
.SYNOPSIS
    Builds WinCap and creates the release files: a portable zip and a signed MSIX.

.DESCRIPTION
    1. dotnet publish: self-contained, single-file WinCap.exe (no .NET install needed).
    2. Zip:  WinCap-<version>-win-x64.zip  (WinCap.exe + README.md)
    3. MSIX: WinCap-<version>-x64.msix     (logos generated from src/WinCap/app.ico)

    MSIX packages must be signed. With -CertificatePath the given PFX is used and the
    manifest Publisher is taken from its subject. Without it a self-signed certificate is
    created and its public part is written as WinCap-<version>-selfsigned.cer; users must
    trust that certificate before they can install the MSIX.

    Used by .github/workflows/release.yml and can be run locally (needs the .NET 9 SDK
    and the Windows 10/11 SDK for makeappx / makepri / signtool).

.EXAMPLE
    ./packaging/package.ps1 -Version 1.0.0
.EXAMPLE
    ./packaging/package.ps1 -Version 1.0.0 -CertificatePath my.pfx -CertificatePassword '***'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [string]$OutputDir = (Join-Path $PSScriptRoot '..\artifacts'),

    [string]$CertificatePath,
    [string]$CertificatePassword,

    # Subject of the self-signed certificate (= MSIX Publisher) when no certificate is given.
    [string]$SelfSignedSubject = 'CN=WinCap'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'src\WinCap\WinCap.csproj'
$icoPath = Join-Path $root 'src\WinCap\app.ico'
New-Item -ItemType Directory -Force $OutputDir | Out-Null
$OutputDir = (Resolve-Path $OutputDir).Path

$work = Join-Path ([IO.Path]::GetTempPath()) "wincap-package-$([guid]::NewGuid().ToString('N'))"
$publishDir = Join-Path $work 'publish'
$layoutDir = Join-Path $work 'msix'
New-Item -ItemType Directory -Force $publishDir, $layoutDir, (Join-Path $layoutDir 'Assets') | Out-Null

function Invoke-Native([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { throw "$([IO.Path]::GetFileName($exe)) failed with exit code $LASTEXITCODE" }
}

function Find-SdkTool([string]$name) {
    $kits = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $tool = Get-ChildItem $kits -Directory -Filter '10.*' -ErrorAction SilentlyContinue |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName "x64\$name" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
    if (-not $tool) { throw "$name not found. Install the Windows 10/11 SDK." }
    return $tool
}

try {
    # ------------------------------------------------------------------
    # 1. Publish
    # ------------------------------------------------------------------
    Write-Host "==> Publishing WinCap $Version"
    Invoke-Native 'dotnet' @(
        'publish', $project,
        '-c', 'Release',
        '-r', 'win-x64',
        '--self-contained', 'true',
        '-p:Platform=x64',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:EnableCompressionInSingleFile=true',
        '-p:DebugType=none',
        "-p:Version=$Version",
        '-o', $publishDir
    )

    # ------------------------------------------------------------------
    # 2. Zip (portable)
    # ------------------------------------------------------------------
    $zipPath = Join-Path $OutputDir "WinCap-$Version-win-x64.zip"
    Write-Host "==> Creating $zipPath"
    Copy-Item (Join-Path $root 'README.md') $publishDir
    if (Test-Path $zipPath) { Remove-Item $zipPath }
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
    Remove-Item (Join-Path $publishDir 'README.md')

    # ------------------------------------------------------------------
    # 3. Signing certificate (decides the manifest Publisher)
    # ------------------------------------------------------------------
    $selfSigned = $null
    if ($CertificatePath) {
        $pfxPath = (Resolve-Path $CertificatePath).Path
        $pfxPassword = $CertificatePassword
        $cert = [Security.Cryptography.X509Certificates.X509Certificate2]::new($pfxPath, $pfxPassword)
        $publisher = $cert.Subject
        $cert.Dispose()
    }
    else {
        Write-Host "==> Creating self-signed certificate $SelfSignedSubject"
        $selfSigned = New-SelfSignedCertificate -Type Custom -Subject $SelfSignedSubject `
            -KeyUsage DigitalSignature -FriendlyName 'WinCap MSIX signing (self-signed)' `
            -CertStoreLocation 'Cert:\CurrentUser\My' -NotAfter (Get-Date).AddYears(3) `
            -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
        $pfxPath = Join-Path $work 'selfsigned.pfx'
        $pfxPassword = [guid]::NewGuid().ToString('N')
        $secure = ConvertTo-SecureString $pfxPassword -AsPlainText -Force
        Export-PfxCertificate -Cert $selfSigned -FilePath $pfxPath -Password $secure | Out-Null
        Export-Certificate -Cert $selfSigned -FilePath (Join-Path $OutputDir "WinCap-$Version-selfsigned.cer") | Out-Null
        $publisher = $selfSigned.Subject
    }
    # Display name: the CN value of the subject.
    $publisherDisplayName = if ($publisher -match 'CN=("[^"]+"|[^,]+)') { $Matches[1].Trim('"') } else { $publisher }

    # ------------------------------------------------------------------
    # 4. MSIX layout: exe, manifest, logos
    # ------------------------------------------------------------------
    Write-Host '==> Preparing MSIX layout'
    Copy-Item (Join-Path $publishDir '*') $layoutDir -Recurse

    $manifest = Get-Content (Join-Path $PSScriptRoot 'AppxManifest.xml') -Raw
    $manifest = $manifest.Replace('$(Version)', "$Version.0").
        Replace('$(Publisher)', [Security.SecurityElement]::Escape($publisher)).
        Replace('$(PublisherDisplayName)', [Security.SecurityElement]::Escape($publisherDisplayName))
    Set-Content (Join-Path $layoutDir 'AppxManifest.xml') $manifest -Encoding utf8

    Add-Type -AssemblyName System.Drawing
    $assets = Join-Path $layoutDir 'Assets'

    # Draws the app icon centered on a transparent canvas (iconSize <= canvas).
    function New-Logo([string]$file, [int]$canvasW, [int]$canvasH, [int]$iconSize) {
        $icon = [Drawing.Icon]::new($icoPath, $iconSize, $iconSize)
        $frame = $icon.ToBitmap()
        $bmp = [Drawing.Bitmap]::new($canvasW, $canvasH)
        $g = [Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.DrawImage($frame, [int](($canvasW - $iconSize) / 2), [int](($canvasH - $iconSize) / 2), $iconSize, $iconSize)
        $bmp.Save((Join-Path $assets $file), [Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose(); $bmp.Dispose(); $frame.Dispose(); $icon.Dispose()
    }

    # App list / taskbar / Start icons. makepri picks the matching scale / target size.
    foreach ($s in @{ 100 = 44; 125 = 55; 150 = 66; 200 = 88; 400 = 176 }.GetEnumerator()) {
        New-Logo "Square44x44Logo.scale-$($s.Key).png" $s.Value $s.Value $s.Value
    }
    foreach ($t in 16, 24, 32, 48, 256) {
        New-Logo "Square44x44Logo.targetsize-$t.png" $t $t $t
        New-Logo "Square44x44Logo.targetsize-$($t)_altform-unplated.png" $t $t $t
    }
    New-Logo 'Square150x150Logo.scale-100.png' 150 150 96
    New-Logo 'Square150x150Logo.scale-200.png' 300 300 192
    New-Logo 'StoreLogo.scale-100.png' 50 50 50
    New-Logo 'StoreLogo.scale-200.png' 100 100 100

    # ------------------------------------------------------------------
    # 5. resources.pri, pack, sign
    # ------------------------------------------------------------------
    $makepri = Find-SdkTool 'makepri.exe'
    $makeappx = Find-SdkTool 'makeappx.exe'
    $signtool = Find-SdkTool 'signtool.exe'

    Write-Host '==> Indexing resources'
    $priConfig = Join-Path $work 'priconfig.xml'
    Invoke-Native $makepri @('createconfig', '/cf', $priConfig, '/dq', 'en-US', '/pv', '10.0.0', '/o')
    # Keep every scale in the single resources.pri (the default config splits them out for bundles).
    [xml]$cfg = Get-Content $priConfig
    $packaging = $cfg.SelectSingleNode('/resources/packaging')
    if ($packaging) { [void]$packaging.ParentNode.RemoveChild($packaging); $cfg.Save($priConfig) }
    Invoke-Native $makepri @('new', '/pr', $layoutDir, '/cf', $priConfig,
        '/mn', (Join-Path $layoutDir 'AppxManifest.xml'), '/of', (Join-Path $layoutDir 'resources.pri'), '/o')

    $msixPath = Join-Path $OutputDir "WinCap-$Version-x64.msix"
    Write-Host "==> Packing $msixPath"
    Invoke-Native $makeappx @('pack', '/d', $layoutDir, '/p', $msixPath, '/o')

    Write-Host "==> Signing ($publisher)"
    $signArgs = @('sign', '/fd', 'SHA256', '/f', $pfxPath, '/p', $pfxPassword)
    if (-not $selfSigned) { $signArgs += @('/tr', 'http://timestamp.digicert.com', '/td', 'SHA256') }
    Invoke-Native $signtool ($signArgs + $msixPath)

    Write-Host '==> Done'
    Get-ChildItem $OutputDir | ForEach-Object { Write-Host ("    {0}  ({1:N1} MB)" -f $_.Name, ($_.Length / 1MB)) }
}
finally {
    if ($selfSigned) { Remove-Item "Cert:\CurrentUser\My\$($selfSigned.Thumbprint)" -ErrorAction SilentlyContinue }
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
