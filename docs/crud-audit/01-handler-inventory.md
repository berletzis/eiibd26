# 01 — Inventario de Handlers CRUD
_Generado: 2025-07-14 | Rama: master_

> Cada fila representa un handler verificado en código fuente.
> Estado: ✅ OK aparente | ⚠️ Riesgo detectado | ❌ Error confirmado | 🔍 Pendiente revisión profunda

---

## MIS CONDICIONES
**Page model:** `Areas/Identity/Pages/Usuario/UsuarioCondiciones.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Parámetros clave | Estado |
|---|---|---|---|---|---|
| LIST | `OnGetAsync` | GET page | `condicionUsuario` + joins | — | ✅ OK |
| CREATE | `OnPostAgregarCondicionAsync` | `fetch ?handler=AgregarCondicion` | `condicionUsuario` | `condicionId` (int) | ✅ OK |
| EDIT fecha | `OnPostEditarFechaInicioAsync` | `fetch ?handler=EditarFechaInicio` | `condicionUsuario` | `condUsuarioId`, `nuevaFechaInicio` (DateTime?) | ✅ OK (nullable fix aplicado) |
| DELETE | `OnPostEliminarCondicionAsync` | `fetch ?handler=EliminarCondicion` | `condicionUsuario` soft-delete | `condUsuarioId` | ✅ OK — bloquea si hay síntomas/tratamientos |
| TOGGLE PRINCIPAL | `OnPostTogglePrincipalCondicionAsync` | `fetch ?handler=TogglePrincipalCondicion` | `condicionUsuario.EsPrincipal` | `condUsuarioId` | ✅ OK |

**Autocomplete:** `GET /api/Condiciones/autocomplete?q=` — endpoint API separado (no auditado aún)

---

## MIS SÍNTOMAS
**Page model:** `Areas/Identity/Pages/Usuario/UsuarioSintomas.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Parámetros clave | Estado |
|---|---|---|---|---|---|
| LIST | `OnGetAsync` | GET page | `sintomasUsuario` + tracking + relaciones | — | ✅ OK |
| CREATE | `OnPostAgregarSintomaAsync` | `fetch ?handler=AgregarSintoma` | `sintomasUsuario` | `sintomaId` (int) | ✅ OK |
| TRACK | `OnPostTrackSintomaAsync` | `_TrackingSintomaModal` → `?handler=TrackSintoma` | `TrackingSintomaUsuario` | `sintomaUsuarioId`, `estado`, `dolor?`, `tieneSangrado?`, `frecuenciaId?` | ✅ OK |
| EDIT fecha | `OnPostEditarFechaInicioAsync` | `fetch ?handler=EditarFechaInicio` | `sintomasUsuario` | `sintId`, `nuevaFechaInicio` (DateTime?) | ✅ OK (nullable fix aplicado) |
| DELETE | `OnPostEliminarSintomaAsync` | `fetch ?handler=EliminarSintoma` | `sintomasUsuario` soft-delete | `sintId` | ✅ OK — bloquea si tiene condiciones/tratamientos/tracking |
| TOGGLE PRINCIPAL | `OnPostTogglePrincipalSintomaAsync` | `fetch ?handler=TogglePrincipalSintoma` | `sintomasUsuario.EsPrincipal` | `sintId` | ✅ OK |
| ASOCIAR CONDICIONES | `OnPostAsociarCondicionesAsync` | `fetch ?handler=AsociarCondiciones` | `SintomaCondicionUsuario` | `sintomaId`, `condicionUsuarioIds[]` | ✅ OK |
| QUITAR REL CONDICION | `OnPostQuitarRelacionCondicionAsync` | `fetch ?handler=QuitarRelacionCondicion` | `SintomaCondicionUsuario` | `sintomaId`, `condicionUsuarioId` | ✅ OK |
| ASOCIAR TRATAMIENTOS | `OnPostAsociarTratamientosAsync` | `fetch ?handler=AsociarTratamientos` | `TratamientoSintomaUsuario` | `sintomaId`, `tratamientoUsuarioIds[]` | ✅ OK |
| QUITAR REL TRATAMIENTO | `OnPostQuitarRelacionTratamientoAsync` | `fetch ?handler=QuitarRelacionTratamiento` | `TratamientoSintomaUsuario` | `sintomaId`, `tratamientoUsuarioId` | ✅ OK |

---

## MIS TRATAMIENTOS
**Page model:** `Areas/Identity/Pages/Usuario/UsuarioTratamientos.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Parámetros clave | Estado |
|---|---|---|---|---|---|
| LIST | `OnGetAsync` | GET page | `tratamientoUsuario` + relaciones | — | ✅ OK |
| CREATE | `OnPostAgregarTratamientoAsync` | `fetch ?handler=AgregarTratamiento` | `tratamientoUsuario` | `tratamientoId` (int) | ✅ OK |
| EDIT fecha inicio | `OnPostEditarFechaInicioAsync` | `fetch ?handler=EditarFechaInicio` | `tratamientoUsuario` | `tratId`, `nuevaFechaInicio` (DateTime?) | ✅ OK (nullable fix aplicado) |
| EDIT fecha fin | `OnPostEditarFechaFinAsync` | `fetch ?handler=EditarFechaFin` | `tratamientoUsuario.FechaFin` | `tratId`, `nuevaFechaFin` (string?) | ✅ OK — parseo manual en handler |
| DELETE | `OnPostEliminarTratamientoAsync` | `fetch ?handler=EliminarTratamiento` | `tratamientoUsuario` soft-delete | `tratId` | ✅ OK — bloquea si tiene síntomas/condiciones activos |
| TOGGLE PRINCIPAL | `OnPostTogglePrincipalTratamientoAsync` | `fetch ?handler=TogglePrincipalTratamiento` | `tratamientoUsuario.EsPrincipal` | `tratId` | ✅ OK |
| ASOCIAR SÍNTOMAS | `OnPostAsociarSintomasAsync` | `fetch ?handler=AsociarSintomas` | `TratamientoSintomaUsuario` | `tratamientoId`, `sintomaUsuarioIds[]` | ✅ OK |
| QUITAR REL SÍNTOMA | `OnPostQuitarRelacionSintomaAsync` | `fetch ?handler=QuitarRelacionSintoma` | `TratamientoSintomaUsuario` | `tratamientoId`, `sintomaUsuarioId` | ✅ OK |
| ASOCIAR CONDICIONES | `OnPostAsociarCondicionesAsync` | `fetch ?handler=AsociarCondiciones` | `TratamientoCondicionUsuario` | `tratamientoId`, `condicionUsuarioIds[]` | ✅ OK |
| QUITAR REL CONDICION | `OnPostQuitarRelacionCondicionAsync` | `fetch ?handler=QuitarRelacionCondicion` | `TratamientoCondicionUsuario` | `tratamientoId`, `condicionUsuarioId` | ✅ OK |

---

## SEGUIMIENTO SÍNTOMAS
**Page model:** `Areas/Identity/Pages/Usuario/UsuarioSintomasSeguimiento.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Parámetros clave | Estado |
|---|---|---|---|---|---|
| LIST / MATRIX | `OnGetAsync` | GET page | `TrackingSintomaUsuario` (últimos 7 días) | — | ✅ OK |
| TRACK (matriz) | `OnPostTrackSintomaMatrizAsync` | `fetch ?handler=TrackSintomaMatriz` | `TrackingSintomaUsuario` | `sintomaUsuarioId`, `estado`, `fecha` (string), `dolor?`, `tieneSangrado?`, `frecuenciaId?` | ✅ OK — `fecha` se recibe como string para evitar problemas de formato |

**Nota:** El tracking desde `UsuarioSintomas` usa `OnPostTrackSintomaAsync` (sin "Matriz"), que envía `DateTime.Today` directamente desde el servidor.

---

## LABORATORIOS
**Page model:** `Areas/Identity/Pages/Usuario/UsuarioLaboratorios.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Parámetros clave | Estado |
|---|---|---|---|---|---|
| LIST | `OnGetAsync` | GET page | `PatientLaboratoryResults` + includes | — | ✅ OK |
| CREATE | `OnPostAgregarResultadoAsync` | `fetch ?handler=AgregarResultado` | `PatientLaboratoryResult` | `laboratoryTypeId` (int) | ✅ OK — verifica solo tipos hoja (sin hijos) |
| EDIT | `OnPostActualizarResultadoAsync` | `fetch ?handler=ActualizarResultado` | `PatientLaboratoryResult` | `resultadoId`, `resultValue?`, `resultUnit?`, `notes?`, `resultDate?`, `condicionUsuarioId?`, `sintomaUsuarioId?`, `tratamientoUsuarioId?`, `laboratoryUnitCatalogId?` | ✅ OK (nullable fix aplicado) |
| DELETE | `OnPostEliminarResultadoAsync` | form submit dinámico (JS crea `<form>` + `form.submit()`) | `PatientLaboratoryResult` soft-delete | `resultadoId` | ✅ OK — form.submit(), redirect 302 correcto |

---

## DIRECTORIO MÉDICOS (público)
**Page model:** `Pages/DirectorioMedicos/Index.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Estado |
|---|---|---|---|---|
| SEARCH / LIST | `OnGetAsync` | GET SupportsGet | `MedicosDirectorio` via `IMedicoDirectorioService` | ✅ OK |
| MIS PROPUESTAS | `OnGetAsync` (bloque autenticado) | GET page | `MedicosDirectorio` donde `PropuestoPorUsuarioId == userId && !Activo` | ✅ OK |

**Page model propuesta:** `Pages/DirectorioMedicos/Proponer.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Estado |
|---|---|---|---|---|
| FORM GET | `OnGetAsync` | GET page | — | ✅ OK |
| CREATE propuesta | `OnPostAsync` | form submit estándar | `MedicoDirectorio` via `_service.ProponerMedicoAsync` | ✅ OK — `ModelState.IsValid` + `InvalidOperationException` catch |

---

## DIRECTORIO MÉDICOS (admin)
**Page model:** `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Estado |
|---|---|---|---|---|
| GRID DATA | `OnGetGridDataAsync` | DataTable AJAX GET | `MedicosDirectorio` | ✅ OK |
| CARGAR MÉDICO | `OnGetMedicoAsync` | AJAX GET | `MedicosDirectorio` + confirmaciones + badges | ✅ OK |
| CATÁLOGO PAÍSES | `OnGetPaisesAsync` | AJAX GET | `Paises` | ✅ OK |
| EDITAR | `OnPostEditarAsync` | AJAX POST FormData | `MedicosDirectorio` | ✅ OK — recalcula nivel si cambia verificación |
| VERIFICAR (toggle) | `OnPostVerificarAsync` | AJAX POST FormData | `MedicosDirectorio.EstatusValidacion` | ✅ OK — lee `Request.Form["id"]` |
| ELIMINAR | `OnPostEliminarAsync` | AJAX POST FormData | `MedicosDirectorio.Eliminado` soft-delete | ✅ OK |
| RESTAURAR | `OnPostRestaurarAsync` | AJAX POST FormData | `MedicosDirectorio.Eliminado = false` | ✅ OK |
| VERIFICAR CÉDULA | `OnPostVerificarCedulaAsync` | AJAX POST FormData | `EstatusValidacion = Validado` + recalcular nivel | ✅ OK |
| APROBAR CLAIM | `OnPostAprobarClaimAsync` | AJAX POST FormData | `EstatusReclamacion = Reclamado` + nivel Establecido | ✅ OK |
| RECHAZAR CLAIM | `OnPostRechazarClaimAsync` | AJAX POST FormData | `EstatusReclamacion = Rechazado` + limpia email | ✅ OK |
| OTORGAR BADGE | `OnPostOtorgarBadgeAsync` | AJAX POST | `MedicosPerfilBadge` via `IMedicoBadgeService` | ✅ OK |
| REVOCAR BADGE | `OnPostRevocarBadgeAsync` | AJAX POST | `MedicosPerfilBadge` hard delete | ✅ OK |
| EVALUAR BADGES (uno) | `OnPostEvaluarBadgesAsync` | AJAX POST | Badges automáticos vía servicio | ✅ OK |
| EVALUAR BADGES (todos) | `OnPostEvaluarTodosAsync` | AJAX POST | Loop todos los médicos activos | ✅ OK |

---

## PREGUNTAS Y RESPUESTAS

### Page model (usuario): `Areas/Identity/Pages/Usuario/usuarioPreguntasRespuestas.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Estado |
|---|---|---|---|---|
| LIST | `OnGetAsync` | GET page | `Preguntas` + votos + respuestas (paginado) | ✅ OK |
| CREATE (rápido) | `OnPostCrearPreguntaAsync` | `fetch ?handler=CrearPregunta` | `Pregunta` + slug único + Hangfire job IA | ✅ OK |

### Page model (detalle/edición): `Areas/Identity/Pages/Usuario/UusuarioPreguntaDetalle.cshtml.cs`

| Operación | Handler | Método JS | Entidad | Estado |
|---|---|---|---|---|
| GET/EDIT form | `OnGetAsync(Guid? id)` | GET page | `Preguntas` + relaciones | ✅ OK |
| CREATE / EDIT | `OnPostSaveAsync` | form submit (PRG) | `Preguntas` + `PreguntaCondiciones/Sintomas/Tratamientos` + slug único | ✅ OK — PRG, bloquea edición si tiene respuestas |
| DELETE | `OnPostDeleteAsync(Guid? id)` | form submit | `Preguntas` soft-delete + limpia relaciones | ✅ OK — bloquea si tiene respuestas |

### API Controllers

| Operación | Endpoint | Entidad | Estado |
|---|---|---|---|
| CREATE pregunta (API) | `POST /api/preguntas` | `Pregunta` + slug único + Hangfire job IA | ✅ OK |
| EDIT pregunta (API) | `PUT /api/preguntas/{id}` | `Pregunta` | ✅ OK — NO regenera slug (URLs estables) |
| DELETE pregunta (API) | `POST /api/preguntas/{id}/eliminar` | `Pregunta` soft-delete | ✅ OK |
| VOTAR pregunta | `POST /api/preguntas/{id}/votar` + `[ValidateAntiForgeryToken]` | `Voto` (toggle/cambio dirección) | ✅ OK — maneja toggle y cambio de dirección (+1→-1) |
| CREATE respuesta | `POST /api/respuestas` | `Respuesta` + push notification async | ✅ OK |
| DELETE respuesta | `POST /api/respuestas/{id}/eliminar` | `Respuesta` soft-delete | ✅ OK |
| VOTAR respuesta | `POST /api/respuestas/{id}/votar` + `[ValidateAntiForgeryToken]` | `Voto` (toggle/cambio dirección) | ✅ OK — mismo patrón que votar pregunta |

---

## MODALES DETECTADOS

| Modal | Archivo | Trigger | Operaciones CRUD internas | Estado |
|---|---|---|---|---|
| `_TrackingSintomaModal` | `Areas/Identity/Pages/Usuario/_TrackingSintomaModal.cshtml` | `data-bs-toggle="modal"` o JS directo | Track síntoma → `OnPostTrackSintomaAsync` | ✅ Token antiforgery presente |
| `_EstadoAnimoModal` | `Views/Shared/_EstadoAnimoModal.cshtml` | layout/global | Carga condiciones/síntomas/tratamientos vía `/api/EstadoAnimoUsuario/*`, POST nuevo estado | ✅ Usa `fetch` con token |
| Modal agregar síntoma (inline) | `UsuarioSintomas.cshtml` | botón "Agregar síntoma" | `OnPostAgregarSintomaAsync` via fetch | ✅ Token presente |
| Modal asociar condición (inline) | `UsuarioSintomas.cshtml` | tarjeta síntoma | `OnPostAsociarCondiciones` via fetch | ✅ OK |
| Modal asociar tratamiento (inline) | `UsuarioSintomas.cshtml` | tarjeta síntoma | `OnPostAsociarTratamientos` via fetch | ✅ OK |
| Modal agregar tratamiento (inline) | `UsuarioTratamientos.cshtml` | botón "Agregar tratamiento" | `OnPostAgregarTratamientoAsync` via fetch | ✅ Token presente |
| Modal editar fecha inicio/fin | `UsuarioTratamientos.cshtml` | inline card | `EditarFechaInicio` / `EditarFechaFin` via fetch | ✅ OK |
| Modales admin médicos | `Admin/DirectorioMedicos/Index.cshtml` | botones DataTable row | EDIT, APROBAR, RECHAZAR | ✅ Auditado |

---

## RESUMEN DE ESTADO

---

## ESTADO DE ÁNIMO
**Controller:** `Controllers/EstadoAnimoUsuarioController.cs` (API)

| Operación | Endpoint | Método | Entidad | Parámetros clave | Estado |
|---|---|---|---|---|---|
| LIST historico | `GET /api/EstadoAnimoUsuario/historico` | GET | `EstadoAnimoUsuario` + joins | — (últimos 200) | ✅ OK |
| LIST condiciones | `GET /api/EstadoAnimoUsuario/condiciones-usuario` | GET | `condicionUsuario` | — | ✅ OK |
| LIST síntomas | `GET /api/EstadoAnimoUsuario/sintomas-usuario` | GET | `sintomasUsuario` | — | ✅ OK |
| LIST tratamientos | `GET /api/EstadoAnimoUsuario/tratamientos-usuario` | GET | `tratamientoUsuario` | — | ✅ OK |
| CREATE | `POST /api/EstadoAnimoUsuario/nuevo` | POST Form | `EstadoAnimoUsuario` | `mood`, `texto?`, `condicionUsuarioId?`, `sintomaUsuarioId?`, `tratamientoUsuarioId?`, `fechaRegistro?` | ✅ OK — validación de ownership + fecha ±24h |
| DELETE | `POST /api/EstadoAnimoUsuario/eliminar/{id}` | POST | `EstadoAnimoUsuario` | `id` (int) | ✅ OK — soft-delete |
| ESTADÍSTICAS | `GET /api/EstadoAnimoUsuario/estadisticas?meses=` | GET | `EstadoAnimoUsuario` | `meses` (1-24) | ✅ OK |

---

## RECLAMAR PERFIL MÉDICO
**Page model:** `Pages/DirectorioMedicos/ReclamarPerfil.cshtml.cs`

| Operación | Handler | Método | Entidad | Parámetros clave | Estado |
|---|---|---|---|---|---|
| VIEW | `OnGetAsync(int id)` | GET | `MedicosDirectorio` | `id` | ✅ OK — pre-bloquea si ya reclamado o pendiente |
| SUBMIT solicitud | `OnPostAsync(int id)` | POST (form) | `MedicosDirectorio` | `EmailContacto`, `CedulaProfesionalDeclarada`, `Confirmo` | ✅ OK — usa email de claims, no del form |

---

## AUTOCOMPLETE APIs (catálogos públicos)
**Controllers:** `CondicionesApiController`, `SintomasApiController`, `TratamientosApiController`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| Condiciones autocomplete | `GET /api/condiciones/autocomplete?q=` | `[AllowAnonymous]` + rate limit | `condiciones` | `q` (min 1 char) | ✅ OK — devuelve padres primero, sin datos de pacientes |
| Síntomas autocomplete | `GET /api/sintomas/autocomplete?q=&excludeIds=` | `[AllowAnonymous]` + rate limit | `sintomas` | `q`, `excludeIds` (CSV) | ✅ OK — soporta exclusión de IDs |
| Tratamientos autocomplete | `GET /api/tratamientos/autocomplete?q=` | `[AllowAnonymous]` + rate limit | `tratamientos` | `q` | ✅ OK |

**Nota SEC-001/002/003:** acceso anónimo intencional — son taxonomía médica genérica, no datos de pacientes. Rate limiting `catalogos-autocomplete` en `Program.cs`.

---

## RATINGS DE ARTÍCULOS
**Controller:** `Controllers/ArticleRatingsApiController.cs`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| GET estadísticas | `GET /api/articles/{id}/rating` | Anónimo (stats) / auth para voto usuario | `ArticleRatings` | `articleId` | ✅ OK |
| CREATE / UPDATE rating | `POST /api/articles/{id}/rating` | Anónimo (IP) + autenticado (userId) | `ArticleRatings` | `articleId`, `ratingType` (like/dislike) | ✅ OK — upsert por userId o IP ±24h |

---

## RATINGS DE GLOSARIO
**Controller:** `Controllers/GlossaryRatingsApiController.cs`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| GET estadísticas | `GET /api/glossary/{id}/rating` | Anónimo (stats) | `GlossaryTermRatings` | `termId` | ✅ OK |
| CREATE / UPDATE rating | `POST /api/glossary/{id}/rating` | Anónimo (IP) + autenticado | `GlossaryTermRatings` | `termId`, `ratingType` | ✅ OK — mismo patrón que Article |

---

## FEEDBACK RESPUESTAS IA
**Controller:** `Controllers/RespuestaFeedbackApiController.cs`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| CREATE / UPDATE feedback | `POST /api/respuestas/{id}/feedback` | `[Authorize]` | `RespuestaAIFeedbacks` | `respuestaId`, `esUtil`, `comentario?` | ✅ OK — solo respuestas IA, upsert por userId |
| GET estadísticas | `GET /api/respuestas/{id}/feedback` | `[AllowAnonymous]` | `RespuestaAIFeedbacks` | `respuestaId` | ✅ OK |
| DELETE feedback | `DELETE /api/respuestas/{id}/feedback` | `[Authorize]` | `RespuestaAIFeedbacks` | `respuestaId` | ✅ OK — hard delete del propio feedback |

---

## MOOD RÁPIDO (push)
**Controller:** `Controllers/MoodApiController.cs`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| CREATE mood quick | `POST /api/mood/quick?token=&valor=` | Token de un solo uso (5 min) | `EstadoAnimoUsuario` | `token`, `valor` (1-5) | ✅ OK — `[IgnoreAntiforgeryToken]` intencional, idempotencia ±5 min |

---

## PUSH NOTIFICATIONS
**Controller:** `Controllers/PushApiController.cs`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| GET VAPID key | `GET /api/push/vapid-public-key` | Anónimo | — | — | ✅ OK |
| POST subscribe | `POST /api/push/subscribe` | `[Authorize]` | `PushSubscriptions` | `endpoint`, `keys` | ✅ OK |

---

## BÚSQUEDA
**Controller:** `Controllers/SearchApiController.cs`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| GET suggestions | `GET /api/search/suggestions?q=&condicionId=` | `[AllowAnonymous]` | `Preguntas`, `Contenidos`, `Respuestas` | `q` (min 20 chars), `condicionId?` | ✅ OK — solo lectura, sin datos de paciente |

---

## OPCIONES MOOD (usuario)
**Controller:** `Controllers/UsuarioController.cs`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| GET opciones relación mood | `GET /api/Usuario/RelacionesMoodOptions` | `[Authorize]` | `sintomasUsuario`, `condicionUsuario`, `tratamientoUsuario` | — | ✅ OK — solo devuelve datos del usuario autenticado |

---

## DIAGNÓSTICO NINA (solo desarrollo)
**Controller:** `Controllers/DiagnosticoNinaController.cs`

| Operación | Endpoint | Acceso | Entidad | Parámetros | Estado |
|---|---|---|---|---|---|
| GET test | `GET /api/diagnostico-nina/test` | `[DevelopmentOnly]` + `[Authorize(Roles="Administrador")]` | — | `pregunta?` | ✅ OK — solo en Development, solo admin |

---

## RESUMEN DE ESTADO

| Módulo | Handlers auditados | ✅ OK | ⚠️ Riesgo | ❌ Error | 🔍 Pendiente |
|---|---|---|---|---|---|
| Mis Condiciones | 5 | 5 | 0 | 0 | 0 |
| Mis Síntomas | 10 | 10 | 0 | 0 | 0 |
| Mis Tratamientos | 10 | 10 | 0 | 0 | 0 |
| Seguimiento | 2 | 2 | 0 | 0 | 0 |
| Laboratorios | 4 | 4 | 0 | 0 | 0 |
| Directorio (público) | 3 | 3 | 0 | 0 | 0 |
| Directorio (proponer) | 2 | 2 | 0 | 0 | 0 |
| Directorio (admin) | 14 | 14 | 0 | 0 | 0 |
| Preguntas y Respuestas | 9 | 9 | 0 | 0 | 0 |
| Estado de Ánimo | 7 | 7 | 0 | 0 | 0 |
| Mood rápido (push) | 1 | 1 | 0 | 0 | 0 |
| Reclamar Perfil | 2 | 2 | 0 | 0 | 0 |
| Autocomplete APIs | 3 | 3 | 0 | 0 | 0 |
| Ratings Artículos | 2 | 2 | 0 | 0 | 0 |
| Ratings Glosario | 2 | 2 | 0 | 0 | 0 |
| Feedback Respuestas IA | 3 | 3 | 0 | 0 | 0 |
| Push Notifications | 2 | 2 | 0 | 0 | 0 |
| Búsqueda | 1 | 1 | 0 | 0 | 0 |
| Opciones Mood | 1 | 1 | 0 | 0 | 0 |
| Diagnóstico NINA | 1 | 1 | 0 | 0 | 0 |
| **TOTAL** | **83** | **83** | **0** | **0** | **0** |
