
# 🔍 Diagnóstico SendGrid
Write-Host "🔍 Diagnóstico SendGrid" -ForegroundColor Cyan

# 1. Verificar secrets
Write-Host "`n1. Verificando User Secrets..." -ForegroundColor Yellow
Push-Location "eiibd26"
try {
    $secrets = dotnet user-secrets list 2>&1 | Out-String
    if ($secrets -match "SendGrid:ApiKey") {
        Write-Host "   ✅ SendGrid:ApiKey configurado" -ForegroundColor Green
        
        # Verificar que no esté vacío
        if ($secrets -match "SendGrid:ApiKey = \S+") {
            Write-Host "   ✅ API Key tiene valor" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  API Key está vacío" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ❌ SendGrid:ApiKey NO configurado" -ForegroundColor Red
        Write-Host "   Ejecuta: dotnet user-secrets set 'SendGrid:ApiKey' 'SG.TU_KEY'" -ForegroundColor Yellow
    }
    
    if ($secrets -match "SendGrid:FromEmail") {
        Write-Host "   ✅ SendGrid:FromEmail configurado" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  SendGrid:FromEmail NO configurado (usará default)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Error al leer secrets: $_" -ForegroundColor Red
}

# 2. Verificar appsettings
Write-Host "`n2. Verificando appsettings.json..." -ForegroundColor Yellow
try {
    $appsettings = Get-Content "appsettings.json" -Raw -ErrorAction Stop
    if ($appsettings -match '"SendGrid"') {
        Write-Host "   ⚠️  SendGrid encontrado en appsettings.json" -ForegroundColor Yellow
        Write-Host "   (API Key debe estar en secrets, no en appsettings)" -ForegroundColor Gray
        
        # Verificar si ApiKey está hardcoded (malo)
        if ($appsettings -match '"ApiKey"\s*:\s*"SG\.') {
            Write-Host "   ❌ API KEY HARDCODED en appsettings.json (INSEGURO)" -ForegroundColor Red
        }
    } else {
        Write-Host "   ✅ SendGrid no en appsettings.json (correcto, usar secrets)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠️  No se pudo leer appsettings.json: $_" -ForegroundColor Yellow
}

# 3. Verificar registro de IEmailSender
Write-Host "`n3. Verificando registro en Program.cs..." -ForegroundColor Yellow
try {
    $program = Get-Content "Program.cs" -Raw -ErrorAction Stop
    if ($program -match "AddTransient<IEmailSender,\s*SendGridEmailSender>") {
        Write-Host "   ✅ IEmailSender registrado correctamente" -ForegroundColor Green
    } else {
        Write-Host "   ❌ IEmailSender NO registrado" -ForegroundColor Red
        Write-Host "   Debe tener: builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Error al leer Program.cs: $_" -ForegroundColor Red
}

# 4. Verificar que SendGridEmailSender existe
Write-Host "`n4. Verificando archivo SendGridEmailSender.cs..." -ForegroundColor Yellow
if (Test-Path "Services\SendGridEmailSender.cs") {
    Write-Host "   ✅ SendGridEmailSender.cs existe" -ForegroundColor Green
} else {
    Write-Host "   ❌ SendGridEmailSender.cs NO encontrado" -ForegroundColor Red
}

# 5. Verificar Output Window (si está en debug)
Write-Host "`n5. Instrucciones para ver logs:" -ForegroundColor Yellow
Write-Host "   - Ejecuta la aplicación (F5)" -ForegroundColor White
Write-Host "   - Ve a: Debug → Windows → Output" -ForegroundColor White
Write-Host "   - Filtra por: 'SendGrid'" -ForegroundColor White
Write-Host "   - Busca líneas como:" -ForegroundColor White
Write-Host "     ✅ 'SendGrid email sent to...'" -ForegroundColor Green
Write-Host "     ⚠️  'SendGrid API key not configured'" -ForegroundColor Yellow
Write-Host "     ❌ 'SendGrid send failed. Status: ...'" -ForegroundColor Red

Write-Host "`n✅ Diagnóstico completado" -ForegroundColor Cyan
Write-Host "`n📋 Próximos pasos:" -ForegroundColor Yellow
Write-Host "1. Si API Key no está configurado:" -ForegroundColor White
Write-Host "   dotnet user-secrets set 'SendGrid:ApiKey' 'SG.TU_KEY'" -ForegroundColor Gray
Write-Host "   dotnet user-secrets set 'SendGrid:FromEmail' 'no-reply@eiibd.com'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Reinicia la aplicación (Shift+F5, luego F5)" -ForegroundColor White
Write-Host ""
Write-Host "3. Prueba recuperar contraseña:" -ForegroundColor White
Write-Host "   https://localhost:7002/Identity/Account/ForgotPassword" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Revisa Activity Feed en SendGrid:" -ForegroundColor White
Write-Host "   https://app.sendgrid.com/email_activity" -ForegroundColor Gray

Pop-Location
