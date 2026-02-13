# 🔒 Backup Automation Script - Performance Optimizations
# Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# Uso: .\create-backups.ps1

$ErrorActionPreference = "Stop"

Write-Host "🔒 BACKUP SCRIPT - eiibd26 Performance Optimizations" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Directorio raíz del proyecto
$ProjectRoot = "D:\Users\berletzis\Source\Repos\eiibd\eiibd26"
Set-Location $ProjectRoot

# Crear carpeta de backups si no existe
$BackupDir = Join-Path $ProjectRoot "BACKUPS"
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
    Write-Host "✅ Carpeta BACKUPS creada" -ForegroundColor Green
}

# Timestamp para backups incrementales
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Lista de archivos a respaldar
$FilesToBackup = @(
    @{
        Source = "eiibd26\Program.cs"
        BackupName = "Program.cs"
        Description = "Configuración principal de la aplicación (middleware, servicios)"
    },
    @{
        Source = "eiibd26\Pages\Home\Index.cshtml.cs"
        BackupName = "Index.cshtml.cs"
        Description = "Homepage - código C# (queries, lógica de negocio)"
    },
    @{
        Source = "eiibd26\Pages\Home\Index.cshtml"
        BackupName = "Index.cshtml"
        Description = "Homepage - vista Razor"
    }
)

Write-Host "📁 Archivos a respaldar:" -ForegroundColor Yellow
foreach ($file in $FilesToBackup) {
    Write-Host "   • $($file.Source) - $($file.Description)" -ForegroundColor White
}
Write-Host ""

# Función para crear backup con verificación
function Create-Backup {
    param(
        [string]$SourcePath,
        [string]$BackupName,
        [string]$Description
    )
    
    $FullSourcePath = Join-Path $ProjectRoot $SourcePath
    
    if (-not (Test-Path $FullSourcePath)) {
        Write-Host "   ⚠️  SKIP: $SourcePath (no existe)" -ForegroundColor Yellow
        return $false
    }
    
    # Backup con timestamp (incremental)
    $TimestampedBackup = Join-Path $BackupDir "$BackupName.$Timestamp.backup"
    Copy-Item $FullSourcePath -Destination $TimestampedBackup -Force
    
    # Backup "original" (solo si no existe)
    $OriginalBackup = Join-Path $BackupDir "$BackupName.original"
    if (-not (Test-Path $OriginalBackup)) {
        Copy-Item $FullSourcePath -Destination $OriginalBackup -Force
        Write-Host "   ✅ $BackupName.original creado" -ForegroundColor Green
    }
    
    # Backup "latest" (siempre sobreescribe)
    $LatestBackup = Join-Path $BackupDir "$BackupName.latest"
    Copy-Item $FullSourcePath -Destination $LatestBackup -Force
    
    Write-Host "   ✅ $BackupName ($Timestamp) - $Description" -ForegroundColor Green
    return $true
}

# Crear backups
Write-Host "🔄 Creando backups..." -ForegroundColor Cyan
Write-Host ""
$SuccessCount = 0
foreach ($file in $FilesToBackup) {
    if (Create-Backup -SourcePath $file.Source -BackupName $file.BackupName -Description $file.Description) {
        $SuccessCount++
    }
}

Write-Host ""
Write-Host "✅ Backups completados: $SuccessCount/$($FilesToBackup.Count)" -ForegroundColor Green
Write-Host ""

# Mostrar contenido de la carpeta BACKUPS
Write-Host "📂 Contenido de BACKUPS:" -ForegroundColor Cyan
Get-ChildItem $BackupDir | Format-Table Name, Length, LastWriteTime -AutoSize

Write-Host ""
Write-Host "🔍 Tipos de backup creados:" -ForegroundColor Yellow
Write-Host "   • .original  → Backup inicial (nunca se sobreescribe)" -ForegroundColor White
Write-Host "   • .latest    → Último backup antes de cambios" -ForegroundColor White
Write-Host "   • .TIMESTAMP → Backup con fecha/hora específica" -ForegroundColor White
Write-Host ""

# Crear archivo de manifiesto
$ManifestPath = Join-Path $BackupDir "MANIFEST_$Timestamp.txt"
$ManifestContent = @"
BACKUP MANIFEST
===============
Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Usuario: $env:USERNAME
Computadora: $env:COMPUTERNAME
Directorio: $ProjectRoot

Archivos respaldados:
"@

foreach ($file in $FilesToBackup) {
    $FullPath = Join-Path $ProjectRoot $file.Source
    if (Test-Path $FullPath) {
        $FileInfo = Get-Item $FullPath
        $ManifestContent += "`n- $($file.Source)`n"
        $ManifestContent += "  Tamaño: $($FileInfo.Length) bytes`n"
        $ManifestContent += "  Modificado: $($FileInfo.LastWriteTime)`n"
        $ManifestContent += "  Hash MD5: $(Get-FileHash $FullPath -Algorithm MD5 | Select-Object -ExpandProperty Hash)`n"
    }
}

$ManifestContent | Out-File $ManifestPath -Encoding UTF8
Write-Host "📄 Manifiesto creado: MANIFEST_$Timestamp.txt" -ForegroundColor Green
Write-Host ""

# Comandos útiles para rollback
Write-Host "🆘 COMANDOS DE ROLLBACK:" -ForegroundColor Yellow
Write-Host ""
Write-Host "   # Restaurar desde backup original (antes de TODOS los cambios):" -ForegroundColor White
Write-Host '   Copy-Item ".\BACKUPS\Program.cs.original" -Destination ".\eiibd26\Program.cs" -Force' -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Restaurar desde backup más reciente:" -ForegroundColor White
Write-Host '   Copy-Item ".\BACKUPS\Program.cs.latest" -Destination ".\eiibd26\Program.cs" -Force' -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Restaurar desde backup específico (timestamp):" -ForegroundColor White
Write-Host "   Copy-Item `".\BACKUPS\Program.cs.$Timestamp.backup`" -Destination `".\eiibd26\Program.cs`" -Force" -ForegroundColor Cyan
Write-Host ""

# Preguntar si desea continuar con las optimizaciones
Write-Host "⚡ ¿Deseas continuar con las optimizaciones de performance? (S/N): " -ForegroundColor Yellow -NoNewline
$Response = Read-Host

if ($Response -eq "S" -or $Response -eq "s") {
    Write-Host ""
    Write-Host "✅ Backups completados. Procede con las optimizaciones." -ForegroundColor Green
    Write-Host "   Consulta ROLLBACK_GUIDE.md para instrucciones detalladas." -ForegroundColor White
    Write-Host ""
    exit 0
} else {
    Write-Host ""
    Write-Host "⏸️  Optimizaciones canceladas. Los backups se mantienen en BACKUPS/" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
