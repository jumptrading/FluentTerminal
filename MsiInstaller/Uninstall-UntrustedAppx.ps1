param(
    [string]$installedPackageName = $null
)

$PackageFullName = (Get-AppxPackage $installedPackageName).PackageFullName
if ($PackageFullName) {
    Write-Host "Removing package: $installedPackageName"
    Remove-AppxPackage -package $PackageFullName
} else {
    Write-Host "Can't find package: $installedPackageName"
}
