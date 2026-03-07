# ========================================
# Script de Deploy con Limpieza de Cache
# ========================================

Write-Host "🚀 Iniciando proceso de deploy..." -ForegroundColor Cyan
Write-Host ""

# 1. Limpiar
Write-Host "🧹 Limpiando build anterior..." -ForegroundColor Yellow
dotnet clean eiibd26 --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error en clean" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Clean completado" -ForegroundColor Green
Write-Host ""

# 2. Build
Write-Host "🏗️ Compilando en Release..." -ForegroundColor Yellow
dotnet build eiibd26 --configuration Release --no-incremental
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error en build" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build exitoso" -ForegroundColor Green
Write-Host ""

# 3. Publish
Write-Host "📦 Publicando aplicación..." -ForegroundColor Yellow
$publishPath = ".\publish"
if (Test-Path $publishPath) {
    Write-Host "   Eliminando carpeta publish anterior..." -ForegroundColor DarkYellow
    Remove-Item $publishPath -Recurse -Force
}
dotnet publish eiibd26 --configuration Release --output $publishPath --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error en publish" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Publish completado" -ForegroundColor Green
Write-Host ""

# 4. Verificar bundle
Write-Host "🔍 Verificando bundle.min.css..." -ForegroundColor Yellow
$bundlePath = Join-Path $publishPath "wwwroot\css\bundle.min.css"
if (Test-Path $bundlePath) {
    $bundleSize = (Get-Item $bundlePath).Length
    $bundleSizeKB = [math]::Round($bundleSize / 1024, 2)
    Write-Host "✅ Bundle encontrado: $bundleSizeKB KB" -ForegroundColor Green
    
    # Verificar que contenga los nuevos estilos
    $bundleContent = Get-Content $bundlePath -Raw
    $checks = @(
        @{Name="Sidebar"; Pattern="article-index-sidebar"},
        @{Name="Rating"; Pattern="rating-btn"},
        @{Name="Cards"; Pattern="card-horizontal"},
        @{Name="Preguntas"; Pattern="se-item"}
    )
    
    Write-Host ""
    Write-Host "   Verificando contenido del bundle:" -ForegroundColor Cyan
    foreach ($check in $checks) {
        if ($bundleContent -match $check.Pattern) {
            Write-Host "   ✅ $($check.Name): Incluido" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️ $($check.Name): NO encontrado" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "⚠️ Bundle NO encontrado - se generará en primera ejecución" -ForegroundColor Yellow
}
Write-Host ""

# 5. Verificar archivos críticos
Write-Host "📋 Verificando archivos críticos..." -ForegroundColor Yellow
$criticalFiles = @(
    "wwwroot\css\detalle.css",
    "wwwroot\css\contenidos-cards.css",
    "wwwroot\css\preguntas.css",
    "wwwroot\manifest.json",
    "wwwroot\service-worker.js",
    "wwwroot\js\pwa.js"
)

foreach ($file in $criticalFiles) {
    $fullPath = Join-Path $publishPath $file
    if (Test-Path $fullPath) {
        Write-Host "   ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $file FALTA" -ForegroundColor Red
    }
}
Write-Host ""

# 6. Resumen
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ DEPLOY LISTO PARA PRODUCCIÓN" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📦 Carpeta de output: $publishPath" -ForegroundColor White
Write-Host ""
Write-Host "📋 PRÓXIMOS PASOS:" -ForegroundColor Cyan
Write-Host "   1. Copiar carpeta '$publishPath' al servidor" -ForegroundColor White
Write-Host "   2. Ejecutar SQL: Data\Migrations\SQL\CreateArticleRatingsTable.sql" -ForegroundColor White
Write-Host "   3. Hard refresh en navegador (Ctrl+Shift+R)" -ForegroundColor White
Write-Host "   4. Limpiar CDN cache (si aplica)" -ForegroundColor White
Write-Host ""
Write-Host "🧪 VERIFICAR:" -ForegroundColor Yellow
Write-Host "   • Bundle se carga: https://tu-dominio.com/css/bundle.min.css" -ForegroundColor White
Write-Host "   • Estilos se aplican correctamente" -ForegroundColor White
Write-Host "   • Rating API: https://tu-dominio.com/api/articles/1/rating" -ForegroundColor White
Write-Host "   • PWA Test: https://tu-dominio.com/pwa-test.html" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentación: CSS-BUNDLE-FIX.md" -ForegroundColor Cyan
Write-Host ""
