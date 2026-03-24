# SECRETS.md — Gestión de Secretos y Credenciales

## ⚠️ Aviso Importante

**Ninguna credencial real debe aparecer en este repositorio.**  
`appsettings.json` contiene únicamente valores vacíos `""` para las claves sensibles.  
Los valores reales se configuran según el entorno como se describe a continuación.

---

## Secrets requeridos

| Clave de Configuración | Descripción | Obligatorio |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión SQL Server | ✅ |
| `SendGrid:ApiKey` | API Key de SendGrid (envío de emails) | ✅ |
| `AiAnswer:AnthropicApiKey` | API Key de Anthropic Claude (respuestas IA) | ✅ |
| `VapidKeys:PublicKey` | Clave pública VAPID (notificaciones push) | ✅ |
| `VapidKeys:PrivateKey` | Clave privada VAPID (notificaciones push) | ✅ |
| `Twilio:AccountSid` | Account SID de Twilio (SMS) | ✅ |
| `Twilio:AuthToken` | Auth Token de Twilio (SMS) | ✅ |
| `Twilio:FromNumber` | Número de teléfono Twilio | ✅ |

---

## 🖥️ Desarrollo Local — User Secrets

### Paso 1: Inicializar (ya configurado en el proyecto)
```bash
dotnet user-secrets init --project eiibd26/eiibd26.csproj
```

### Paso 2: Cargar cada secreto
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=eiibd26;user id=...;password=...;TrustServerCertificate=True" --project eiibd26/eiibd26.csproj

dotnet user-secrets set "SendGrid:ApiKey" "SG.xxxx" --project eiibd26/eiibd26.csproj
dotnet user-secrets set "SendGrid:FromEmail" "no-reply@eiibd.com" --project eiibd26/eiibd26.csproj
dotnet user-secrets set "SendGrid:FromName" "EIIBD" --project eiibd26/eiibd26.csproj

dotnet user-secrets set "Twilio:AccountSid" "ACxxxx" --project eiibd26/eiibd26.csproj
dotnet user-secrets set "Twilio:AuthToken" "xxxx" --project eiibd26/eiibd26.csproj
dotnet user-secrets set "Twilio:FromNumber" "+1xxxx" --project eiibd26/eiibd26.csproj

dotnet user-secrets set "VapidKeys:PublicKey" "xxxx" --project eiibd26/eiibd26.csproj
dotnet user-secrets set "VapidKeys:PrivateKey" "xxxx" --project eiibd26/eiibd26.csproj

dotnet user-secrets set "AiAnswer:AnthropicApiKey" "sk-ant-api03-xxxx" --project eiibd26/eiibd26.csproj
```

### Verificar
```bash
dotnet user-secrets list --project eiibd26/eiibd26.csproj
```

### Ubicación del archivo local (NO versionar)
```
Windows: %APPDATA%\Microsoft\UserSecrets\aspnet-eiibd26-af0d7b28-f236-4a39-aa6a-bc25536708d7\secrets.json
Linux/Mac: ~/.microsoft/usersecrets/aspnet-eiibd26-af0d7b28-f236-4a39-aa6a-bc25536708d7/secrets.json
```

---

## 🚀 Producción — Variables de Entorno

En producción, configurar cada secreto como variable de entorno.  
.NET Core usa `__` (doble guión bajo) como separador de secciones.

### Ejemplo — IIS (web.config / applicationHost.config)
```xml
<environmentVariables>
  <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=...;..." />
  <environmentVariable name="SendGrid__ApiKey" value="SG.xxxx" />
  <environmentVariable name="AiAnswer__AnthropicApiKey" value="sk-ant-api03-xxxx" />
  <environmentVariable name="VapidKeys__PublicKey" value="xxxx" />
  <environmentVariable name="VapidKeys__PrivateKey" value="xxxx" />
  <environmentVariable name="Twilio__AccountSid" value="ACxxxx" />
  <environmentVariable name="Twilio__AuthToken" value="xxxx" />
  <environmentVariable name="Twilio__FromNumber" value="+1xxxx" />
</environmentVariables>
```

### Ejemplo — Docker
```bash
docker run -e "ConnectionStrings__DefaultConnection=Server=..." \
           -e "SendGrid__ApiKey=SG.xxxx" \
           -e "AiAnswer__AnthropicApiKey=sk-ant-api03-xxxx" \
           eiibd26
```

### Ejemplo — GitHub Actions / CI CD
```yaml
env:
  ConnectionStrings__DefaultConnection: ${{ secrets.DB_CONNECTION_STRING }}
  SendGrid__ApiKey: ${{ secrets.SENDGRID_API_KEY }}
  AiAnswer__AnthropicApiKey: ${{ secrets.ANTHROPIC_API_KEY }}
  VapidKeys__PublicKey: ${{ secrets.VAPID_PUBLIC_KEY }}
  VapidKeys__PrivateKey: ${{ secrets.VAPID_PRIVATE_KEY }}
  Twilio__AccountSid: ${{ secrets.TWILIO_ACCOUNT_SID }}
  Twilio__AuthToken: ${{ secrets.TWILIO_AUTH_TOKEN }}
  Twilio__FromNumber: ${{ secrets.TWILIO_FROM_NUMBER }}
```

---

## 🔑 Generar nuevas VAPID Keys

```bash
npx web-push generate-vapid-keys
```

---

## 🧹 Limpiar historial de git (si se commitieron credenciales)

Si las credenciales ya fueron commitadas, **rotarlas primero** y luego limpiar el historial:

```bash
# 1. Dejar de trackear appsettings.json (si accidentalmente fue commitado con credenciales)
git rm --cached eiibd26/appsettings.json

# 2. Commit del cambio
git commit -m "chore: remove appsettings.json from tracking"

# 3. Para limpiar historial completo (requiere BFG Repo Cleaner o git filter-repo)
# https://rtyley.github.io/bfg-repo-cleaner/
```

> ⚠️ **Rotar todas las credenciales comprometidas** antes o inmediatamente después de limpiar el historial.

---

## ✅ Validación al inicio de la aplicación

La clase `SecretsValidator` verifica automáticamente al arrancar:

- **Producción**: si falta algún secret crítico → **la app NO inicia** (`InvalidOperationException`)
- **Desarrollo**: si falta algún secret → **warning en log**, la app continúa degradada

Comportamiento de servicios sin credenciales configuradas:
- **SendGrid**: no envía emails, solo loguea (warning)
- **Twilio**: no envía SMS, solo loguea (warning)  
- **Push Notifications**: falla silenciosamente
- **AI (Anthropic)**: respuestas deshabilitadas
