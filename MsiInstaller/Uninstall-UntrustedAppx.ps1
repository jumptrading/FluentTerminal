param(
    [string]$packageName = $null
)

$PackageFullName = (Get-AppxPackage $packageName).PackageFullName
if ($PackageFullName) {
    Write-Host "Removing package: $packageName"
    Remove-AppxPackage -package $PackageFullName
} else {
    Write-Host "Can't find package: $packageName"
}
