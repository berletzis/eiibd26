# 02 — Trazabilidad de Flujos CRUD
_Generado: 2025-07-14 | Rama: master_

> Para cada operación se traza: UI → JS → Handler → Servicio/Repo → DB → Respuesta → Refresh UI
> Ruptura documentada exactamente donde ocurre.

---

## MÓDULO: MIS CONDICIONES

### FLUJO: Agregar condición

```
UI: autocomplete input + botón "Agregar"
  ↓
JS: fetch POST /Identity/Usuario/UsuarioCondiciones?handler=AgregarCondicion
	body FormData: condicionId, __RequestVerificationToken
  ↓
Handler: OnPostAgregarCondicionAsync(int condicionId)
	- Valida usuario autenticado
	- Verifica que la condición exista y no tenga hijos (no permite categorías padre)
	- Verifica duplicado con !Eliminado
	- Crea condicionUsuario {idUsuario, idCondicion, fechaInicio=Now, Eliminado=false}
  ↓
DB: INSERT condicionUsuario
  ↓
Respuesta: {ok:true} o {ok:false, mensaje}
  ↓
Refresh UI: location.reload() en JS al recibir ok:true
```
**Estado: ✅ SIN RUPTURA DETECTADA**

---

### FLUJO: Editar fecha de inicio

```
UI: input date en card + botón guardar
  ↓
JS: fetch POST ?handler=EditarFechaInicio
	body FormData: condUsuarioId, nuevaFechaInicio, __RequestVerificationToken
  ↓
Handler: OnPostEditarFechaInicioAsync(int condUsuarioId, DateTime? nuevaFechaInicio)
	- Valida HasValue (retorna 400 si no)
	- Busca relación por idUsuario + id + !Eliminado
	- Actualiza fechaInicio
  ↓
DB: UPDATE condicionUsuario SET fechaInicio
  ↓
Respuesta: {ok:true}
  ↓
Refresh UI: actualiza texto en DOM sin reload
```
**Estado: ✅ SIN RUPTURA — nullable fix ya aplicado**

---

### FLUJO: Eliminar condición

```
UI: botón "Eliminar" en card
  ↓
JS: form POST (no fetch) → submit estándar ?handler=EliminarCondicion
	incluye condUsuarioId en hidden input
  ↓
Handler: OnPostEliminarCondicionAsync(int condUsuarioId)
	- Verifica tieneSintomas → 400 si bloqueo
	- Verifica tieneTratamientos → 400 si bloqueo
	- soft-delete: Eliminado=true, fechaEliminado=Now
  ↓
DB: UPDATE condicionUsuario SET Eliminado=1
  ↓
Respuesta: RedirectToPage() (recarga completa)
  ↓
Refresh UI: full page reload
```
**Estado: ✅ OK — La respuesta es redirect, el JS debe manejar eso o usa form estándar**

---

### FLUJO: Toggle principal

```
UI: botón estrella/pin en card
  ↓
JS: fetch POST ?handler=TogglePrincipalCondicion
	body FormData: condUsuarioId, __RequestVerificationToken
  ↓
Handler: OnPostTogglePrincipalCondicionAsync(int condUsuarioId)
	- Invierte EsPrincipal
	- Si true: desactiva todos los otros del mismo usuario
	- Guarda fechaModificado
  ↓
DB: UPDATE condicionUsuario SET EsPrincipal, FechaModificado
  ↓
Respuesta: {ok:true, esPrincipal:bool}
  ↓
Refresh UI: actualiza icono en DOM
```
**Estado: ✅ SIN RUPTURA**

---

## MÓDULO: MIS SÍNTOMAS

### FLUJO: Agregar síntoma

```
UI: autocomplete input + botón "Agregar"
  ↓
JS: fetch POST ?handler=AgregarSintoma
	body FormData: sintomaId, __RequestVerificationToken
  ↓
Handler: OnPostAgregarSintomaAsync(int sintomaId)
	- Verifica existencia en catálogo sintomas
	- Verifica duplicado idUsuario+idSintoma+!Eliminado
	- Crea sintomasUsuario
  ↓
DB: INSERT sintomasUsuario
  ↓
Respuesta: {ok:true} / {ok:false, mensaje}
  ↓
Refresh UI: location.reload()
```
**Estado: ✅ OK**

---

### FLUJO: Registrar tracking (modal)

```
UI: _TrackingSintomaModal — selección estado/dolor/sangrado/frecuencia
  ↓
JS (en _TrackingSintomaModal.cshtml): 
	getToken() desde input[name="__RequestVerificationToken"]
	fetch POST /Identity/Usuario/UsuarioSintomas?handler=TrackSintoma
	body FormData: sintomaUsuarioId, estado, dolor?, tieneSangrado?, frecuenciaId?
  ↓
Handler: OnPostTrackSintomaAsync(...)
	- Valida sintomaUsuarioId pertenece al usuario
	- Llama _trackingService.GuardarTrackingAsync(TrackingRequestDto)
  ↓
Servicio: ITrackingSintomaService.GuardarTrackingAsync
	- Upsert en TrackingSintomaUsuario por (IdUsuario, IdSintomaUsuario, Fecha=Today)
  ↓
DB: INSERT o UPDATE TrackingSintomaUsuario
  ↓
Respuesta: {ok:true}
  ↓
Refresh UI: cierra modal, actualiza estado visual del día
```
**Estado: ✅ OK**

---

### FLUJO: Asociar condiciones a síntoma

```
UI: modal de relaciones dentro de tarjeta síntoma
  ↓
JS: fetch POST ?handler=AsociarCondiciones
	body FormData: sintomaId, condicionUsuarioIds[] (lista de IDs checkbox)
  ↓
Handler: OnPostAsociarCondicionesAsync(int sintomaId, List<int> condicionUsuarioIds)
	- Obtiene relaciones existentes
	- Elimina las que no están en la nueva lista
	- Filtra IDs válidos (pertenecen al usuario, !Eliminado)
	- Inserta las nuevas
  ↓
DB: DELETE + INSERT SintomaCondicionUsuario
  ↓
Respuesta: {ok:true}
  ↓
Refresh UI: actualiza lista de condiciones en la tarjeta
```
**Estado: ✅ OK**

---

### FLUJO: Eliminar síntoma

```
UI: botón "Eliminar" en card
  ↓
JS: fetch POST ?handler=EliminarSintoma
	body FormData: sintId, __RequestVerificationToken
  ↓
Handler: OnPostEliminarSintomaAsync(int sintId)
	- Verifica no tiene condiciones → bloquea con mensaje
	- Verifica no tiene tratamientos → bloquea con mensaje  
	- Verifica no tiene tracking → bloquea con mensaje
	- soft-delete: Eliminado=true, fechaModificado=Now
  ↓
DB: UPDATE sintomasUsuario SET Eliminado=1
  ↓
Respuesta: {ok:true} / {ok:false, mensaje}
  ↓
Refresh UI: elimina tarjeta del DOM
```
**Estado: ✅ OK — El bloqueo por tracking puede frustrar usuarios; es by-design**

---

## MÓDULO: MIS TRATAMIENTOS

### FLUJO: Editar fecha de fin

```
UI: input date "Fecha fin" en card
  ↓
JS: fetch POST ?handler=EditarFechaFin
	body FormData: tratId, nuevaFechaFin (string vacío si se borra)
  ↓
Handler: OnPostEditarFechaFinAsync(int tratId, string? nuevaFechaFin = null)
	- Parseo manual: string.IsNullOrWhiteSpace → null, else DateTime.TryParse
	- Valida fechaFin >= fechaInicio
	- Actualiza rel.FechaFin (puede ser null para borrar fecha fin)
  ↓
DB: UPDATE tratamientoUsuario SET FechaFin
  ↓
Respuesta: {ok:true} / {ok:false, mensaje}
```
**Estado: ✅ OK — Acepta string vacío correctamente para borrar fecha fin**

---

## MÓDULO: SEGUIMIENTO SÍNTOMAS

### FLUJO: Track desde matriz

```
UI: celda de la matriz (día × síntoma) → click/dropdown estado
  ↓
JS (UsuarioSintomasSeguimiento.cshtml):
	token desde #formTrackingMatriz input[__RequestVerificationToken]
	fetch POST ?handler=TrackSintomaMatriz
	body FormData: sintomaUsuarioId, estado, fecha (string "yyyy-MM-dd"), dolor?, tieneSangrado?, frecuenciaId?
  ↓
Handler: OnPostTrackSintomaMatrizAsync(int sintomaUsuarioId, string estado, string fecha, ...)
	- DateTime.TryParse(fecha, out fechaParseada) — acepta string para evitar errores de formato
	- Llama _trackingService.GuardarTrackingAsync(TrackingRequestDto)
  ↓
Servicio: ITrackingSintomaService.GuardarTrackingAsync
	- Upsert TrackingSintomaUsuario
  ↓
DB: UPSERT TrackingSintomaUsuario
  ↓
Respuesta: {ok:true}
  ↓
Refresh UI: actualiza celda de la matriz
```
**Estado: ✅ OK — Fecha como string evita problemas de binding**

---

## MÓDULO: LABORATORIOS

### FLUJO: Agregar resultado

```
UI: autocomplete tipo laboratorio + botón "Agregar"
  ↓
JS: fetch POST ?handler=AgregarResultado
	body FormData: laboratoryTypeId
  ↓
Handler: OnPostAgregarResultadoAsync(int laboratoryTypeId)
	- Verifica tipo no tiene hijos (solo hojas)
	- Verifica tipo activo
	- Verifica que no existe resultado activo para ese tipo
	- Crea PatientLaboratoryResult vacío (datos se completan después)
  ↓
DB: INSERT PatientLaboratoryResults
  ↓
Respuesta: {ok:true}
  ↓
Refresh UI: recarga o muestra nuevo card
```
**Estado: ✅ OK**

---

### FLUJO: Actualizar resultado (edición en card)

```
UI: inputs en card laboratorio
  ↓
JS: fetch POST ?handler=ActualizarResultado
	body FormData: resultadoId, resultValue?, notes?, resultDate?,
				   condicionUsuarioId?, sintomaUsuarioId?, tratamientoUsuarioId?,
				   laboratoryUnitCatalogId?
  ↓
Handler: OnPostActualizarResultadoAsync(int resultadoId, string? resultValue, ...)
	- Todos parámetros nullable (fix aplicado)
	- Valida ownership (PatientId == userId)
	- Valida ownership de condición/síntoma/tratamiento si se envían
	- Actualiza campos
	- Si laboratoryUnitCatalogId → overwrites ResultUnit con abreviatura del catálogo
  ↓
DB: UPDATE PatientLaboratoryResults
  ↓
Respuesta: {ok:true}
```
**Estado: ✅ OK**

---

### FLUJO: Eliminar resultado ⚠️

```
UI: botón "Eliminar" en card
  ↓
JS: fetch POST o form submit → ?handler=EliminarResultado
	body: resultadoId
  ↓
Handler: OnPostEliminarResultadoAsync(int resultadoId)
	- soft-delete
	- RETORNA: RedirectToPage() — full page redirect
  ↓
DB: UPDATE PatientLaboratoryResults SET Eliminado=1
  ↓
Respuesta: HTTP 302 Redirect (NO json)
  ↓
Refresh UI: ¿JS espera JSON o maneja redirect?
```
**Estado: ✅ OK — VERIFICADO: El JS crea un `<form>` dinámico y llama `form.submit()`, no fetch. El redirect 302 es correcto.**

---

## MÓDULO: DIRECTORIO MÉDICOS

### FLUJO: Proponer médico

```
UI: /DirectorioMedicos/Proponer — formulario estándar
  ↓
Form submit: POST /DirectorioMedicos/Proponer (antiforgery automático con asp-page)
  ↓
Handler: OnPostAsync()
	- ModelState.IsValid → si no, redisplay con errores
	- _service.ProponerMedicoAsync(Input, usuarioId)
	- catch InvalidOperationException → ModelState error
  ↓
Servicio: IMedicoDirectorioService.ProponerMedicoAsync
	- Persiste MedicoDirectorio con Estado="pendiente"
  ↓
DB: INSERT MedicosDirectorio
  ↓
Respuesta: TempData["Success"] + RedirectToPage("Index")
  ↓
Refresh UI: página Index con mensaje de éxito
```
**Estado: ✅ OK**

---

## MÓDULO: DIRECTORIO MÉDICOS (admin)

### FLUJO: Editar médico

```
UI: panel admin → modal edición
  ↓
JS: fetch POST ?handler=Editar (FormData con id, nombreCompleto, campos opcionales)
  ↓
Handler: OnPostEditarAsync(int id, string nombreCompleto, ...)
    - Valida nombreCompleto no vacío
    - Busca MedicoDirectorio por id
    - Detecta cambio en cedulaVerificada → actualiza EstatusValidacion + FechaCedulaVerificada
    - Guarda FechaModificacion = UtcNow
    - Si cambio en verificación → RecalcularNivelAsync
  ↓
DB: UPDATE MedicosDirectorio
  ↓
Respuesta: {ok:true}
```
**Estado: ✅ SIN RUPTURA**

---

### FLUJO: Aprobar/Rechazar claim

```
UI: botones en panel admin
  ↓
JS: fetch POST ?handler=AprobarClaim / ?handler=RechazarClaim (FormData con id)
  ↓
Handler: OnPostAprobarClaimAsync / OnPostRechazarClaimAsync
    - Lee id desde Request.Form["id"] (no parámetro binding — consistente con todos los handlers admin)
    - Aprobar: EstatusReclamacion=Reclamado, NivelConfianza=Establecido
    - Rechazar: EstatusReclamacion=Rechazado, limpia EmailSolicitudClaim
  ↓
DB: UPDATE MedicosDirectorio
  ↓
Respuesta: {success:true}
```
**Estado: ✅ OK**

---

## MÓDULO: PREGUNTAS Y RESPUESTAS

### FLUJO: Crear pregunta (modal rápido)

```
UI: modal "Nueva Pregunta" en usuarioPreguntasRespuestas
  ↓
JS: fetch POST ?handler=CrearPregunta (FormData: titulo, cuerpo)
  ↓
Handler: OnPostCrearPreguntaAsync()
    - Lee titulo/cuerpo de Request.Form directamente
    - Genera slug único con SlugHelper.GenerateUniqueSlugForPregunta
    - Crea Pregunta con Id=Guid.NewGuid()
    - SaveChangesAsync → Encola AiAnswerJob en Hangfire
  ↓
DB: INSERT Preguntas
  ↓
Respuesta: {ok:true, id, slug}
```
**Estado: ✅ OK**

---

### FLUJO: Crear/Editar pregunta (formulario completo)

```
UI: UusuarioPreguntaDetalle — formulario Título + Cuerpo + relaciones
  ↓
Form submit PRG: POST ?handler=Save
  ↓
Handler: OnPostSaveAsync()
    - Si Id.HasValue → EDIT: bloquea si tiene respuestas activas
    - Si !Id.HasValue → CREATE: genera slug único, encola Hangfire job IA
    - ReplaceRelationsAsync → DELETE/INSERT PreguntaCondiciones/Sintomas/Tratamientos
    - TempData["SuccessMessage"] + RedirectToPage(new { id })
  ↓
DB: INSERT o UPDATE Preguntas + relaciones
  ↓
Respuesta: HTTP 302 → GET con id (PRG previene re-POST)
```
**Estado: ✅ OK**

---

### FLUJO: Votar pregunta / respuesta

```
UI: botones ▲ ▼ en tarjetas
  ↓
JS: fetch POST /api/preguntas/{id}/votar o /api/respuestas/{id}/votar
    body JSON: {valor: 1 o -1}
    headers: RequestVerificationToken (antiforgery)
  ↓
Controller: [ValidateAntiForgeryToken] VotarPregunta / VotarRespuesta
    - No se puede votar la propia pregunta/respuesta
    - Si no existe voto: INSERT
    - Si existe mismo valor: toggle Eliminado (cancela voto)
    - Si existe valor diferente: Eliminado=true al anterior + INSERT nuevo
    - Race condition: captura DbUpdateException por unique constraint
  ↓
DB: INSERT / UPDATE Votos
  ↓
Respuesta: {score, userVote}
```
**Estado: ✅ OK — manejo robusto de todos los casos**

---

### FLUJO: Crear respuesta

```
UI: formulario respuesta en detalle pregunta
  ↓
JS: fetch POST /api/respuestas (body JSON: {cuerpo, preguntaId})
  ↓
Controller: CrearRespuesta
    - Lee preguntaId desde header X-Pregunta-Id, query o body (flexible)
    - Valida cuerpo (min 10, max 10000 chars)
    - Crea Respuesta + notificación push async al autor
  ↓
DB: INSERT Respuestas
  ↓
Respuesta: {id}
```
**Estado: ✅ OK**

---

---

## MÓDULO: ESTADO DE ÁNIMO

### FLUJO: Registrar estado de ánimo

```
UI: _EstadoAnimoModal (global, en layout)
  ↓
JS: fetch POST /api/EstadoAnimoUsuario/nuevo (FormData)
    campos: mood (1-5 o nombre), texto?, condicionUsuarioId?, sintomaUsuarioId?, tratamientoUsuarioId?, fechaRegistro?
  ↓
Controller: [Authorize] Nuevo([FromForm] ...)
    - Valida mood numérico (1-5) o Enum.TryParse
    - Valida texto max 2000 chars
    - ClinicalOwnershipValidator.ValidateEstadoAnimoRelationsAsync  ← SEC-010
      · Verifica que condicionUsuarioId/sintomaUsuarioId/tratamientoUsuarioId pertenezcan al usuario
    - Acepta fechaRegistro solo si está dentro de las últimas 24h; si no, usa UtcNow
    - INSERT EstadoAnimoUsuario
  ↓
DB: INSERT EstadoAnimoUsuario
  ↓
Respuesta: {Id, EstadoMood, Texto, FechaRegistro, Condicion?, Sintoma?, Tratamiento?}
```
**Estado: ✅ SIN RUPTURA — validación de ownership excelente (SEC-010)**

---

### FLUJO: Eliminar estado de ánimo

```
UI: botón eliminar en historial
  ↓
JS: fetch POST /api/EstadoAnimoUsuario/eliminar/{id}
  ↓
Controller: Eliminar(int id)
    - Busca registro por id AND idUsuario (no puede borrar lo ajeno)
    - Soft-delete: Eliminado = true
  ↓
DB: UPDATE EstadoAnimoUsuario SET Eliminado=1
  ↓
Respuesta: {ok:true}
```
**Estado: ✅ OK**

---

## MÓDULO: RECLAMAR PERFIL MÉDICO

### FLUJO: Enviar solicitud de reclamo

```
UI: Pages/DirectorioMedicos/ReclamarPerfil — formulario con cédula + confirmación
  ↓
GET OnGetAsync(int id)
    - Verifica que el médico exista y no esté ya reclamado ni tenga solicitud pendiente
    - Pre-llena EmailContacto con email del usuario autenticado
  ↓
FORM submit POST OnPostAsync(int id)
    - Revalida estado del médico
    - Valida Confirmo == true y Cédula no vacía
    - Usa User.FindFirstValue(Email) — NO el campo EmailContacto del form ← correcto (SEC)
    - EstatusReclamacion = EnProceso, guarda cédula y email
  ↓
DB: UPDATE MedicosDirectorio
  ↓
Respuesta: PRG → RedirectToPage(Detalle) con TempData["Success"]
```
**Estado: ✅ SIN RUPTURA — el email del reclamo viene de Claims, no del formulario (protección tampering)**

---

---

## MÓDULO: AUTOCOMPLETE APIs

### FLUJO: Búsqueda en catálogos (condiciones / síntomas / tratamientos)

```
UI: campo de texto "Agregar condición/síntoma/tratamiento"
  ↓
JS: fetch GET /api/condiciones|sintomas|tratamientos/autocomplete?q=texto[&excludeIds=1,2]
    — sin auth (AllowAnonymous), sin CSRF (GET de solo lectura)
    — Rate limiter "catalogos-autocomplete" aplicado
  ↓
Controller: Autocomplete([FromQuery] string q)
    - Si q vacío: devuelve []
    - Condiciones: carga toda la tabla en memoria, resolve padreNombre en O(1) con Dictionary,
      agrupa padres antes que hijos, sin duplicados
    - Síntomas: WHERE nombre CONTAINS q, excluye excludeIds CSV si se provee
    - Tratamientos: WHERE nombre CONTAINS q
  ↓
DB: SELECT catálogo (ninguna tabla de pacientes expuesta)
  ↓
Respuesta: [{id, nombre, icono, ...}]
```
**Estado: ✅ SIN RUPTURA — datos genéricos, no de paciente; rate limit activo**

---

## MÓDULOS: RATINGS, FEEDBACK, MOOD PUSH, BÚSQUEDA

### FLUJO: Rating artículo / glosario (like/dislike)

```
UI: botones 👍👎 en páginas de artículo o glosario
  ↓
JS: fetch POST /api/articles/{id}/rating o /api/glossary/{id}/rating
    body JSON: {ratingType: "like" | "dislike"}
  ↓
Controller: RateArticle / RateTerm
    - Sin [ValidateAntiForgeryToken] (anónimo permitido)
    - Autenticado → upsert por userId
    - Anónimo    → upsert por IP dentro de ±24h
    - Devuelve estadísticas actualizadas
  ↓
DB: INSERT / UPDATE ArticleRatings | GlossaryTermRatings
  ↓
Respuesta: {ok, estadisticas: {likes, dislikes, total}}
```
**Estado: ✅ OK — upsert con deduplicación correcta. No expone datos de paciente.**

---

### FLUJO: Feedback respuesta IA (útil / no útil)

```
UI: botones 👍👎 en respuestas IA de Q&A
  ↓
JS: fetch POST /api/respuestas/{id}/feedback
    body JSON: {esUtil: true|false, comentario?}
    [Authorize] → requiere sesión
  ↓
Controller: DarFeedback
    - Solo respuestas con EsIA=true
    - Upsert por userId: actualiza si ya existe
    - DELETE /api/respuestas/{id}/feedback → hard delete del propio feedback
  ↓
DB: INSERT / UPDATE / DELETE RespuestaAIFeedbacks
  ↓
Respuesta: {ok, estadisticas: {total, likes, dislikes, porcentajeLikes}}
```
**Estado: ✅ OK**

---

### FLUJO: Mood rápido desde notificación push

```
UI: botón de acción en notificación push del navegador
  ↓
JS/Service Worker: GET /api/mood/quick?token=TOKEN&valor=3
    [IgnoreAntiforgeryToken] — sin cookie de sesión
  ↓
Controller: QuickMood
    - Valida token de un solo uso (5 min) via IPushMoodTokenService
    - Valida valor ∈ [1..5]
    - Idempotencia: si ya registró mood en últimos 5 min → {ok:true, duplicado:true}
    - INSERT EstadoAnimoUsuario
  ↓
DB: INSERT EstadoAnimoUsuario
  ↓
Respuesta: {ok:true, id}
```
**Estado: ✅ OK — [IgnoreAntiforgeryToken] intencional y justificado (push no tiene cookie)**

---

### FLUJO: Búsqueda de sugerencias

```
UI: barra de búsqueda global
  ↓
JS: fetch GET /api/search/suggestions?q=texto[&condicionId=1]
    [AllowAnonymous]
  ↓
Controller: GetSuggestions
    - q < 20 chars → devuelve vacío con mensaje
    - SearchSuggestionService.GetSuggestionsAsync → Preguntas + Contenidos + Respuestas
    - URLs canónicas via slug, fallback a ?id=
  ↓
DB: SELECT (sin datos de paciente)
  ↓
Respuesta: {preguntas[], articulos[], respuestas[]}
```
**Estado: ✅ OK — mínimo 20 chars previene búsquedas de 1 letra; sin datos privados**

---

## MÓDULO: MI SALUD

**Resultado de búsqueda:** No existe página `MiSalud.cshtml` en el proyecto.
El archivo `wwwroot/css/miSalud.css` existe (estilos para el panel de usuario),
pero el módulo no tiene página Razor Page propia: los flujos están distribuidos en
`UsuarioCondiciones`, `UsuarioSintomas`, `UsuarioTratamientos`, `UsuarioSintomasSeguimiento`,
`UsuarioLaboratorios` y `UsuarioEstadoAnimo` — todos ya auditados.

---

## RESUMEN DE RUPTURAS DETECTADAS

**0 rupturas activas detectadas en los 59 handlers auditados.**

| # | Observación | Tipo | Módulo | Acción |
|---|---|---|---|---|
| O-01 | Handlers admin leen `id` de `Request.Form["id"]` en vez de parámetro de método | Inconsistencia de estilo (no error) | Admin Médicos | Ninguna |
| O-02 | `[ValidateAntiForgeryToken]` en votar — JS envía token en header | **✅ VERIFICADO** — `Preguntas.cshtml` l.742 y `Detalles.cshtml` l.1475 envían `RequestVerificationToken` correctamente | Preguntas/Respuestas | Sin acción |
| O-03 | Encoding corrupto en comentarios de código | Cosmético | Varios | Limpieza futura |
| O-04 | **`DateTime?` binding falla con `type="date"` HTML** — input envía `yyyy-MM-dd`, model binder usa cultura servidor (`es-MX`) → 400 | **✅ CORREGIDO** — `OnPostEditarFechaInicioAsync` en Condiciones, Síntomas y Tratamientos ahora recibe `string?` + `TryParseExact("yyyy-MM-dd", InvariantCulture)` | Condiciones / Síntomas / Tratamientos | Ninguna |
