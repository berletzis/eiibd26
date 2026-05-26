# TODO_SIGUIENTE_SESION.md
**Generado:** fin de sesión 2025  
**Criterio de prioridad:** impacto en seguridad > bugs activos > features > deuda técnica  

---

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
