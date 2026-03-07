#!/usr/bin/env pwsh

# 🔍 Script de Diagnóstico - Captura Output en Archivo

Write-Host "================================" -ForegroundColor Cyan
Write-Host "🔍 Capturando Output de Compilación" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$outputFile = ".\BUILD-OUTPUT.txt"

# Compilar y capturar output
Write-Host "🔨 Compilando..." -ForegroundColor Yellow
$buildOutput = dotnet build --configuration Debug 2>&1

# Guardar en archivo
$buildOutput | Out-File -FilePath $outputFile -Encoding UTF8 -Force

Write-Host ""
Write-Host "📄 Output guardado en: $outputFile" -ForegroundColor Green
Write-Host ""

# Analizar el archivo
Write-Host "🔎 Analizando build..." -ForegroundColor Yellow
$content = Get-Content $outputFile -Raw

# Contar errores
$errors = [regex]::Matches($content, "error").Count
$warnings = [regex]::Matches($content, "warning").Count

Write-Host "  Errores: $errors" -ForegroundColor $(if ($errors -gt 0) { "Red" } else { "Green" })
Write-Host "  Warnings: $warnings" -ForegroundColor Yellow

if ($errors -gt 0) {
    Write-Host ""
    Write-Host "❌ ERRORES ENCONTRADOS:" -ForegroundColor Red
    $errorLines = $content -split "`n" | Where-Object { $_ -match "error" } | Select-Object -First 5
    $errorLines | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host ""
    Write-Host "✅ COMPILACIÓN EXITOSA" -ForegroundColor Green
}

Write-Host ""
Write-Host "Abre el archivo BUILD-OUTPUT.txt para ver todos los detalles" -ForegroundColor Cyan
Write-Host "Ubica: 'error' para ver errores específicos" -ForegroundColor Cyan
Write-Host ""
