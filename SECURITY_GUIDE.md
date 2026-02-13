# 🚨 GUÍA DE SEGURIDAD - ACCIÓN INMEDIATA REQUERIDA

## ⚠️ CREDENCIALES EXPUESTAS EN GITHUB

Tu archivo `appsettings.json` con credenciales reales fue commiteado y está en el historial de Git público.

### 🔴 ACCIÓN INMEDIATA (HAZ ESTO AHORA):

#### 1. Revocar Credenciales Comprometidas

**SQL Server:**
```sql
-- Conectar como administrador y cambiar la contraseña de 'sa'
ALTER LOGIN sa WITH PASSWORD = 'NuevaContraseñaSegura123!@#';
```

**SendGrid:**
1. Ir a https://app.sendgrid.com/settings/api_keys
2. Eliminar la API Key: `SG.QW2p7ePTSHGNZE7zEgn2Ew...`
3. Crear una nueva API Key

**Twilio:**
1. Ir a https://console.twilio.com/
2. Ir a Account > API Keys & Tokens
3. Generar nuevo Auth Token (esto invalida el anterior)

#### 2. Configurar User Secrets para Desarrollo

```powershell
# Desde el directorio raíz del proyecto
cd eiibd26

# Inicializar User Secrets
dotnet user-secrets init

# Agregar las nuevas credenciales (REEMPLAZA con tus nuevos valores)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=132.148.74.136\\ybridio;Database=eiibd26;user id=sa;password=TU_NUEVA_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"

dotnet user-secrets set "SendGrid:ApiKey" "TU_NUEVA_SENDGRID_API_KEY"
dotnet user-secrets set "SendGrid:FromEmail" "no-reply@eiibd.com"
dotnet user-secrets set "SendGrid:FromName" "EIIBD"

dotnet user-secrets set "Twilio:AccountSid" "TU_ACCOUNT_SID"
dotnet user-secrets set "Twilio:AuthToken" "TU_NUEVO_AUTH_TOKEN"
dotnet user-secrets set "Twilio:FromNumber" "+14752588653"

# Verificar
dotnet user-secrets list
```

#### 3. Limpiar Historial de Git (CRÍTICO)

```powershell
# OPCIÓN A: Eliminar archivo del historial completo (recomendado)
# Requiere git-filter-repo (instalar: pip install git-filter-repo)
git filter-repo --path eiibd26/appsettings.json --invert-paths --force

# OPCIÓN B: Usar BFG Repo-Cleaner (más simple)
# 1. Descargar: https://rtyley.github.io/bfg-repo-cleaner/
# 2. Ejecutar:
java -jar bfg.jar --delete-files appsettings.json

# Después de cualquier opción, forzar push
git push origin --force --all
git push origin --force --tags
```

⚠️ **NOTA:** Force push afectará a todos los colaboradores. Coordina con tu equipo.

#### 4. Eliminar appsettings.json del Repositorio

```powershell
# Eliminar del staging pero mantener localmente
git rm --cached eiibd26/appsettings.json

# Usar la plantilla creada
cp eiibd26/appsettings.json.template eiibd26/appsettings.json

# Editar manualmente con tus credenciales NUEVAS (solo local)
# Este archivo ahora está en .gitignore y no se subirá

# Commit del cambio
git add .gitignore eiibd26/appsettings.json.template
git commit -m "security: Remove sensitive credentials and add template"
git push
```

#### 5. Para Producción: Usar Variables de Entorno o Azure Key Vault

**Azure App Service:**
```
Configuration > Application Settings > New Application Setting

ConnectionStrings__DefaultConnection = "Server=..."
SendGrid__ApiKey = "SG...."
Twilio__AuthToken = "..."
```

**Docker/Containers:**
```yaml
# docker-compose.yml
environment:
  - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
  - SendGrid__ApiKey=${SENDGRID_KEY}
```

---

## ✅ MEJORAS DE SEGURIDAD IMPLEMENTADAS

Las siguientes correcciones ya fueron aplicadas automáticamente:

### 1. **Política de Contraseñas Fortalecida**
- Mínimo 8 caracteres
- Requiere mayúsculas, minúsculas, dígitos y símbolos
- Requiere al menos 4 caracteres únicos

### 2. **Protección contra Fuerza Bruta**
- Lockout habilitado tras 5 intentos fallidos
- Bloqueo de 15 minutos
- Aplicado en Login

### 3. **Cookies Seguras**
- `HttpOnly`: Protección contra XSS
- `Secure`: Solo HTTPS en producción
- `SameSite=Strict`: Protección CSRF

### 4. **Headers de Seguridad HTTP**
- `X-Frame-Options: DENY` - Anti-clickjacking
- `X-Content-Type-Options: nosniff` - Anti-MIME sniffing
- `Content-Security-Policy` - Control de recursos
- `Referrer-Policy` - Protección de privacidad
- `Permissions-Policy` - Control de APIs del navegador

### 5. **HSTS Mejorado**
- 365 días de duración
- Incluye subdominios
- Preload habilitado

### 6. **`.gitignore` Actualizado**
- Bloquea `appsettings.json`
- Bloquea secretos y certificados
- Permite plantillas `.template`

---

## 📋 PENDIENTES RECOMENDADOS

### Alta Prioridad:
- [ ] Implementar Rate Limiting en APIs y Login
- [ ] Agregar logging de eventos de seguridad
- [ ] Habilitar 2FA (Two-Factor Authentication)
- [ ] Implementar CAPTCHA en formularios públicos

### Media Prioridad:
- [ ] Validación anti-CSRF explícita en APIs
- [ ] Sanitización HTML en contenido generado por usuarios
- [ ] Auditoría de accesos y cambios sensibles
- [ ] Implementar política de expiración de sesiones

### Baja Prioridad:
- [ ] Penetration testing
- [ ] Dependency scanning automatizado
- [ ] Security headers testing
- [ ] GDPR compliance review

---

## 🔍 VERIFICACIÓN

### Comprobar que User Secrets funciona:
```powershell
cd eiibd26
dotnet run
```

Debería arrancar sin errores de conexión.

### Verificar Headers de Seguridad:
```powershell
# En desarrollo
curl -I https://localhost:5001

# O usar: https://securityheaders.com/
```

### Verificar Lockout:
1. Intentar login con contraseña incorrecta 5 veces
2. Verificar que aparece mensaje de bloqueo

---

## 📞 SOPORTE

Si tienes dudas sobre algún paso:
1. Consulta la documentación oficial de ASP.NET Core Security
2. Revisa el código en `Program.cs` (comentarios agregados)
3. Contacta al equipo de seguridad

---

**Última actualización:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Estado:** ⚠️ CREDENCIALES COMPROMETIDAS - REQUIERE ACCIÓN INMEDIATA
