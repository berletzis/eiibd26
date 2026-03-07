#!/usr/bin/env pwsh

# 🔧 Script de Limpieza y Compilación - Búsqueda Optimizada

Write-Host "================================" -ForegroundColor Cyan
Write-Host "🔧 Limpieza y Compilación de Proyecto" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Obtener ruta actual
$projectPath = Split-Path -Parent $MyInvocation.MyCommandPath
Write-Host "📁 Ruta del proyecto: $projectPath" -ForegroundColor Yellow

# Paso 1: Detener si está ejecutándose
Write-Host ""
Write-Host "⏹️  Deteniendo aplicación si está ejecutándose..." -ForegroundColor Yellow
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Paso 2: Limpiar
Write-Host ""
Write-Host "🧹 Limpiando caché de compilación..." -ForegroundColor Yellow
dotnet clean 2>&1 | ForEach-Object { Write-Host $_ }

# Paso 3: Eliminar carpetas
Write-Host ""
Write-Host "🗑️  Eliminando carpetas bin/obj..." -ForegroundColor Yellow
Remove-Item -Path "$projectPath\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$projectPath\obj" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "✅ Carpetas eliminadas" -ForegroundColor Green

# Paso 4: Restaurar
Write-Host ""
Write-Host "📦 Restaurando paquetes NuGet..." -ForegroundColor Yellow
dotnet restore 2>&1 | ForEach-Object { Write-Host $_ }

# Paso 5: Compilar
Write-Host ""
Write-Host "🔨 Compilando proyecto..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Debug 2>&1

# Mostrar resultado
Write-Host $buildResult
if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ COMPILACIÓN EXITOSA" -ForegroundColor Green
    Write-Host ""
    Write-Host "🚀 PRÓXIMOS PASOS:" -ForegroundColor Cyan
    Write-Host "1. Abre Visual Studio"
    Write-Host "2. Presiona F5 para ejecutar"
    Write-Host "3. Limpia cache del navegador (Ctrl+Shift+Delete)"
    Write-Host "4. Ve a https://localhost:7002/Contenidos"
    Write-Host "5. Busca 'Diarrea' y verifica que aparece en posición 1"
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "❌ ERROR EN COMPILACIÓN" -ForegroundColor Red
    Write-Host "Revisa los errores arriba" -ForegroundColor Red
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "✅ Script completado" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
