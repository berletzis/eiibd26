# TODO_SIGUIENTE_SESION.md
**Última actualización:** 2026 (sesión IA pipeline + Termino/Detalle)
**Criterio de prioridad:** impacto en seguridad > bugs activos > features > deuda técnica

---

## ✅ COMPLETADO esta sesión

| ID | Descripción |
|----|-------------|
| ~~A-01~~ | Datos de prueba: ya no se muestran al público (fix filtro badge) |
| ~~A-02~~ | Revisados handlers POST nullable (BUG-02 a BUG-05 resueltos) |
| ~~BUG-FIX~~ | Antiforgery duplicado `append→set` en 5 formularios |
| ~~BUG-FIX~~ | JS listeners inoperativos en `usuarioPreguntasRespuestas` |
| ~~AI-01~~ | `AIRequestLog` conectado: tabla SQL + DbSet + persistencia en `AiAnswerJob` |
| ~~AI-02~~ | Avatar de médico en "Validado por Profesionales de la Salud" |
| ~~AI-03~~ | Orden NINA: primero si <3 validaciones médicas, último si ≥3 |
| ~~AI-04~~ | Bloque "Compartir Término" en sidebar de Termino/Detalle |

---

## 🔴 ALTO

### A-01 · Eliminar datos de prueba de producción (BD)
**Descripción:** Registros `Comment = "validar descripcion usuario prueba"` / `"xxxx"` siguen en BD. Ya no se muestran (fix activo) pero contaminan las métricas.
**Acción:**
```sql
-- Verificar primero:
SELECT Id, Comment, Approved FROM GlossaryValidations
WHERE Comment IN ('validar descripcion usuario prueba', 'xxxx')
   OR (Comment LIKE '%prueba%' AND LEN(Comment) < 50);

-- Ejecutar si es correcto:
UPDATE GlossaryValidations SET Approved = 0
WHERE Comment IN ('validar descripcion usuario prueba', 'xxxx')
   OR (Comment LIKE '%prueba%' AND LEN(Comment) < 50);
```
**Archivo:** BD producción directa

### A-02 · Endpoint y UI para `RespuestaAIFeedback`
**Descripción:** El modelo y la tabla existen, el DbSet está registrado, pero no hay ningún endpoint ni página que escriba en `RespuestaAIFeedback`. El feedback es 0 registros. Para recopilar "¿fue útil esta respuesta de NINA?" se necesita un endpoint API mínimo.
**Acción sugerida:**
- Crear `/api/ai/feedback` (POST) que reciba `{ RespuestaId, EsUtil, Comentario? }` con `[Authorize]`
- Agregar botones 👍/👎 en la card de respuesta de NINA en `usuarioPreguntasRespuestas.cshtml` y en `UusuarioPreguntaDetalle.cshtml`
**Archivos:** nuevo controller o Razor handler + UI en páginas de respuestas

### A-03 · Hook `EvaluarBadgesAutomaticosAsync` en `AddValidationAsync`
**Descripción:** Cuando un médico valida un término, se debe evaluar si alcanza los criterios para badge automático. No implementado.
**Archivo:** `eiibd26/Services/Glossary/GlossaryService.cs` → `AddValidationAsync`

### A-04 · Revisar encoding de `UsuarioSintomas.cshtml.cs`
**Descripción:** El archivo puede tener caracteres `\uFFFD` en strings ("Síntoma").
**Acción:** Abrir en VS → buscar `?` o `\uFFFD` → guardar con UTF-8 BOM.
**Archivo:** `eiibd26/Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs`

---

## 🟡 MEDIO

### M-01 · Panel de admin para gestionar validaciones del glosario
**Descripción:** No hay UI para que un admin apruebe/rechace validaciones. Actualmente requiere SQL directo.
**Acción:** Crear página en `Areas/Identity/Pages/Admin/Glosario/` con tabla de validaciones + botón Aprobar/Rechazar.

### M-02 · Dashboard médico Q&A filtrado por áreas EII del médico
**Descripción:** Muestra todas las preguntas, debe filtrar por áreas EII del perfil.
**Archivo:** `eiibd26/Areas/Identity/Pages/Medico/Dashboard.cshtml.cs`

### M-03 · Admin panel para badges manuales
**Descripción:** Badges `verificado` y `creador_contenido` deben poder asignarse desde admin, no solo automáticamente.
**Archivos:** Nueva página en `Areas/Admin` + `IMedicoBadgeService`

### M-04 · Performance: `FilterCommentsByVerifiedDoctorAsync` hace N queries por término
**Descripción:** Queries a `MedicosPerfilExtendido`, `MedicosPerfilBadge`, `MedicosBadge`, y ahora también a `Perfil` (para avatar), en cada llamada. Si hay muchos términos cargados simultáneamente, puede ser costoso.
**Acción:** Evaluar con métricas reales usando `AIRequestLog`. Si hay problema, cachear el set de userIds verificados con `IMemoryCache` (TTL ~5 min).

### M-05 · Dashboard/métricas de NINA desde `AIRequestLog`
**Descripción:** La tabla `AIRequestLog` ahora se llena con cada ejecución. Crear una vista admin simple con:
- Total requests / success rate
- Tiempo promedio de procesamiento
- Modelos usados
- Errores recientes
**Archivos:** Nueva página en `Areas/Identity/Pages/Admin/` o controlador

### M-06 · Deuda DEP-003: Hangfire actualización
**Prerequisito:** Staging con copia de la BD de jobs.
**Acción cuando esté disponible:** Revisar changelog, hacer upgrade en staging, verificar jobs, promover a producción.

---

## 🟢 BAJO

### B-01 · Deuda DEP-005: QuestPDF licencia
**Acción:** Revisar términos de licencia de QuestPDF. Documentar decisión.

### B-02 · Deuda DEP-007: Twilio migration guide
**Acción:** Leer Twilio breaking changes. Auditar usos de `TwilioClient`. Planificar actualización.

### B-03 · Deuda DEP-008: WebPush alternativa
**Acción:** Evaluar `Lib.AspNetCore.WebPush` como reemplazo.

### B-04 · Consolidación CSS
**Descripción:** Varios archivos CSS con reglas duplicadas en `wwwroot/css/`.
**Acción:** Auditar y consolidar en sesión de mantenimiento dedicada.

### B-05 · Completar auditoría de módulos `04modulos.html`
**Acción:** Revisar findings OPEN en `Documentation/auditoria/04modulos.html`.

### B-06 · MeaningComments sin AvatarUrl (tipo 1 validaciones)
**Descripción:** Los comentarios de `MeaningComments` son `List<string>` sin datos de usuario. En "Validado por Profesionales" muestran siempre el avatar default. Para mostrar el avatar real se necesitaría cambiar `MeaningComments` de `List<string>` a `List<ValidationCommentDto>`.
**Decisión actual:** Dejar con avatar default — es un cambio de mayor impacto y los comentarios tipo 1 son menos frecuentes.

---

## Reglas para NO repetir errores (referencia rápida)

| Regla | Descripción |
|-------|-------------|
| **NRT-001** | Parámetros de `OnPost*Async` que vienen de formulario → siempre `string?`, `DateTime?`, `int?` |
| **JS-001** | Un error de sintaxis en un bloque de listeners silencia todo lo que sigue → revisar consola del navegador |
| **FORM-001** | `FormData(form)` ya incluye el token antiforgery → usar `set()` nunca `append()` para no duplicar |
| **DB-001** | Modelo en `Models/` sin `DbSet` en `ApplicationDbContext` = nunca se persiste. Verificar código + tabla en BD |
| **AVATAR-001** | `Perfil.Avatar` puede ser `null`, `""` o `"default.jpg"` → todos son "sin foto". Solo `/uploads/...` es real |
| **APIKEY-001** | Una API key bien formateada puede estar revocada. Siempre validar con llamada HTTP real |
| **SQL-001** | Nunca `dotnet ef database update` en producción. Cambios de esquema = SQL directo |



## 🔴 ALTO

### A-01 · Eliminar datos de prueba de producción
**Descripción:** En la tabla `GlossaryValidations` hay registros con `Comment = "validar descripcion usuario prueba"` y `Comment = "xxxx"` de cuentas de test con `Approved = true`. Ya no se muestran al público (fix aplicado), pero siguen en la BD.  
**Acción:** Ejecutar SQL directo en producción:
```sql
UPDATE GlossaryValidations 
SET Approved = 0 
WHERE Comment IN ('validar descripcion usuario prueba', 'xxxx')
   OR Comment LIKE '%prueba%' AND LEN(Comment) < 50
```
Verificar antes de ejecutar con SELECT.  
**Archivo:** BD producción directa (no código)

### A-02 · Revisar todos los handlers POST por parámetros no nullable
**Descripción:** Los bugs BUG-02 a BUG-05 mostraron un patrón sistemático. Pueden existir otros handlers con el mismo problema.  
**Acción:** Buscar en todo el proyecto:
```
grep -r "OnPost.*Async.*string [^?]" --include=*.cshtml.cs
grep -r "OnPost.*Async.*DateTime [^?]" --include=*.cshtml.cs
grep -r "OnPost.*Async.*int [^?]" --include=*.cshtml.cs
```
Aplicar `?` a todos los que reciban datos de formulario.  
**Archivos:** Todos los `*.cshtml.cs` en Areas/Identity/Pages/

### A-03 · Hook `EvaluarBadgesAutomaticosAsync` en `AddValidationAsync`
**Descripción:** Pendiente desde la sesión anterior. Cuando un médico valida un término, se debe evaluar si alcanza los criterios para badge automático.  
**Archivo:** `eiibd26/Services/Glossary/GlossaryService.cs` → `AddValidationAsync`  
**Referencia:** CLAUDE.md sesión 2026-05-22

### A-04 · Revisar encoding de `UsuarioSintomas.cshtml.cs`
**Descripción:** El archivo tiene al menos un string con carácter corrupto (`\uFFFD` en "Síntoma"). Puede haber más en el mismo archivo.  
**Acción:** Abrir en VS y buscar caracteres `?` o `\uFFFD`. Guardar con UTF-8 BOM.  
**Archivo:** `eiibd26/Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs`

---

## 🟡 MEDIO

### M-01 · Panel de admin para gestionar validaciones del glosario
**Descripción:** No hay forma de que un admin apruebe/rechace validaciones desde la UI. Actualmente se haría con SQL directo.  
**Acción:** Crear página en Areas/Admin con tabla de validaciones pendientes + botón Aprobar/Rechazar.

### M-02 · Dashboard médico Q&A filtrado por áreas EII del médico
**Descripción:** El dashboard de médico muestra todas las preguntas. Debe filtrar por las áreas EII configuradas en su perfil.  
**Archivo:** `eiibd26/Areas/Identity/Pages/Medico/Dashboard.cshtml.cs`

### M-03 · Admin panel para badges manuales
**Descripción:** Los badges `verificado` y `creador_contenido` deben poder asignarse desde un panel de admin, no solo automáticamente.  
**Archivos:** Nueva página en Areas/Admin + `IMedicoBadgeService`

### M-04 · Performance: `FilterCommentsByVerifiedDoctorAsync` hace 3 queries por término
**Descripción:** El nuevo método en `GlossaryService` ejecuta queries a `MedicosPerfilExtendido`, `MedicosPerfilBadge`, `MedicosBadge` en cada llamada. Si hay muchos términos cargados simultáneamente, puede ser costoso.  
**Acción:** Evaluar con métricas reales. Si hay problema, cachear el set de userIds verificados con `IMemoryCache` (TTL ~5 min).

### M-05 · Deuda DEP-003: Hangfire actualización
**Prerequisito:** Staging con copia de la BD de jobs.  
**Acción cuando esté disponible:** Revisar changelog de Hangfire, hacer upgrade en staging, verificar jobs, promover a producción.

---

## 🟢 BAJO

### B-01 · Deuda DEP-005: QuestPDF licencia
**Acción:** Revisar términos de licencia de QuestPDF para la versión actual y la más reciente. Documentar la decisión.

### B-02 · Deuda DEP-007: Twilio migration guide
**Acción:** Leer Twilio breaking changes. Auditar usos de `TwilioClient` en el proyecto. Planificar actualización.

### B-03 · Deuda DEP-008: WebPush alternativa
**Acción:** Evaluar `Lib.AspNetCore.WebPush` como reemplazo. Verificar que el feature de push notifications justifica el esfuerzo.

### B-04 · Consolidación CSS
**Descripción:** Pendiente desde sesión 2026-05-22. Varios archivos CSS con reglas duplicadas.  
**Acción:** Auditar `wwwroot/css/` y consolidar en sesión de mantenimiento dedicada.

### B-05 · Completar auditoría de módulos `04modulos.html`
**Descripción:** Algunos findings de la auditoría de módulos aún están en estado OPEN.  
**Archivo:** `Documentation/auditoria/04modulos.html`
