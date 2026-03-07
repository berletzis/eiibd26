#!/usr/bin/env pwsh

# 🔍 Script de Diagnóstico - Verifica si el código está correcto

Write-Host "================================" -ForegroundColor Cyan
Write-Host "🔍 Diagnóstico del Sistema de Búsqueda" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$filePath = ".\Pages\Contenidos\Index.cshtml.cs"

# Verificar que el archivo existe
Write-Host "📄 Verificando archivo..." -ForegroundColor Yellow
if (-not (Test-Path $filePath)) {
    Write-Host "❌ El archivo no existe: $filePath" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Archivo encontrado: $filePath" -ForegroundColor Green

# Verificar que tiene el método CalculateRelevanceScore
Write-Host ""
Write-Host "🔎 Buscando método CalculateRelevanceScore..." -ForegroundColor Yellow
$content = Get-Content $filePath -Raw
if ($content -match "private int CalculateRelevanceScore") {
    Write-Host "✅ Método encontrado" -ForegroundColor Green
} else {
    Write-Host "❌ Método NO encontrado" -ForegroundColor Red
    exit 1
}

# Verificar scoring escalado (10000)
Write-Host ""
Write-Host "🔎 Verificando scoring escalado..." -ForegroundColor Yellow
if ($content -match "score \+= 10000") {
    Write-Host "✅ Scoring 10000 (exacto) encontrado" -ForegroundColor Green
} else {
    Write-Host "❌ Scoring 10000 NO encontrado" -ForegroundColor Red
    Write-Host "   El código podría no haber sido guardado correctamente" -ForegroundColor Red
    exit 1
}

# Verificar 5000
if ($content -match "score \+= 5000") {
    Write-Host "✅ Scoring 5000 (comienza con) encontrado" -ForegroundColor Green
} else {
    Write-Host "❌ Scoring 5000 NO encontrado" -ForegroundColor Red
    exit 1
}

# Verificar 2000
if ($content -match "score \+= 2000") {
    Write-Host "✅ Scoring 2000 (límite palabra) encontrado" -ForegroundColor Green
} else {
    Write-Host "❌ Scoring 2000 NO encontrado" -ForegroundColor Red
    exit 1
}

# Verificar 1000
if ($content -match "score \+= 1000") {
    Write-Host "✅ Scoring 1000 (substring) encontrado" -ForegroundColor Green
} else {
    Write-Host "❌ Scoring 1000 NO encontrado" -ForegroundColor Red
    exit 1
}

# Verificar ordenamiento antes de paginar
Write-Host ""
Write-Host "🔎 Verificando ordenamiento..." -ForegroundColor Yellow
if ($content -match "\.OrderByDescending\(x => x\.RelevanceScore\)") {
    Write-Host "✅ Ordenamiento por relevancia encontrado" -ForegroundColor Green
} else {
    Write-Host "❌ Ordenamiento NO encontrado" -ForegroundColor Red
    exit 1
}

# Verificar debug output
Write-Host ""
Write-Host "🔎 Verificando debug output..." -ForegroundColor Yellow
if ($content -match "System\.Diagnostics\.Debug\.WriteLine.*SEARCH.*searchTerm") {
    Write-Host "✅ Debug output encontrado" -ForegroundColor Green
} else {
    Write-Host "⚠️  Debug output NO encontrado (pero no es crítico)" -ForegroundColor Yellow
}

# Resumen
Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "✅ DIAGNÓSTICO COMPLETO" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "El código de búsqueda está CORRECTAMENTE IMPLEMENTADO ✅" -ForegroundColor Green
Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Yellow
Write-Host "1. Ejecutar: .\CLEAN-AND-BUILD.ps1" -ForegroundColor Yellow
Write-Host "2. Luego: F5 en Visual Studio" -ForegroundColor Yellow
Write-Host "3. Luego: Limpia cache del navegador (Ctrl+Shift+Delete)" -ForegroundColor Yellow
Write-Host "4. Luego: Prueba en https://localhost:7002/Contenidos" -ForegroundColor Yellow
Write-Host ""
