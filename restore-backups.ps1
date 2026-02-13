# 🔙 Restore Script - Rollback Performance Optimizations
# Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# Uso: .\restore-backups.ps1

$ErrorActionPreference = "Stop"

Write-Host "🔙 RESTORE SCRIPT - eiibd26 Rollback" -ForegroundColor Red
Write-Host "=====================================" -ForegroundColor Red
Write-Host ""

# Directorio raíz del proyecto
$ProjectRoot = "D:\Users\berletzis\Source\Repos\eiibd\eiibd26"
Set-Location $ProjectRoot

$BackupDir = Join-Path $ProjectRoot "BACKUPS"

if (-not (Test-Path $BackupDir)) {
    Write-Host "❌ ERROR: No se encontró la carpeta BACKUPS" -ForegroundColor Red
    Write-Host "   Ejecuta create-backups.ps1 primero" -ForegroundColor Yellow
    exit 1
}

Write-Host "⚠️  ADVERTENCIA: Este script restaurará archivos desde backups" -ForegroundColor Yellow
Write-Host "   Esto SOBREESCRIBIRÁ los archivos actuales" -ForegroundColor Yellow
Write-Host ""

# Mostrar backups disponibles
Write-Host "📂 Backups disponibles:" -ForegroundColor Cyan
Get-ChildItem $BackupDir -Filter "*.original" | Format-Table Name, LastWriteTime -AutoSize
Get-ChildItem $BackupDir -Filter "*.latest" | Format-Table Name, LastWriteTime -AutoSize

Write-Host ""
Write-Host "Opciones de restauración:" -ForegroundColor Yellow
Write-Host "  [1] Restaurar TODO desde .original (estado inicial antes de optimizaciones)" -ForegroundColor White
Write-Host "  [2] Restaurar TODO desde .latest (último backup antes del cambio más reciente)" -ForegroundColor White
Write-Host "  [3] Restaurar SOLO Program.cs desde .original" -ForegroundColor White
Write-Host "  [4] Restaurar SOLO Index.cshtml.cs desde .original" -ForegroundColor White
Write-Host "  [5] Personalizado (elegir archivo y versión)" -ForegroundColor White
Write-Host "  [0] Cancelar" -ForegroundColor Gray
Write-Host ""

Write-Host "Elige una opción: " -NoNewline -ForegroundColor Yellow
$Choice = Read-Host

function Restore-File {
    param(
        [string]$BackupName,
        [string]$DestinationPath,
        [string]$BackupType = "original"
    )
    
    $BackupFile = Join-Path $BackupDir "$BackupName.$BackupType"
    $DestinationFull = Join-Path $ProjectRoot $DestinationPath
    
    if (-not (Test-Path $BackupFile)) {
        Write-Host "   ❌ ERROR: No existe $BackupName.$BackupType" -ForegroundColor Red
        return $false
    }
    
    # Crear backup del archivo actual antes de sobrescribir
    $PreRestoreBackup = "$DestinationFull.pre-restore-$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    if (Test-Path $DestinationFull) {
        Copy-Item $DestinationFull -Destination $PreRestoreBackup -Force
        Write-Host "   💾 Backup pre-restore: $([System.IO.Path]::GetFileName($PreRestoreBackup))" -ForegroundColor Gray
    }
    
    Copy-Item $BackupFile -Destination $DestinationFull -Force
    Write-Host "   ✅ Restaurado: $DestinationPath desde $BackupName.$BackupType" -ForegroundColor Green
    return $true
}

function Restore-All {
    param([string]$BackupType)
    
    Write-Host ""
    Write-Host "🔄 Restaurando TODOS los archivos desde .$BackupType..." -ForegroundColor Cyan
    Write-Host ""
    
    $Files = @(
        @{ Backup = "Program.cs"; Destination = "eiibd26\Program.cs" },
        @{ Backup = "Index.cshtml.cs"; Destination = "eiibd26\Pages\Home\Index.cshtml.cs" },
        @{ Backup = "Index.cshtml"; Destination = "eiibd26\Pages\Home\Index.cshtml" }
    )
    
    foreach ($file in $Files) {
        Restore-File -BackupName $file.Backup -DestinationPath $file.Destination -BackupType $BackupType
    }
    
    Write-Host ""
    Write-Host "✅ Restauración completa desde .$BackupType" -ForegroundColor Green
}

switch ($Choice) {
    "1" {
        Write-Host ""
        Write-Host "⚠️  Esto restaurará el estado INICIAL (antes de TODAS las optimizaciones)" -ForegroundColor Yellow
        Write-Host "   ¿Estás seguro? (S/N): " -NoNewline -ForegroundColor Red
        $Confirm = Read-Host
        if ($Confirm -eq "S" -or $Confirm -eq "s") {
            Restore-All -BackupType "original"
        } else {
            Write-Host "❌ Cancelado" -ForegroundColor Yellow
            exit 0
        }
    }
    "2" {
        Restore-All -BackupType "latest"
    }
    "3" {
        Restore-File -BackupName "Program.cs" -DestinationPath "eiibd26\Program.cs" -BackupType "original"
    }
    "4" {
        Restore-File -BackupName "Index.cshtml.cs" -DestinationPath "eiibd26\Pages\Home\Index.cshtml.cs" -BackupType "original"
    }
    "5" {
        Write-Host ""
        Write-Host "📝 Archivos disponibles:" -ForegroundColor Cyan
        $AvailableBackups = Get-ChildItem $BackupDir -Filter "*.original" | Select-Object -ExpandProperty Name
        for ($i = 0; $i -lt $AvailableBackups.Count; $i++) {
            Write-Host "  [$i] $($AvailableBackups[$i])" -ForegroundColor White
        }
        Write-Host ""
        Write-Host "Elige archivo (número): " -NoNewline -ForegroundColor Yellow
        $FileIndex = Read-Host
        
        Write-Host "Tipo de backup (original/latest/timestamp): " -NoNewline -ForegroundColor Yellow
        $BackupType = Read-Host
        
        $SelectedBackup = $AvailableBackups[$FileIndex] -replace "\.original$", ""
        
        # Mapeo de archivos
        $DestinationMap = @{
            "Program.cs" = "eiibd26\Program.cs"
            "Index.cshtml.cs" = "eiibd26\Pages\Home\Index.cshtml.cs"
            "Index.cshtml" = "eiibd26\Pages\Home\Index.cshtml"
        }
        
        if ($DestinationMap.ContainsKey($SelectedBackup)) {
            Restore-File -BackupName $SelectedBackup -DestinationPath $DestinationMap[$SelectedBackup] -BackupType $BackupType
        } else {
            Write-Host "❌ Archivo no reconocido" -ForegroundColor Red
        }
    }
    "0" {
        Write-Host "❌ Operación cancelada" -ForegroundColor Yellow
        exit 0
    }
    default {
        Write-Host "❌ Opción inválida" -ForegroundColor Red
        exit 1
    }
}

# Verificación post-restauración
Write-Host ""
Write-Host "🔍 Verificando restauración..." -ForegroundColor Cyan

# Compilar proyecto
Write-Host "   Compilando proyecto..." -ForegroundColor White
$BuildOutput = dotnet build ".\eiibd26\eiibd26.csproj" 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Compilación exitosa" -ForegroundColor Green
} else {
    Write-Host "   ❌ ERROR en compilación:" -ForegroundColor Red
    Write-Host $BuildOutput -ForegroundColor Gray
    Write-Host ""
    Write-Host "   🔧 Intenta:" -ForegroundColor Yellow
    Write-Host "      dotnet clean" -ForegroundColor White
    Write-Host "      dotnet restore" -ForegroundColor White
    Write-Host "      dotnet build" -ForegroundColor White
}

Write-Host ""
Write-Host "📝 Próximos pasos:" -ForegroundColor Yellow
Write-Host "   1. Revisa los archivos restaurados en Visual Studio" -ForegroundColor White
Write-Host "   2. Ejecuta la aplicación para verificar funcionamiento" -ForegroundColor White
Write-Host "   3. Revisa errores de compilación si existen" -ForegroundColor White
Write-Host ""
Write-Host "📄 Consulta ROLLBACK_GUIDE.md para más información" -ForegroundColor Cyan
Write-Host ""
