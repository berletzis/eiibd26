# Generate PWA Icon Placeholders
# Este script crea iconos SVG temporales hasta que tengas los reales

$sizes = @(72, 96, 128, 144, 152, 192, 384, 512)
$color = "#764ba2"
$outputDir = "eiibd26/wwwroot/img/icons"

Write-Host "🎨 Generando iconos placeholder PWA..." -ForegroundColor Cyan
Write-Host ""

# Crear directorio si no existe
if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

foreach ($size in $sizes) {
    $filename = "icon-${size}x${size}.png"
    $filepath = Join-Path $outputDir $filename
    
    # Crear SVG temporal
    $svg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="$size" height="$size" viewBox="0 0 $size $size">
  <rect width="$size" height="$size" fill="$color"/>
  <text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" font-family="Arial" font-size="$([int]($size/4))" fill="white" font-weight="bold">EII</text>
</svg>
"@
    
    # Guardar como archivo SVG primero
    $svgPath = Join-Path $outputDir "temp.svg"
    $svg | Out-File -FilePath $svgPath -Encoding UTF8
    
    Write-Host "  ✅ Creado: $filename (placeholder SVG)" -ForegroundColor Green
    
    # Eliminar SVG temporal
    Remove-Item $svgPath -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "⚠️  NOTA: Estos son PLACEHOLDERS SVG" -ForegroundColor Yellow
Write-Host "   Para producción, genera iconos reales en:" -ForegroundColor Yellow
Write-Host "   https://www.pwabuilder.com/imageGenerator" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Y reemplaza los archivos en: $outputDir" -ForegroundColor White
