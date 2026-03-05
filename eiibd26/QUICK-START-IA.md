# 🚀 PASOS PARA ACTIVAR LA IA - GUÍA RÁPIDA

## ✅ FRONTEND YA ESTÁ LISTO

El badge de IA y todo el HTML/CSS ya están implementados.  
Solo necesitas configurar 3 cosas para que funcione:

---

## 🔑 PASO 1: OBTENER API KEY (5 minutos)

### 1. Ve a Anthropic Console
```
https://console.anthropic.com/
```

### 2. Sign Up o Sign In
- Usa tu email
- Verifica tu cuenta

### 3. Agregar Créditos (OBLIGATORIO)
```
https://console.anthropic.com/settings/billing
→ Add Credit
→ $5 USD mínimo (suficiente para ~500 respuestas)
```

### 4. Crear API Key
```
https://console.anthropic.com/settings/keys
→ Create Key
→ Copia la clave: sk-ant-api03-XXXXXXXX...
```

⚠️ **La clave se muestra solo UNA VEZ. Guárdala.**

---

## 📝 PASO 2: CONFIGURAR EN TU PROYECTO

### Abrir archivo:
```
eiibd26/appsettings.json
```

### Buscar esta línea (línea ~30):
```json
"AnthropicApiKey": "ANTHROPIC_API_KEY_AQUI",
```

### Reemplazar con tu clave real:
```json
"AnthropicApiKey": "sk-ant-api03-XXXXXXXXXXXXXXXXXXXXXXXX",
```

**Ejemplo real:**
```json
"AnthropicApiKey": "sk-ant-api03-xyz789abc456def123...",
```

---

## 👤 PASO 3: CREAR USUARIO DEL SISTEMA

### 3.1 Abrir SQL Server Management Studio

### 3.2 Conectar a tu servidor:
```
Server: 132.148.74.136\ybridio
Database: eiibd26
```

### 3.3 Ejecutar este script:

```sql
USE [eiibd26];
GO

-- Crear usuario del sistema para IA
DECLARE @UserId UNIQUEIDENTIFIER = NEWID();
DECLARE @Email NVARCHAR(256) = 'system-ai@eiibd.com';
DECLARE @UserName NVARCHAR(256) = 'Sistema IA';
DECLARE @NormalizedEmail NVARCHAR(256) = 'SYSTEM-AI@EIIBD.COM';
DECLARE @NormalizedUserName NVARCHAR(256) = 'SISTEMA IA';

-- Insertar en AspNetUsers
INSERT INTO [AspNetUsers] (
    [Id],
    [UserName],
    [NormalizedUserName],
    [Email],
    [NormalizedEmail],
    [EmailConfirmed],
    [PhoneNumberConfirmed],
    [TwoFactorEnabled],
    [LockoutEnabled],
    [AccessFailedCount]
)
VALUES (
    @UserId,
    @UserName,
    @NormalizedUserName,
    @Email,
    @NormalizedEmail,
    1, -- Email confirmado
    0, -- Teléfono no confirmado
    0, -- 2FA deshabilitado
    0, -- Lockout deshabilitado
    0  -- Access failed count
);

-- Mostrar el ID generado (CÓPIALO)
SELECT 
    @UserId AS UserId,
    'COPIA ESTE ID Y PÉGALO EN appsettings.json → SystemUserId' AS Instruccion;

PRINT '✅ Usuario del sistema creado exitosamente';
PRINT 'ID: ' + CAST(@UserId AS NVARCHAR(50));
PRINT '';
PRINT 'SIGUIENTE PASO:';
PRINT '1. Copia el ID de arriba';
PRINT '2. Abre appsettings.json';
PRINT '3. Busca "SystemUserId"';
PRINT '4. Reemplaza con el ID copiado';
```

### 3.4 Copiar el ID generado

Verás algo como:
```
abc12345-def6-7890-ghij-klmnopqrstuv
```

### 3.5 Actualizar appsettings.json

Buscar (línea ~41):
```json
"SystemUserId": "00000000-0000-0000-0000-000000000000",
```

Reemplazar con el ID copiado:
```json
"SystemUserId": "abc12345-def6-7890-ghij-klmnopqrstuv",
```

---

## 💾 PASO 4: EJECUTAR MIGRACIONES

### 4.1 Agregar Campos AI (si aún no lo hiciste)

```sql
USE [eiibd26];
GO

-- Verificar si los campos ya existen
SELECT TOP 1 
    CASE WHEN COL_LENGTH('Respuestas', 'EsIA') IS NOT NULL THEN '✅ YA EXISTE' ELSE '❌ FALTA' END AS EsIA,
    CASE WHEN COL_LENGTH('Respuestas', 'ModeloIA') IS NOT NULL THEN '✅ YA EXISTE' ELSE '❌ FALTA' END AS ModeloIA,
    CASE WHEN COL_LENGTH('Preguntas', 'TieneRespuestaIA') IS NOT NULL THEN '✅ YA EXISTE' ELSE '❌ FALTA' END AS TieneRespuestaIA
FROM Respuestas;

-- Si alguno dice "❌ FALTA", ejecuta:
ALTER TABLE Respuestas ADD EsIA bit NOT NULL DEFAULT 0;
ALTER TABLE Respuestas ADD ModeloIA nvarchar(100) NULL;
ALTER TABLE Respuestas ADD EsColapsada bit NOT NULL DEFAULT 0;
ALTER TABLE Respuestas ADD Puntuacion int NOT NULL DEFAULT 0;

ALTER TABLE Preguntas ADD TieneRespuestaIA bit NOT NULL DEFAULT 0;
ALTER TABLE Preguntas ADD FechaGeneracionIA datetimeoffset(7) NULL;

PRINT '✅ Campos AI agregados exitosamente';
```

### 4.2 Crear Constraint de Duplicados

```sql
USE [eiibd26];
GO

-- Verificar si ya existe
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'UX_Respuestas_OneAIAnswerPerQuestion' 
    AND object_id = OBJECT_ID('Respuestas')
)
BEGIN
    PRINT '⚠️ Constraint ya existe, saltando...';
END
ELSE
BEGIN
    -- Crear constraint
    CREATE UNIQUE NONCLUSTERED INDEX [UX_Respuestas_OneAIAnswerPerQuestion]
    ON [Respuestas]([PreguntaId])
    WHERE [EsIA] = 1 AND [Eliminado] = 0;
    
    PRINT '✅ Constraint creado exitosamente';
END
```

---

## 🔄 PASO 5: REINICIAR APLICACIÓN

### En Visual Studio:
```
1. Detener debugging (Shift + F5)
2. Iniciar de nuevo (F5)
```

### En Terminal:
```bash
# Detener (Ctrl + C)
# Iniciar
dotnet run
```

---

## 🧪 PASO 6: PROBAR

### 6.1 Crear pregunta de prueba

1. Ve a: `https://localhost:7002/Preguntas`
2. Click en **"Nueva Pregunta"**
3. Escribe:
   ```
   Título: ¿Qué es la Enfermedad de Crohn?
   Cuerpo: Recientemente diagnosticado, quisiera entender mejor la enfermedad. ¿Qué debo saber?
   ```
4. Click **"Publicar"**

### 6.2 Esperar 10-15 segundos

⏳ La respuesta se genera en segundo plano (no es instantánea)

### 6.3 Refrescar la página

```
F5 o Ctrl + R
```

### 6.4 Verificar que aparece el badge:

```
🤖 Respuesta Informativa (IA)
```

✅ Si ves el badge púrpura con el robot, **¡FUNCIONA!** 🎉

---

## ❌ SI NO FUNCIONA

### Ver logs en Visual Studio:

```
View → Output → Show output from: Debug
```

Buscar errores como:

```
[Error] Anthropic API key is not configured
[Error] System user not found
[Error] Invalid column name 'EsIA'
```

### Verificar en SQL:

```sql
-- Ver si la pregunta tiene respuesta IA
SELECT 
    p.Titulo,
    p.TieneRespuestaIA,
    p.FechaGeneracionIA,
    r.EsIA,
    r.ModeloIA,
    LEFT(r.Cuerpo, 100) AS Preview
FROM Preguntas p
LEFT JOIN Respuestas r ON r.PreguntaId = p.Id AND r.EsIA = 1
WHERE p.TieneRespuestaIA = 1
ORDER BY p.FechaCreacion DESC;
```

---

## ✅ CHECKLIST FINAL

Marca cada paso al completarlo:

- [ ] API key obtenida de https://console.anthropic.com/settings/keys
- [ ] Créditos agregados ($5 mínimo)
- [ ] API key pegada en `appsettings.json` (línea ~30)
- [ ] Usuario del sistema creado (SQL)
- [ ] SystemUserId copiado a `appsettings.json` (línea ~41)
- [ ] Campos AI agregados a BD (MIGRATION-AI-FIELDS.sql)
- [ ] Constraint creado (20250104_AddUniqueAIAnswerConstraint.sql)
- [ ] Aplicación reiniciada
- [ ] Pregunta de prueba creada
- [ ] Respuesta IA visible con badge 🤖

---

## 💰 COSTOS ESPERADOS

| Uso | Respuestas/Mes | Costo/Mes |
|-----|----------------|-----------|
| Testing | ~50 | $0.50 |
| Bajo | ~500 | $5.00 |
| Medio | ~2,000 | $20.00 |
| Alto | ~10,000 | $100.00 |

**Recomendación inicial:** Agrega $5 USD para testing

---

## 📞 SI NECESITAS AYUDA

1. Revisa: `AI-API-KEY-SETUP.md` (guía completa)
2. Revisa: `FRONTEND-IA-IMPLEMENTATION-SUMMARY.md` (resumen técnico)
3. Verifica logs: `Output → Debug` en Visual Studio
4. Consulta: https://docs.anthropic.com/

---

## 🎯 RESUMEN DE 1 MINUTO

```bash
# 1. Obtén API key
https://console.anthropic.com/settings/keys

# 2. Agrega $5 de créditos
https://console.anthropic.com/settings/billing

# 3. Pega API key en appsettings.json línea 30
"AnthropicApiKey": "sk-ant-api03-TU_CLAVE"

# 4. Ejecuta SQL para crear usuario sistema

# 5. Copia el ID y pégalo en appsettings.json línea 41
"SystemUserId": "ID-COPIADO"

# 6. Ejecuta migraciones SQL (campos + constraint)

# 7. Reinicia app (Shift+F5, luego F5)

# 8. Crea pregunta de prueba

# 9. Espera 10-15 seg y refresca (F5)

# 10. ¡Verifica badge 🤖!
```

---

**¡Todo lo demás ya está listo!** 🎉  
**Frontend, CSS, queries, ordenamiento, todo implementado.**  
**Solo necesitas estos 3 secretos configurados.**

---

**Última actualización:** 2025-01-04  
**Estado:** ✅ Ready to configure
