#!/usr/bin/env pwsh

# 🔍 Verificación Rápida - Solo Errores

Write-Host "🔨 Compilando y verificando..." -ForegroundColor Yellow

$build = dotnet build --configuration Debug 2>&1

# Filtrar errores
$errors = $build | Where-Object { $_ -match "error" }
$success = $build | Where-Object { $_ -match "Build succeeded" }

if ($success) {
    Write-Host ""
    Write-Host "✅ BUILD EXITOSO" -ForegroundColor Green
    Write-Host ""
    Write-Host "Próximos pasos:" -ForegroundColor Cyan
    Write-Host "1. Presiona F5 en Visual Studio" -ForegroundColor White
    Write-Host "2. Limpia cache navegador (Ctrl+Shift+Delete)" -ForegroundColor White
    Write-Host "3. Ve a /Contenidos y busca 'Diarrea'" -ForegroundColor White
    Write-Host "4. En Output → Debug deberías ver debug messages" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "❌ ERRORES DE BUILD:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "Guarda el output en archivo:" -ForegroundColor Yellow
    Write-Host "  dotnet build 2>&1 | Out-File build-errors.txt" -ForegroundColor Yellow
}
