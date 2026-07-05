# Sayra Server Windows Service Installation Script

$ServiceName = "SayraServer"
$BinaryPath = "$PSScriptRoot\Sayra.Server.Core.exe"

if (-not (Test-Path $BinaryPath)) {
    Write-Error "Could not find $BinaryPath. Please build the project first."
    exit 1
}

# Check if service already exists
$Service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($Service) {
    Write-Host "Stopping existing service..."
    Stop-Service -Name $ServiceName
    Write-Host "Removing existing service..."
    sc.exe delete $ServiceName
}

Write-Host "Installing Sayra Server as a Windows Service..."
New-Service -Name $ServiceName `
            -BinaryPathName $BinaryPath `
            -DisplayName "Sayra Server Management System" `
            -Description "Commercial-grade LAN cyber cafe management system core server." `
            -StartupType Automatic

# Set recovery options
Write-Host "Configuring service recovery policies..."
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

Write-Host "Starting service..."
Start-Service -Name $ServiceName

Write-Host "Installation Complete."
