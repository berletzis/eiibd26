#!/usr/bin/env pwsh
# ====================================================================
# COMANDOS RÁPIDOS DE SEGURIDAD - EIIBD26
# ====================================================================
# Copia y pega estos comandos según necesites
# ====================================================================

Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║           🔐 COMANDOS RÁPIDOS DE SEGURIDAD                   ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Host ""
Write-Host "Elige una opción:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 🔧 Configurar User Secrets (recomendado)" -ForegroundColor Green
Write-Host "2. 📋 Ver User Secrets actuales" -ForegroundColor Cyan
Write-Host "3. 🗑️  Limpiar appsettings.json del Git" -ForegroundColor Magenta
Write-Host "4. 🔨 Limpiar historial de Git (BFG)" -ForegroundColor Red
Write-Host "5. ✅ Verificar aplicación" -ForegroundColor Green
Write-Host "6. 📊 Verificar headers de seguridad" -ForegroundColor Blue
Write-Host "7. 🚀 Ejecutar aplicación" -ForegroundColor Green
Write-Host "0. ❌ Salir" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Selecciona (0-7)"

switch ($choice) {
    "1" {
        Write-Host "🔧 Ejecutando configuración de User Secrets..." -ForegroundColor Green
        & .\setup-secrets.ps1
    }
    
    "2" {
        Write-Host "📋 User Secrets actuales:" -ForegroundColor Cyan
        dotnet user-secrets list --project eiibd26
        Write-Host ""
        Write-Host "📍 Ubicación:" -ForegroundColor Cyan
        Write-Host "   $env:APPDATA\Microsoft\UserSecrets" -ForegroundColor Gray
    }
    
    "3" {
        Write-Host "🗑️  Limpiando appsettings.json del staging..." -ForegroundColor Magenta
        
        git rm --cached eiibd26/appsettings.json
        
        if (-not (Test-Path "eiibd26/appsettings.json")) {
            Copy-Item "eiibd26/appsettings.json.template" "eiibd26/appsettings.json"
            Write-Host "✅ Plantilla copiada a appsettings.json (edítala con tus credenciales)" -ForegroundColor Green
        }
        
        Write-Host ""
        Write-Host "⚠️  Ahora debes:" -ForegroundColor Yellow
        Write-Host "  1. Editar eiibd26/appsettings.json con tus NUEVAS credenciales" -ForegroundColor Yellow
        Write-Host "  2. Hacer commit:" -ForegroundColor Yellow
        Write-Host "     git add .gitignore eiibd26/appsettings.json.template" -ForegroundColor Gray
        Write-Host "     git commit -m 'security: Remove credentials from repo'" -ForegroundColor Gray
        Write-Host "     git push" -ForegroundColor Gray
    }
    
    "4" {
        Write-Host "🔨 Limpieza de historial de Git" -ForegroundColor Red
        Write-Host ""
        Write-Host "⚠️  ADVERTENCIA: Esto reescribirá el historial de Git" -ForegroundColor Red
        Write-Host "    Todos los colaboradores deberán re-clonar el repositorio" -ForegroundColor Red
        Write-Host ""
        
        $confirm = Read-Host "¿Estás seguro? (escribe 'SI' para continuar)"
        
        if ($confirm -eq "SI") {
            Write-Host ""
            Write-Host "Opción 1 - BFG Repo-Cleaner (recomendado):" -ForegroundColor Cyan
            Write-Host "  1. Descarga BFG: https://rtyley.github.io/bfg-repo-cleaner/" -ForegroundColor Gray
            Write-Host "  2. Ejecuta:" -ForegroundColor Gray
            Write-Host "     java -jar bfg.jar --delete-files appsettings.json" -ForegroundColor Yellow
            Write-Host "     git reflog expire --expire=now --all" -ForegroundColor Yellow
            Write-Host "     git gc --prune=now --aggressive" -ForegroundColor Yellow
            Write-Host "     git push origin --force --all" -ForegroundColor Yellow
            Write-Host ""
            
            Write-Host "Opción 2 - git-filter-repo:" -ForegroundColor Cyan
            Write-Host "  1. Instala: pip install git-filter-repo" -ForegroundColor Gray
            Write-Host "  2. Ejecuta:" -ForegroundColor Gray
            Write-Host "     git filter-repo --path eiibd26/appsettings.json --invert-paths --force" -ForegroundColor Yellow
            Write-Host "     git push origin --force --all" -ForegroundColor Yellow
        } else {
            Write-Host "❌ Operación cancelada" -ForegroundColor Red
        }
    }
    
    "5" {
        Write-Host "✅ Verificando aplicación..." -ForegroundColor Green
        Write-Host ""
        
        # Verificar User Secrets
        Write-Host "1️⃣ User Secrets:" -ForegroundColor Cyan
        $secrets = dotnet user-secrets list --project eiibd26 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ Configurados" -ForegroundColor Green
        } else {
            Write-Host "   ❌ No configurados - ejecuta opción 1" -ForegroundColor Red
        }
        
        # Verificar .gitignore
        Write-Host "2️⃣ .gitignore:" -ForegroundColor Cyan
        $gitignoreContent = Get-Content ".gitignore" -Raw
        if ($gitignoreContent -match "appsettings\.json") {
            Write-Host "   ✅ Protege appsettings.json" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  No protege appsettings.json" -ForegroundColor Yellow
        }
        
        # Verificar archivos en staging
        Write-Host "3️⃣ Git staging:" -ForegroundColor Cyan
        $staged = git ls-files | Select-String "appsettings.json"
        if ($staged) {
            Write-Host "   ⚠️  appsettings.json está en el repo - ejecuta opción 3" -ForegroundColor Yellow
        } else {
            Write-Host "   ✅ appsettings.json no está trackeado" -ForegroundColor Green
        }
        
        # Verificar compilación
        Write-Host "4️⃣ Compilación:" -ForegroundColor Cyan
        Write-Host "   Compilando..." -ForegroundColor Gray
        $buildOutput = dotnet build eiibd26 --no-incremental 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ Sin errores" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Hay errores de compilación" -ForegroundColor Red
            Write-Host $buildOutput
        }
        
        Write-Host ""
    }
    
    "6" {
        Write-Host "📊 Verificando headers de seguridad..." -ForegroundColor Blue
        Write-Host ""
        Write-Host "⚠️  La aplicación debe estar ejecutándose" -ForegroundColor Yellow
        Write-Host ""
        
        $url = Read-Host "URL de la app (default: https://localhost:5001)"
        if ([string]::IsNullOrWhiteSpace($url)) {
            $url = "https://localhost:5001"
        }
        
        try {
            $response = Invoke-WebRequest -Uri $url -Method HEAD -SkipCertificateCheck
            
            Write-Host "Headers de seguridad encontrados:" -ForegroundColor Green
            Write-Host ""
            
            $securityHeaders = @(
                "X-Frame-Options",
                "X-Content-Type-Options",
                "X-XSS-Protection",
                "Referrer-Policy",
                "Content-Security-Policy",
                "Strict-Transport-Security",
                "Permissions-Policy"
            )
            
            foreach ($header in $securityHeaders) {
                if ($response.Headers[$header]) {
                    Write-Host "  ✅ $header : $($response.Headers[$header])" -ForegroundColor Green
                } else {
                    Write-Host "  ❌ $header : No configurado" -ForegroundColor Red
                }
            }
        } catch {
            Write-Host "❌ Error: La aplicación no está accesible en $url" -ForegroundColor Red
            Write-Host "   Asegúrate de que esté ejecutándose con 'dotnet run'" -ForegroundColor Yellow
        }
        
        Write-Host ""
    }
    
    "7" {
        Write-Host "🚀 Ejecutando aplicación..." -ForegroundColor Green
        Write-Host ""
        Write-Host "Presiona Ctrl+C para detener" -ForegroundColor Yellow
        Write-Host ""
        
        Set-Location eiibd26
        dotnet run
    }
    
    "0" {
        Write-Host "👋 ¡Hasta luego!" -ForegroundColor Gray
        exit
    }
    
    default {
        Write-Host "❌ Opción inválida" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Presiona Enter para continuar..." -ForegroundColor Gray
Read-Host
