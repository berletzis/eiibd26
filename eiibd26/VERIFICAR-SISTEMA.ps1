# ===== SCRIPT DE VERIFICACIÓN RÁPIDA =====
# Ejecuta este script para verificar el estado del sistema IA

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "VERIFICACIÓN SISTEMA IA - EIIBD26" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verificar que appsettings.json existe
$appSettingsPath = ".\appsettings.json"
if (Test-Path $appSettingsPath) {
    Write-Host "✅ appsettings.json encontrado" -ForegroundColor Green
    
    # Leer configuración de IA
    $config = Get-Content $appSettingsPath | ConvertFrom-Json
    $aiConfig = $config.AiAnswer
    
    Write-Host ""
    Write-Host "Configuración IA:" -ForegroundColor Yellow
    Write-Host "  Enabled: $($aiConfig.Enabled)" -ForegroundColor $(if ($aiConfig.Enabled) { "Green" } else { "Red" })
    
    $hasApiKey = $aiConfig.AnthropicApiKey -and $aiConfig.AnthropicApiKey -ne "ANTHROPIC_API_KEY_AQUI" -and $aiConfig.AnthropicApiKey.Length -gt 20
    Write-Host "  API Key configurada: $hasApiKey" -ForegroundColor $(if ($hasApiKey) { "Green" } else { "Red" })
    
    Write-Host "  Model: $($aiConfig.Model)" -ForegroundColor Cyan
    
    $hasSystemUserId = $aiConfig.SystemUserId -and $aiConfig.SystemUserId -ne "00000000-0000-0000-0000-000000000000"
    Write-Host "  System User ID: $($aiConfig.SystemUserId)" -ForegroundColor $(if ($hasSystemUserId) { "Green" } else { "Red" })
    
    Write-Host ""
    
    if (-not $aiConfig.Enabled) {
        Write-Host "❌ PROBLEMA: AiAnswer.Enabled está en false" -ForegroundColor Red
        Write-Host "   SOLUCIÓN: Cambia a true en appsettings.json" -ForegroundColor Yellow
    }
    
    if (-not $hasApiKey) {
        Write-Host "❌ PROBLEMA: API Key no configurada o es placeholder" -ForegroundColor Red
        Write-Host "   SOLUCIÓN: Agrega tu API key real de Anthropic" -ForegroundColor Yellow
    }
    
    if (-not $hasSystemUserId) {
        Write-Host "❌ PROBLEMA: System User ID no configurado" -ForegroundColor Red
        Write-Host "   SOLUCIÓN: Ejecuta SETUP-SYSTEM-USER.sql y copia el GUID" -ForegroundColor Yellow
    }
    
} else {
    Write-Host "❌ appsettings.json NO encontrado" -ForegroundColor Red
    Write-Host "   Asegúrate de estar en el directorio eiibd26" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "2. ARCHIVOS DE CÓDIGO" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Verificar archivos clave
$archivos = @(
    "Controllers\PreguntasApiController.cs",
    "Controllers\AiTestController.cs",
    "Jobs\AiAnswerJob.cs",
    "Services\AI\AiAnswerService.cs",
    "Pages\Test\AiTest.cshtml"
)

foreach ($archivo in $archivos) {
    if (Test-Path $archivo) {
        Write-Host "✅ $archivo" -ForegroundColor Green
    } else {
        Write-Host "❌ $archivo NO ENCONTRADO" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "3. PRÓXIMOS PASOS" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Si hay ❌ arriba, corrígelos primero" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. Ejecuta el diagnóstico SQL:" -ForegroundColor Yellow
Write-Host "   - Abre SQL Server Management Studio" -ForegroundColor White
Write-Host "   - Ejecuta: DIAGNOSTICO-RAPIDO.sql" -ForegroundColor White
Write-Host ""
Write-Host "3. Reinicia la aplicación en Visual Studio:" -ForegroundColor Yellow
Write-Host "   - Detener debug (Shift+F5)" -ForegroundColor White
Write-Host "   - Iniciar de nuevo (F5)" -ForegroundColor White
Write-Host ""
Write-Host "4. Accede al panel de diagnóstico:" -ForegroundColor Yellow
Write-Host "   - https://localhost:PUERTO/Test/AiTest" -ForegroundColor White
Write-Host "   - Presiona el botón 'Generar Respuesta IA'" -ForegroundColor White
Write-Host ""
Write-Host "5. O usa la API directamente:" -ForegroundColor Yellow
Write-Host "   - GET https://localhost:PUERTO/api/test/ai/status" -ForegroundColor White
Write-Host "   - POST https://localhost:PUERTO/api/test/ai/force-latest" -ForegroundColor White
Write-Host ""

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "FIN DE VERIFICACIÓN" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
