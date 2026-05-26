# 04 · Deuda técnica — Dependencias

**Auditoría:** 09dependencias.html  
**Fecha:** 2025  

---

## Findings diferidos

Los siguientes findings de la auditoría `09dependencias.html` **no se modifican en esta iteración** por riesgo de rotura o por requerir trabajo de migración mayor.

---

### DEP-003 · Hangfire — versión legacy
| Campo | Valor |
|-------|-------|
| Finding | DEP-003 |
| Severidad | MEDIUM |
| Paquete | `Hangfire` (y componentes) |
| Estado actual | Funcional en producción |
| Razón de diferimiento | Hangfire tiene su propio modelo de versionado. Una actualización mayor puede requerir migración de esquema de la BD de jobs. |
| Prerequisito | Revisar changelogs de Hangfire · Probar en ambiente de staging antes de actualizar |

---

### DEP-005 · QuestPDF — cambio de licencia en versiones recientes
| Campo | Valor |
|-------|-------|
| Finding | DEP-005 |
| Severidad | MEDIUM |
| Paquete | `QuestPDF` |
| Estado actual | Funcional |
| Razón de diferimiento | Versiones recientes tienen modelo de licencia comercial. Actualizar requiere evaluar impacto legal antes de cambio de versión. |
| Prerequisito | Revisar términos de licencia de la versión objetivo |

---

### DEP-007 · Twilio — versión mayor disponible
| Campo | Valor |
|-------|-------|
| Finding | DEP-007 |
| Severidad | MEDIUM |
| Paquete | `Twilio` |
| Estado actual | Funcional |
| Razón de diferimiento | Cambio de versión mayor con breaking changes documentados en la API de Twilio. Requiere revisión de código de integración. |
| Prerequisito | Revisar Twilio migration guide · Auditar todos los usos de `TwilioClient` y SMS/WhatsApp services |

---

### DEP-008 · WebPush — sin mantenimiento activo
| Campo | Valor |
|-------|-------|
| Finding | DEP-008 |
| Severidad | LOW |
| Paquete | `WebPush` |
| Estado actual | Funcional |
| Razón de diferimiento | Requiere buscar alternativa mantenida o asumir el mantenimiento del código. No hay versión nueva disponible. |
| Prerequisito | Evaluar alternativas (`Lib.AspNetCore.WebPush` u otras) · Verificar que el feature de push notifications justifica la migración |

---

## Criterios para abordar la deuda

1. **DEP-003 (Hangfire):** Prioritario cuando se planifique una ventana de mantenimiento de la BD
2. **DEP-005 (QuestPDF):** Requiere aprobación legal antes de avanzar
3. **DEP-007 (Twilio):** Puede abordarse en el sprint de mejoras de comunicaciones
4. **DEP-008 (WebPush):** Baja prioridad mientras el feature funcione

---

_Estos findings permanecen como OPEN en `09dependencias.html`_
