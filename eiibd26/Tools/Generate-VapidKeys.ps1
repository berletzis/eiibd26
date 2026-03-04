# Generate VAPID Keys for Web Push
Write-Host "=== VAPID KEY GENERATOR ===" -ForegroundColor Cyan
Write-Host ""

# Generate random bytes for keys
$publicBytes = New-Object byte[] 65
$privateBytes = New-Object byte[] 32

$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($publicBytes)
$rng.GetBytes($privateBytes)

# Convert to base64url
function ConvertTo-Base64Url {
    param([byte[]]$bytes)
    $base64 = [Convert]::ToBase64String($bytes)
    return $base64.Replace('+', '-').Replace('/', '_').TrimEnd('=')
}

$publicKey = ConvertTo-Base64Url $publicBytes
$privateKey = ConvertTo-Base64Url $privateBytes

Write-Host "Public Key:" -ForegroundColor Green
Write-Host $publicKey
Write-Host ""
Write-Host "Private Key:" -ForegroundColor Green
Write-Host $privateKey
Write-Host ""
Write-Host "=== AGREGAR EN appsettings.json ===" -ForegroundColor Yellow
Write-Host ""
Write-Host @"
{
  "VapidKeys": {
    "PublicKey": "$publicKey",
    "PrivateKey": "$privateKey",
    "Subject": "mailto:admin@eiibd.com"
  }
}
"@
