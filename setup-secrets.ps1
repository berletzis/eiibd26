# ====================================================================
# Script de Configuración de User Secrets - EIIBD26
# ====================================================================
# Este script configura User Secrets para desarrollo local de forma segura
# ====================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CONFIGURACIÓN DE USER SECRETS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "eiibd26"

# Verificar que estamos en la raíz correcta
if (-not (Test-Path $projectPath)) {
    Write-Host "❌ ERROR: No se encuentra el directorio '$projectPath'" -ForegroundColor Red
    Write-Host "Ejecuta este script desde la raíz del repositorio." -ForegroundColor Yellow
    exit 1
}

Write-Host "📁 Proyecto encontrado: $projectPath" -ForegroundColor Green
Write-Host ""

# Inicializar User Secrets
Write-Host "🔐 Inicializando User Secrets..." -ForegroundColor Cyan
dotnet user-secrets init --project $projectPath
Write-Host ""

# Solicitar credenciales de SQL Server
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  SQL SERVER CREDENTIALS" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

$sqlServer = Read-Host "Servidor SQL (ej: 132.148.74.136\ybridio)"
$sqlDatabase = Read-Host "Base de datos (ej: eiibd26)"
$sqlUser = Read-Host "Usuario SQL (ej: sa)"
$sqlPassword = Read-Host "Contraseña SQL" -AsSecureString
$sqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlPassword)
)

$connectionString = "Server=$sqlServer;Database=$sqlDatabase;user id=$sqlUser;password=$sqlPasswordPlain;TrustServerCertificate=True;MultipleActiveResultSets=true"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString --project $projectPath
Write-Host "✅ Cadena de conexión configurada" -ForegroundColor Green
Write-Host ""

# Solicitar credenciales de SendGrid
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  SENDGRID CREDENTIALS" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

$sendGridKey = Read-Host "SendGrid API Key (ej: SG.xxx...)"
$sendGridEmail = Read-Host "Email remitente (ej: no-reply@eiibd.com)"
$sendGridName = Read-Host "Nombre remitente (ej: EIIBD)"

dotnet user-secrets set "SendGrid:ApiKey" $sendGridKey --project $projectPath
dotnet user-secrets set "SendGrid:FromEmail" $sendGridEmail --project $projectPath
dotnet user-secrets set "SendGrid:FromName" $sendGridName --project $projectPath
Write-Host "✅ SendGrid configurado" -ForegroundColor Green
Write-Host ""

# Solicitar credenciales de Twilio
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  TWILIO CREDENTIALS" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

$twilioSid = Read-Host "Twilio Account SID (ej: ACxxx...)"
$twilioToken = Read-Host "Twilio Auth Token" -AsSecureString
$twilioTokenPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($twilioToken)
)
$twilioPhone = Read-Host "Twilio Phone Number (ej: +14752588653)"

dotnet user-secrets set "Twilio:AccountSid" $twilioSid --project $projectPath
dotnet user-secrets set "Twilio:AuthToken" $twilioTokenPlain --project $projectPath
dotnet user-secrets set "Twilio:FromNumber" $twilioPhone --project $projectPath
Write-Host "✅ Twilio configurado" -ForegroundColor Green
Write-Host ""

# Mostrar resumen
Write-Host "========================================" -ForegroundColor Green
Write-Host "  ✅ CONFIGURACIÓN COMPLETADA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "📋 User Secrets configurados:" -ForegroundColor Cyan
dotnet user-secrets list --project $projectPath
Write-Host ""

Write-Host "🎉 ¡Listo! Tu aplicación ahora usa User Secrets." -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  IMPORTANTE:" -ForegroundColor Yellow
Write-Host "  1. Estos secretos solo están en TU máquina local" -ForegroundColor Yellow
Write-Host "  2. No se subirán a Git (están fuera del repositorio)" -ForegroundColor Yellow
Write-Host "  3. Cada desarrollador debe ejecutar este script" -ForegroundColor Yellow
Write-Host "  4. Para producción, usa Azure Key Vault o variables de entorno" -ForegroundColor Yellow
Write-Host ""

Write-Host "📍 Ubicación de tus secretos:" -ForegroundColor Cyan
$secretsPath = "$env:APPDATA\Microsoft\UserSecrets"
Write-Host "   $secretsPath" -ForegroundColor Gray
Write-Host ""

Write-Host "🚀 Puedes ejecutar tu app con:" -ForegroundColor Cyan
Write-Host "   cd $projectPath" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
