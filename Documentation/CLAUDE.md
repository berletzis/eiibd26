# CLAUDE.md — Fuente de verdad para Claude Code · Proyecto EIIBD

> Este archivo es la documentación oficial del proyecto para Claude Code.
> Toda documentación vive en `Documentation/`. No crear .md dentro de `eiibd26/`.

---

## Rol y contexto
Eres un desarrollador senior especializado en ASP.NET Core 8, Razor Pages,
EF Core y SQL Server. Este proyecto tiene ~1000 usuarios en producción.
Priorizas estabilidad sobre velocidad. Nada de experimentos en producción.

---

## Antes de cualquier cambio
1. Lee los archivos relevantes completos — nunca asumas el contenido
2. Identifica TODAS las tablas y modelos involucrados
3. Verifica build antes de empezar (`dotnet build --no-restore`)
4. Si hay ambigüedad, pregunta antes de actuar

---

## Reglas estrictas
- NO reescribir lógica de negocio — solo mover o corregir lo necesario
- NO modificar queries ni cálculos sin autorización explícita
- NO cambiar rutas públicas (SEO en producción)
- NO introducir CQRS, MediatR ni patrones nuevos sin discutir
- Restricción de scope: se puede modificar cualquier proyecto de la solución (incluido NINA-WorkerService) cuando sea el objetivo explícito de la tarea acordada. Lo prohibido es el cambio disperso: tocar proyectos o archivos ajenos al objetivo "de paso". Cada sesión define qué proyecto(s) están en scope y no se sale de ahí sin confirmarlo. Conectar3eros permanece fuera de scope salvo indicación explícita.
- NO hacer migraciones EF Core — los cambios de esquema se hacen con SQL directo
- NO crear archivos `.md` dentro de `eiibd26/` — toda documentación va en `Documentation/`
- Trabajar fase por fase — build limpio entre cada fase

---

## Stack técnico
- **Backend**: ASP.NET Core 8 · Razor Pages + MVC Controllers
- **ORM**: Entity Framework Core 8 · SQL Server
- **Identidad**: ASP.NET Identity · Roles: `Administrador`, `Medico`, `(Paciente/default)`
- **Frontend**: Bootstrap 5 · Vanilla JS · partial views
- **Servicios externos**: SendGrid (email) · Twilio (SMS) · WebPush/VAPID · QuestPDF
- **Background jobs**: Hangfire (queues: `default`, `ai`) · 2 workers
- **IA**: Anthropic Claude API (claude-sonnet-4-6 / claude-haiku-4-5)
- **Imagen**: SixLabors.ImageSharp
- **Markdown**: Markdig

---

## Arquitectura del proyecto

### Estructura de carpetas (repo raíz)
```
eiibd/eiibd26/
├── eiibd26/                   ← Proyecto ASP.NET Core principal
│   ├── Areas/Identity/Pages/  ← Razor Pages de Identity (Auth, Admin, Medico)
│   ├── Controllers/           ← API Controllers (prefijo /api/)
│   ├── Data/                  ← ApplicationDbContext
│   ├── Models/                ← Modelos EF Core
│   ├── Pages/                 ← Razor Pages públicas
│   ├── Services/              ← Lógica de negocio
│   └── wwwroot/               ← Archivos estáticos (NO poner .md aquí)
├── NINA-WorkerService/        ← Worker/scraper — fuera de scope salvo tarea explícita
├── Conectar3eros/             ← Integración terceros — NO TOCAR
├── SQL/                       ← Scripts SQL de cambios de esquema
├── Documentation/             ← TODA la documentación (este folder)
│   ├── CLAUDE.md              ← Este archivo (fuente de verdad)
│   └── planes/                ← Planes de sesiones anteriores
└── CLAUDE.md                  ← Root: importa este archivo
```

### Areas de Identity (rutas principales)
- `/Identity/Account/` — Login, Register, RegisterM (médicos)
- `/Identity/Account/Manage/` — PerfilMedico, UsuarioPerfil
- `/Identity/Admin/` — Panel de administrador
- `/Identity/Medico/` — Dashboard médico

---

## Servicios clave

### NINA (IA Router) — `Services/AI/NinaModelRouterService.cs`
Router que selecciona Sonnet/Haiku/Base según complejidad de la pregunta.
Respuestas guardadas en tabla `Respuestas` con `EsIA = true`.
- Encolado via Hangfire (`AiAnswerJob`)
- Safety layers: forbidden phrases → fallback → disclaimer obligatorio
- Costo estimado: ~$0.01/pregunta · ~$1–10/mes según volumen

### Glosario — `Services/Glossary/GlossaryService.cs`
Glosario médico con adaptador, caché y validación comunitaria.

### PDF — `Services/Export/MedicalSummaryService.cs` + `PdfGeneratorService`
Resúmenes médicos exportables en PDF (QuestPDF).

### HealthInsight — `Services/Analytics/HealthInsightService.cs`
Insights clínicos calculados en memoria, sin queries extra a DB.

### MedicoBadge — `Services/Medico/IMedicoBadgeService.cs`
Evaluación automática de badges para médicos del directorio.
Se llama después de confirmar reclamaciones o guardar perfil.
**Envolver siempre en try-catch** — un fallo no debe bloquear otros saves.

---

## Modelos y tablas principales

### Perfil (tabla `Perfil`)
- PK: `idUser` (Guid) — FK a AspNetUsers
- `Avatar` (string NOT NULL) · `Nombre` (string NOT NULL) · `FechaCreacion`
- Privacy flags: `PermitirTelefonoReal`, `PermitirCorreoNoticias`, `AceptoPP`,
  `PermitirMostrarPais`, `PermitirCompartirDatosMedicos` — todos `bool?`
- **Nunca bindear `Perfil` completo como `[BindProperty]`** — sus campos
  `[Required]` causan errores de ModelState. Usar propiedades bool individuales.

### MedicoPerfilExtendido (tabla `MedicoPerfilExtendido`)
- PK: `Id` (int) · FK `MedicoId` → `MedicosDirectorio` · FK `UserId` → AspNetUsers
- Foto guardada en `wwwroot/uploads/medicos/medico-{guid}.jpg`
- `Hospitales`: JSON serializado `["hospital1","hospital2"]` — siempre deserializar

### MedicoDirectorio (tabla `MedicosDirectorio`)
- Campo `AspNetUserId` (Guid?) — se setea cuando admin aprueba reclamación
- Campo `EmailSolicitudClaim` — email del médico al momento de reclamar
- Auto-link: buscar por `AspNetUserId == userId OR EmailSolicitudClaim == email`

### MedicoAreaEii (tabla `MedicoAreaEii`)
- Clave compuesta: `(MedicoPerfilId, CondicionId)` — configurada en OnModelCreating
- Patrón de guardado: delete-all + insert-new (siempre dentro de `if (ModelState.IsValid)`)

---

## Patrones establecidos

### Checkboxes bool en Razor Pages
```html
<!-- CORRECTO: hidden false + checkbox true con el mismo name -->
<input type="hidden" name="MiBool" value="false" />
<input type="checkbox" name="MiBool" value="true" @(Model.MiBool ? "checked" : "") />
```
```csharp
[BindProperty] public bool MiBool { get; set; }
```

### Fotos de médico → sincronizar con Perfil.Avatar
Cuando se guarda una foto en `MedicoPerfilExtendido.Foto`, también actualizar
`Perfil.Avatar` al mismo path para que el top-menu lo muestre correctamente.

### Avatar en top-menu
- Desktop (`_TopMenuDesktop.cshtml`): lee `Perfil.Avatar` desde DB (con inject `ApplicationDbContext`)
- Mobile (`_TopMenuMobile.cshtml`): igual, inject `ApplicationDbContext`
- Prioridad: `Perfil.Avatar` si empieza con `/uploads/` → else filesystem fallback

### Hospitales (campo JSON)
En Detalle.cshtml.cs `CargarUbicacionesAsync`:
```csharp
// CORRECTO: deserializar JSON primero
try { hospitales = JsonSerializer.Deserialize<List<string>>(perfil.Hospitales); }
catch { hospitales = perfil.Hospitales.Split('\n', ';', '|'); } // fallback legacy
```

### Privacidad en PerfilMedico
Los 5 flags de privacidad van como `[BindProperty] bool Priv*` individuales.
El bloque de guardado de `Perfil` debe ejecutarse SIEMPRE (no dentro de `if ModelState.IsValid`).
`EvaluarBadgesAutomaticosAsync` debe estar en try-catch para no bloquear el guardado.

---

## Cuando hay error de ModelState
- Loguear **todos** los campos que fallan antes de retornar
- Nunca mostrar mensaje genérico sin especificar qué campo
- Patrón de logging:
```csharp
var errores = ModelState
    .Where(x => x.Value?.Errors.Count > 0)
    .SelectMany(x => x.Value!.Errors.Select(e =>
        string.IsNullOrWhiteSpace(e.ErrorMessage)
            ? $"Campo '{x.Key}': valor inválido"
            : e.ErrorMessage))
    .ToList();
ErrorMessage = "Por favor corrige: " + string.Join("; ", errores);
```

---

## Cambios de esquema (SQL directo)
NO se usan migraciones EF Core. Los cambios van en `SQL/` como scripts idempotentes.
Ejemplo de columna nullable:
```sql
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tabla')
               AND name = 'NuevaColumna')
    ALTER TABLE dbo.Tabla ADD NuevaColumna BIT NULL;
```

---

## Al terminar cada tarea
1. `dotnet build --no-restore` — cero errores CS
2. Confirmar qué archivos se modificaron
3. Sugerir commit message

---

## Referencia de conexión BD (local dev)
Servidor: user secrets (`ConnectionStrings:DefaultConnection`)
No hardcodear credenciales. Ver `SECRETS.md` en raíz del repo (no commitear).

---

## SESIONES REGISTRADAS

### 📅 Sesión: Admin Directory - Badges y Confirmaciones (Enero 2025)

**Contexto**: Revisión completa de la sección de administración de médicos para aplicar la misma normalización de badges del directorio público y agregar moderación de confirmaciones comunitarias.

**Archivos modificados**:
- `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml`
- `Areas/Identity/Pages/Admin/DirectorioMedicos/Index.cshtml.cs`

**Cambios implementados**:

1. **Normalización de badges en admin grid**
   - Actualizada función `triBadges(row)` con tooltips canónicos:
     - 🟢 "Validado por Pacientes (≥5 confirmaciones)"
     - 🔵 "Cédula Verificada"  
     - 🟢 "Perfil Reclamado"
   - Eliminada ambigüedad entre nombres duplicados
   - Umbral de confirmaciones actualizado a 5 (coherente con badge DB)

2. **Sistema de moderación de confirmaciones comunitarias**
   - Nueva tabla mejorada en panel lateral con columnas:
     - Email (usuario confirmador)
     - Fecha (de la confirmación)
     - Tipo (tipo de confirmación)
     - **Estado** (badge visual: Activa/En revisión)
     - **Acción** (botón toggle por confirmación)
   - Contador inteligente: `Total: 12 (10 activas, 2 en revisión)`
   - Reutilización de campo `ConfirmacionComunitaria.Eliminado`:
     - `false` → Confirmación activa (cuenta para badges y nivel)
     - `true` → Confirmación en revisión (preservada pero no cuenta)

3. **Handler de moderación** (`OnPostToggleConfirmacionAsync`)
   - Toggle reversible del estado de confirmación
   - Recalculo automático de nivel de confianza del médico
   - Re-evaluación automática de badges (badge comunidad puede cambiar)
   - Logging implícito vía EF Core SaveChanges

4. **Flujo de datos actualizado**
   - Query usa `.IgnoreQueryFilters()` para mostrar todas las confirmaciones
   - DTO distingue `totalConfirmaciones` (activas) vs `totalConfirmacionesIncRevision`
   - Confirmaciones en revisión aparecen con fondo amarillo en tabla

**Documentación generada**:
- `Documentation/directorio-profesionales-badges/admin-confirmaciones-revision.md`
  - Flujo de moderación completo
  - Impacto en badges y nivel de confianza
  - Casos de uso (spam, disputas, errores, reactivación)
  - Código relevante (backend + frontend)

**Ventajas del sistema**:
- ✅ No destructivo (confirmaciones se preservan, no se borran)
- ✅ Reversible (admin puede reactivar en cualquier momento)
- ✅ Automático (nivel y badges se recalculan inmediatamente)
- ✅ Auditable (todas las confirmaciones quedan en DB con su estado)
- ✅ Transparente (admin ve desglose exacto activas vs revisión)

**Estado**: Compilación exitosa (solo warning Hot Reload, no errores)

**Próximos pasos sugeridos**:
- Reiniciar aplicación para aplicar cambios (o detener debugger)
- Verificar flujo de moderación en browser
- Considerar agregar log explícito de cambios de estado en futuras iteraciones

---

## Referencias rápidas de sesiones anteriores

Ver carpeta `Documentation/sesiones/` para sesiones anteriores detalladas.
Ver carpeta `Documentation/directorio-profesionales-badges/` para todo el análisis de badges.

---

## Patrones de Plataformas de Comunidad — Guía de diseño para EIIBD

Conocimiento condensado de cómo se comportan las plataformas de comunidad maduras
(Stack Overflow, Reddit, Discourse, foros clásicos, blogs con comentarios).
**Propósito**: al diseñar o implementar CUALQUIER feature de comunidad en EIIBD
(preguntas, respuestas, votos, moderación, contenido, perfiles), pasar por estos
checklists para no dejar huecos. Aplica tanto al diseño (chat) como a la
implementación (Claude Code).

---

### REGLA DE ORO

Cada vez que un contenido cambia de estado (se modera, se cierra, se elimina, se
oculta), preguntarse SIEMPRE: **"¿qué acciones quedan abiertas que ya no deberían?"**
Mostrar el nuevo estado NO es suficiente. Hay que cerrar todas las interacciones que
ese estado vuelve inválidas — y cerrarlas en el **BACKEND**, no solo ocultarlas en el front.

---

### 1. ESTADOS DE CONTENIDO

Las plataformas maduras no tienen solo "existe / no existe". Tienen múltiples estados,
y cada uno habilita/bloquea acciones distintas. En EIIBD, los estados relevantes:

| Estado | Visible | Permite interacción | Uso |
|---|---|---|---|
| Activo | Sí | Sí | normal |
| Eliminado (soft) | No | No | borrado, recuperable |
| Deshabilitado/Moderado | Sí, con leyenda | No | infringió políticas |
| (futuro) Cerrado | Sí | Lee sí, responde no | pregunta resuelta o duplicada |

**Checklist al agregar/cambiar un estado:**
- ¿El contenido se muestra o se oculta?
- Si se muestra, ¿con qué leyenda/indicador?
- ¿Qué interacciones se bloquean? (ver sección 2)
- ¿Es reversible? ¿quién puede revertirlo?
- ¿El bloqueo está en el **BACKEND** (endpoint) y no solo en el front?

---

### 2. MATRIZ DE INTERACCIONES A BLOQUEAR

Cuando un contenido se modera/elimina/cierra, revisar TODAS estas interacciones.
Es el checklist que evita el hueco clásico de "deshabilité pero deja responder":

**Para una PREGUNTA bloqueada:**
- [ ] Formulario de agregar respuesta (front: ocultar; backend: rechazar)
- [ ] Endpoint de crear respuesta (validar estado de la pregunta)
- [ ] Votos a la pregunta (front + backend)
- [ ] Compartir / generar short-url (¿tiene sentido compartir algo moderado?)
- [ ] Edición por el dueño
- [ ] Aparición en listados/búsqueda/sugerencias de relacionados

**Para una RESPUESTA bloqueada:**
- [ ] Votos a la respuesta
- [ ] Feedback 👍/👎 (si es respuesta IA)
- [ ] Replies / respuestas hijas
- [ ] Marcarla como aceptada
- [ ] Edición por el dueño

**Principio**: la defensa real va en el **BACKEND**. Ocultar el botón en el front es UX,
pero un endpoint abierto se puede llamar directo. Validar el estado en el servidor SIEMPRE.

---

### 3. PERMISOS Y AUTORÍA

- Separar SIEMPRE "quién puede hacer" de "quién figura como autor". Una cuenta
  etiqueta (ej. "Comunidad EIIBD") NO debe tener rol admin solo para figurar como autor.
  El acceso lo dan cuentas personales; la autoría es un dato.
- El dueño puede X, el admin puede X sobre cualquiera. Endpoints separados: el del
  dueño valida `UsuarioId == userId`; el admin valida rol, sin el check de dueño.
- Contenido del sistema/IA (NINA): su respuesta SÍ es moderable (se puede bajar una
  respuesta IA mala), pero el usuario sistema como AUTOR no se borra.
- Credenciales compartidas = trazabilidad perdida. Evitar cuentas admin compartidas.

---

### 4. MODERACIÓN: ELIMINAR vs OCULTAR vs DESHABILITAR

Tres conceptos distintos que las plataformas separan:

- **Eliminar (soft-delete)**: desaparece, recuperable internamente. Para spam/error.
- **Deshabilitar/moderar**: se ve con leyenda "infringió políticas". Es pedagógico —
  comunica que hubo una violación sin borrar la evidencia.
- **Cerrar**: se ve completo, se puede leer, pero no admite nuevas respuestas
  (pregunta resuelta, duplicada, off-topic).

**Regla aditiva vs fraude**: la moderación normal NO revoca reputación/badges ganados.
El fraude (cuenta falsa que infló su nivel) SÍ borra todo en cascada — es la excepción,
debe diseñarse explícitamente y por separado.

**Cascada de borrado**: borrar una entidad deja huérfanos (validaciones, votos, avatares,
links que apuntan a ella → 404). SIEMPRE mapear qué cuelga de una entidad antes de
borrarla, y limpiar en cascada de hijas a padres.

---

### 5. DUPLICADOS Y CALIDAD

- Las comunidades acumulan duplicados inevitablemente (misma pregunta/término/receta).
  Detectarlos con similitud semántica (umbral configurable) antes de que se multipliquen.
- Contenido de baja calidad: detectable con señales simples (sin imagen, muy corto, sin
  categoría) + señales con IA (ortografía, coherencia). Un semáforo 🟢🟡🔴 ayuda a triar.
- Sugerir contenido relacionado al **crear** (no solo al leer) reduce duplicados.

---

### 6. ANTI-ABUSO Y MÉTRICAS LIMPIAS

- **Doble envío**: deshabilitar el botón al primer clic. Sin esto, doble-clic crea
  duplicados (y dispara procesos como IA dos veces, gastando recursos).
- **Bots de preview**: las redes sociales (Facebook, WhatsApp, etc.) visitan los links
  para generar vista previa. Esos hits NO son humanos — filtrarlos del conteo o las
  métricas quedan infladas.
- **Recargas/inflado**: deduplicar clicks del mismo visitante en ventana corta.
- **Rate limiting** en endpoints públicos de redirect/acción, generoso para no afectar
  uso legítimo (ej. 30/min por IP).
- **Idempotencia**: operaciones de creación deben tolerar reintentos sin duplicar.

---

### 7. SEO Y URLS (sitio que vive de tráfico orgánico)

- URLs públicas con slug legible, NO opacas. El slug comunica el tema y suma SEO.
- Cambios de URL → 301 redirect, nunca romper URLs indexadas.
- Acortadores propios: útiles para compartir EXTERNO con tracking (Facebook, etc.), NO
  para reemplazar las URLs públicas internas (perderías SEO y legibilidad).
- Redirect de short-url: **302** (no 301) para que cada visita cuente.

---

### 8. CHECKLIST RÁPIDO ANTES DE CERRAR CUALQUIER FEATURE DE COMUNIDAD

- [ ] ¿Cubrí todos los **ESTADOS** del contenido? (activo/eliminado/moderado/cerrado)
- [ ] ¿Bloqueé **TODAS** las interacciones que el nuevo estado invalida? (sección 2)
- [ ] ¿La defensa está en el **BACKEND**, no solo en el front?
- [ ] ¿Separé autoría de permisos?
- [ ] ¿Hay huérfanos al borrar? ¿limpié en cascada?
- [ ] ¿Anti-doble-envío en los botones de creación?
- [ ] ¿Las métricas excluyen bots y recargas?
- [ ] ¿Las URLs públicas conservan slug + SEO?
- [ ] ¿Es reversible lo que debería ser reversible? ¿quién lo revierte?
- [ ] ¿Probé el caso límite a propósito (no solo el camino feliz)?
