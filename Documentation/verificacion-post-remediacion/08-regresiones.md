# 08 – Regresiones: Módulos Afectados

**Fecha:** 2025-07-10

---

## Metodología

Verificación estática: grep de uso de funciones modificadas, DI, y compilación.  
Sin ejecución de tests automatizados (no existe suite de tests en el proyecto).

---

## Módulo 1: IA (ARCH-003 + FUNC-017)

| Verificación | Resultado |
|-------------|-----------|
| `VIH/HIV/SIDA` eliminados de `NinaModelRouterService` | ✅ Sin matches |
| Foco EII activo (`EII`, `Crohn`, `Colitis`, `IBDKnowledge`) | ✅ Presente |
| `IBDKnowledgeTemplates.TryResolve` referenciado | ✅ Línea 261 |
| `AiAnswerJob.ProcesarPreguntaAsync` compila | ✅ Build succeeded |
| Job registrado en DI | ✅ `AddScoped<AiAnswerJob>()` |
| Fire-and-forget eliminado | ✅ No hay `Task.Factory.StartNew` en controllers |
| **Estado** | ✅ Sin regresión |

---

## Módulo 2: Directorio Médicos (FUNC-022 + FUNC-023)

| Verificación | Resultado |
|-------------|-----------|
| `Activar.cshtml.cs` persiste `EstatusReclamacion = Reclamado` | ✅ Línea 178 |
| `Activar.cshtml.cs` llama `RecalcularNivelConfianzaAsync` | ✅ Línea 198 |
| `Detalle.cshtml.cs` usa `DirectorioMedicoConfirmaciones` | ✅ |
| `MedicoDirectorioService.RecalcularNivelConfianzaAsync` usa tabla canónica | ✅ |
| Admin `RecalcularNivelAsync` usa tabla canónica | ✅ |
| IMedicoDirectorioService declara `RecalcularNivelConfianzaAsync` | ✅ Línea 23 |
| Build compila sin error en archivos de Directorio | ✅ |
| **Estado** | ✅ Sin regresión |

---

## Módulo 3: P&R (Preguntas y Respuestas)

| Verificación | Resultado |
|-------------|-----------|
| `CrearPregunta` genera slug único (SlugHelper) | ✅ |
| `ActualizarPregunta` preserva slug existente | ✅ |
| Ownership en delete | ✅ `p.UsuarioId != userId.Value` |
| Ownership en update | ✅ `pregunta.UsuarioId != userId.Value` |
| Voting endpoint compila | ✅ |
| Token CSRF en Detalles.cshtml (hidden form + header en feedback) | ✅ |
| Token CSRF en Preguntas.cshtml (voting list) | ❌ Ausente — Documentado en `03-seguridad.md` R-SEC-01 |
| **Estado** | ⚠️ Sin regresión funcional, WARN CSRF en voting list |

---

## Módulo 4: Dashboard

| Verificación | Resultado |
|-------------|-----------|
| `DashboardController.AddSymptom` valida ownership | ✅ `_ownership.OwnsSintomaAsync` |
| `DashboardController.AddMood` tiene `[ValidateAntiForgeryToken]` | ✅ |
| DI de `ClinicalOwnershipValidator` en DashboardController | ✅ Constructor injection |
| Build compila | ✅ |
| **Estado** | ✅ Sin regresión |

---

## Módulo 5: Estado Ánimo / Mood

| Verificación | Resultado |
|-------------|-----------|
| `EstadoAnimoUsuarioController.Registrar` valida FK opcionales | ✅ `ValidateEstadoAnimoRelationsAsync` |
| `EstadoAnimoUsuarioController.Eliminar` filtra por userId | ✅ |
| `MoodApiController` tiene `[IgnoreAntiforgeryToken]` justificado (push token) | ✅ Justificado |
| Build compila | ✅ |
| **Estado** | ✅ Sin regresión |

---

## Módulo 6: Síntomas / Condiciones / Laboratorios

| Verificación | Resultado |
|-------------|-----------|
| `ClinicalOwnershipValidator` tiene métodos para condicion, sintoma, tratamiento, estadoanimo | ✅ |
| Métodos usados en controllers | ✅ (EstadoAnimo + Dashboard) |
| Build de clases clínicas sin errores | ✅ |
| **Estado** | ✅ Sin regresión detectada (verificación parcial — sin cobertura exhaustiva de todos los endpoints) |

---

## Módulo 7: Hangfire

| Verificación | Resultado |
|-------------|-----------|
| `AddHangfire` con SQL Server storage | ✅ |
| `AddHangfireServer` con WorkerCount=2 | ✅ |
| Dashboard en `/hangfire` con auth | ✅ |
| `IBackgroundJobClient` inyectado en `PreguntasApiController` | ✅ |
| `Enqueue<AiAnswerJob>` en `CrearPregunta` | ✅ |
| Build compila Hangfire integration | ✅ |
| **Estado** | ✅ Sin regresión |

---

## Módulo 8: Base de Datos (EF + SQL)

| Verificación | Resultado |
|-------------|-----------|
| `DeleteBehavior.Restrict` en `MedicoAreaEii → Condicion` | ✅ ApplicationDbContext línea 701 |
| 6 índices clínicos en `HasIndex` | ✅ ApplicationDbContext líneas 498–526 |
| SQL script idempotente | ✅ Todos con `IF NOT EXISTS` |
| Build de ApplicationDbContext sin errores | ✅ |
| **Estado** | ✅ Sin regresión (verificación de DB real pendiente) |

---

## Servicios Críticos — Comprobación DI Final

| Servicio | Tipo | Registrado |
|----------|------|------------|
| `ClinicalOwnershipValidator` | Scoped | ✅ |
| `MedicoDirectorioService` / `IMedicoDirectorioService` | Scoped | ✅ |
| `SearchSuggestionService` | (con IMemoryCache) | ✅ |
| `AiAnswerJob` | Scoped | ✅ |
| Hangfire `IBackgroundJobClient` | Framework | ✅ |

---

## Errores Runtime Encontrados

Ninguno detectado vía análisis estático de código. El único error de compilación MSB fue por lock de archivo de proceso en ejecución (no es error de código).

---

## Regresiones Confirmadas

**Ninguna regresión confirmada** en los módulos verificados.

---

## Advertencias No Bloqueantes

| ID | Módulo | Descripción |
|----|--------|-------------|
| WARN-01 | P&R | CSRF en voting list (`Preguntas.cshtml`) — fetch sin token |
| WARN-02 | Directorio | Admin `RecalcularNivelAsync` duplica lógica del servicio |
| WARN-03 | Admin | 2 acciones con `IgnoreAntiforgeryToken` en admin Usuarios sin justificación |
| WARN-04 | Admin | `Contenidos.cshtml.cs` suprime antiforgery a nivel de clase |

---

## Veredicto Fase 8

| Criterio | Estado |
|----------|--------|
| Regresiones de compilación | ✅ Ninguna |
| Regresiones funcionales | ✅ Ninguna detectada |
| Servicios DI rotos | ✅ Ninguno |
| Errores runtime detectados estáticamente | ✅ Ninguno |
| Advertencias de seguridad pendientes | ⚠️ 4 WARN documentados |
| **VEREDICTO** | ✅ **SIN REGRESIONES DETECTADAS** |
