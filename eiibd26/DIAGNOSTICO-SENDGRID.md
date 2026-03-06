# 🔍 Diagnóstico de SendGrid - Recuperación de Contraseña

## Problema Reportado
Los correos de recuperación de contraseña **NO están llegando** usando SendGrid.

---

## ✅ Checklist de Diagnóstico

### 1. **Verificar Configuración de SendGrid**

#### En `appsettings.json` o User Secrets:
```json
{
  "SendGrid": {
    "ApiKey": "SG.xxxxx...",  // ¿Está configurado?
    "FromEmail": "no-reply@tudominio.com",
    "FromName": "EIIBD",
    "DefaultCategories": ["password-reset", "identity"]
  }
}
```

**Comandos para verificar:**
```powershell
# Ver secrets configurados
dotnet user-secrets list --project eiibd26

# Buscar en appsettings.json
Select-String -Path "eiibd26\appsettings*.json" -Pattern "SendGrid"
```

---

### 2. **Verificar Logs de la Aplicación**

**Buscar en logs:**
- ✅ `"SendGrid email sent to..."` → Email se envió correctamente
- ⚠️ `"SendGrid API key not configured"` → API Key faltante
- ❌ `"SendGrid send failed. Status: ..."` → Error de SendGrid
- ❌ `"Exception sending email via SendGrid"` → Excepción

**Ver logs en Output Window:**
- Debug → Windows → Output
- Show output from: "Debug"
- Filtrar por: `SendGrid`

---

### 3. **Problemas Comunes**

#### A. **API Key No Configurada**
```
⚠️ SendGrid API key not configured. Email was not sent.
```

**Solución:**
```powershell
# Configurar API Key en User Secrets
cd eiibd26
dotnet user-secrets set "SendGrid:ApiKey" "SG.TU_API_KEY_AQUI"
dotnet user-secrets set "SendGrid:FromEmail" "no-reply@eiibd.com"
dotnet user-secrets set "SendGrid:FromName" "EIIBD Comunidad"
```

#### B. **Email del Remitente No Verificado**
```
❌ SendGrid send failed. Status: 403
Body: {"errors":[{"message":"The from address does not match a verified Sender Identity"}]}
```

**Solución:**
1. Ir a SendGrid Dashboard
2. Settings → Sender Authentication
3. Verificar el email `no-reply@tudominio.com`
4. O crear un "Single Sender Verification"

#### C. **Email No Confirmado en Identity**
El código tiene esta validación:
```csharp
if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
{
    return RedirectToPage("./ForgotPasswordConfirmation");
}
```

**Solución:** Confirmar email primero desde el link de registro.

#### D. **API Key Inválida o Revocada**
```
❌ SendGrid send failed. Status: 401
Body: {"errors":[{"message":"The provided authorization grant is invalid, expired, or revoked"}]}
```

**Solución:** Generar nueva API Key en SendGrid Dashboard.

---

### 4. **Testing Manual**

#### Endpoint de Test (crear temporalmente):
```csharp
// En Program.cs o un TestController
app.MapGet("/test-email", async (IEmailSender emailSender) =>
{
    try
    {
        await emailSender.SendEmailAsync(
            "tu-email@gmail.com",
            "🧪 Test desde EIIBD",
            "<h1>Test Email</h1><p>Si recibes esto, SendGrid funciona.</p>");
        
        return Results.Ok("Email enviado. Revisa tu bandeja de entrada.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}");
    }
}).RequireAuthorization(); // Solo en desarrollo
```

---

### 5. **Verificar en SendGrid Dashboard**

**Activity Feed:**
1. Ir a https://app.sendgrid.com/email_activity
2. Buscar por email de destino
3. Ver status:
   - **Processed** → SendGrid lo recibió
   - **Delivered** → Llegó al servidor del destinatario
   - **Dropped** → Rechazado (email no verificado)
   - **Bounced** → Email no existe

---

### 6. **Logs Detallados en Código**

El código actual ya tiene logs, pero puedes agregar más detalle:

```csharp
// En SendGridEmailSender.cs línea 102
var response = await client.SendEmailAsync(msg).ConfigureAwait(false);

// ⭐ AGREGAR DESPUÉS:
_logger.LogInformation(
    "📧 SendGrid Response: Status={Status}, Headers={Headers}", 
    response.StatusCode, 
    string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));
```

---

## 🛠️ Script de Diagnóstico Rápido

```powershell
# Guardar como: test-sendgrid.ps1

Write-Host "🔍 Diagnóstico SendGrid" -ForegroundColor Cyan

# 1. Verificar secrets
Write-Host "`n1. Verificando User Secrets..." -ForegroundColor Yellow
cd eiibd26
$secrets = dotnet user-secrets list 2>&1
if ($secrets -match "SendGrid:ApiKey") {
    Write-Host "   ✅ SendGrid:ApiKey configurado" -ForegroundColor Green
} else {
    Write-Host "   ❌ SendGrid:ApiKey NO configurado" -ForegroundColor Red
    Write-Host "   Ejecuta: dotnet user-secrets set 'SendGrid:ApiKey' 'SG.TU_KEY'" -ForegroundColor Yellow
}

# 2. Verificar appsettings
Write-Host "`n2. Verificando appsettings.json..." -ForegroundColor Yellow
$appsettings = Get-Content "appsettings.json" -Raw
if ($appsettings -match "SendGrid") {
    Write-Host "   ⚠️  SendGrid encontrado en appsettings.json" -ForegroundColor Yellow
    Write-Host "   (Recuerda: API Key debe estar en secrets, no en appsettings)" -ForegroundColor Gray
} else {
    Write-Host "   ✅ SendGrid no está en appsettings.json (correcto)" -ForegroundColor Green
}

# 3. Verificar registro de IEmailSender
Write-Host "`n3. Verificando registro en Program.cs..." -ForegroundColor Yellow
$program = Get-Content "Program.cs" -Raw
if ($program -match "AddTransient<IEmailSender, SendGridEmailSender>") {
    Write-Host "   ✅ IEmailSender registrado correctamente" -ForegroundColor Green
} else {
    Write-Host "   ❌ IEmailSender NO registrado" -ForegroundColor Red
}

Write-Host "`n✅ Diagnóstico completado" -ForegroundColor Cyan
Write-Host "`nPróximos pasos:" -ForegroundColor Yellow
Write-Host "1. Ejecuta la aplicación y busca en Output Window por 'SendGrid'" -ForegroundColor White
Write-Host "2. Intenta recuperar contraseña desde /Identity/Account/ForgotPassword" -ForegroundColor White
Write-Host "3. Revisa logs en Output Window (Debug → Windows → Output)" -ForegroundColor White
```

---

## 🔑 Obtener API Key de SendGrid

Si no tienes API Key:

1. Ir a https://app.sendgrid.com/settings/api_keys
2. Click "Create API Key"
3. Name: `eiibd-production` (o `eiibd-dev`)
4. Permissions: **Full Access** (o solo Mail Send)
5. Copiar el key (empieza con `SG.`)
6. Guardar en User Secrets:
```powershell
dotnet user-secrets set "SendGrid:ApiKey" "SG.TU_KEY_COPIADO"
```

---

## ⚡ Solución Rápida

**Si tienes prisa, ejecuta esto:**

```powershell
# 1. Configurar SendGrid
cd eiibd26
dotnet user-secrets set "SendGrid:ApiKey" "SG.TU_API_KEY"
dotnet user-secrets set "SendGrid:FromEmail" "no-reply@eiibd.com"
dotnet user-secrets set "SendGrid:FromName" "EIIBD"

# 2. Reiniciar aplicación
# Detener (Shift+F5) y volver a ejecutar (F5)

# 3. Probar recuperación de contraseña
# Ir a: https://localhost:7002/Identity/Account/ForgotPassword
```

---

## 📞 ¿Sigue sin funcionar?

**Comparte estos datos:**
1. Logs de Output Window (filtrar por "SendGrid")
2. Status en SendGrid Activity Feed
3. ¿Email está verificado en SendGrid?
4. ¿User Secrets configurados? (`dotnet user-secrets list`)

---

**Archivo creado:** `eiibd26/DIAGNOSTICO-SENDGRID.md`

**Ejecuta el script:**
```powershell
cd eiibd26
.\test-sendgrid.ps1
```
