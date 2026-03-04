# Quick PNG Icon Generator usando .NET Drawing
Add-Type -AssemblyName System.Drawing

$sizes = @(72, 96, 128, 144, 152, 192, 384, 512)
$outputDir = "eiibd26\wwwroot\img\icons"

Write-Host "🎨 Generando iconos PNG PWA..." -ForegroundColor Cyan

# Crear directorio
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

foreach ($size in $sizes) {
    $filename = "icon-${size}x${size}.png"
    $filepath = Join-Path $outputDir $filename
    
    # Crear bitmap
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bmp)
    
    # Background morado
    $purple = [System.Drawing.Color]::FromArgb(118, 75, 162) # #764ba2
    $brush = New-Object System.Drawing.SolidBrush($purple)
    $graphics.FillRectangle($brush, 0, 0, $size, $size)
    
    # Texto "EII" blanco
    $white = [System.Drawing.Color]::White
    $textBrush = New-Object System.Drawing.SolidBrush($white)
    $fontSize = [int]($size / 3.5)
    $font = New-Object System.Drawing.Font("Arial", $fontSize, [System.Drawing.FontStyle]::Bold)
    
    # Centrar texto
    $text = "EII"
    $textSize = $graphics.MeasureString($text, $font)
    $x = ($size - $textSize.Width) / 2
    $y = ($size - $textSize.Height) / 2
    
    $graphics.DrawString($text, $font, $textBrush, $x, $y)
    
    # Guardar PNG
    $bmp.Save($filepath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Limpiar
    $graphics.Dispose()
    $bmp.Dispose()
    $brush.Dispose()
    $textBrush.Dispose()
    $font.Dispose()
    
    Write-Host "  ✅ $filename" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎉 Iconos generados en: $outputDir" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  Para producción, reemplaza con iconos reales de:" -ForegroundColor Yellow
Write-Host "   https://www.pwabuilder.com/imageGenerator" -ForegroundColor Cyan
