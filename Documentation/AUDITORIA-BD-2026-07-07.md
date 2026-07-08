# AUDITORÍA DE BASE DE DATOS — eiibd26

**Fecha:** 2026-07-07
**Base:** `eiibd26` (SQL Server, producción — `[servidor-redactado]`)
**Alcance:** Solo lectura absoluta.

> ## ⚠️ Estado de esta auditoría
>
> **Esta versión está reconstruida a partir del CÓDIGO en disco** (entidades EF Core, `OnModelCreating`, y scripts idempotentes en `SQL/`). **NO se ejecutó ninguna consulta contra la base de datos de producción.**
>
> Falta la **verificación en vivo** (inventario real de tablas, `COUNT(*)` de filas, índices reales, columnas huérfanas), que requiere una cadena de conexión. Por seguridad:
>
> - **NO se debe usar la cuenta `sa`** para esto (ver hallazgo crítico abajo).
> - Se requiere una cadena de un usuario **`db_datareader`** de solo lectura.
> - Consultas permitidas: `INFORMATION_SCHEMA.TABLES/COLUMNS`, `sys.tables`, `sys.indexes`, `sys.foreign_keys`, y `COUNT(*)` por tabla. **Nada de `SELECT *`** sobre tablas con datos personales/clínicos; solo nombres de columnas y conteos.
>
> Las secciones marcadas 🔴 **[PENDIENTE — requiere conexión db_datareader]** se completarán en una segunda pasada.

---

## 0. Hallazgo crítico de seguridad (bloquea la Parte B en vivo)

`appsettings.Production.json` tiene la cadena de conexión de producción con **cuenta `sa` y contraseña en texto plano** (además de 5 secretos de terceros: Google Maps, SendGrid, Twilio, VAPID, Anthropic). Ver detalle en `AUDITORIA-SOLUCION-2026-07-07.md §6`.

**Antes de la verificación en vivo, se recomienda:**
1. Crear un login SQL de solo lectura: `CREATE LOGIN [eiibd_readonly] ...; CREATE USER ...; ALTER ROLE db_datareader ADD MEMBER [eiibd_readonly];`
2. Entregar esa cadena (no la de `sa`) para completar esta auditoría.
3. Rotar `sa` y las demás credenciales expuestas.

Con eso, la auditoría en vivo es **físicamente incapaz** de modificar datos.

---

## 1. Inventario de tablas 🔴 [PENDIENTE — requiere conexión db_datareader]

> Query prevista (solo lectura, sin volcar contenido):
> ```sql
> SELECT t.name AS Tabla,
>        (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS NumColumnas
> FROM sys.tables t ORDER BY t.name;
> -- + COUNT(*) por tabla, ejecutado tabla por tabla (sin SELECT *).
> ```

Tablas **conocidas por el código** (deben existir en la base; el conteo de filas se llenará en vivo):

### 1.a Tablas del crawler / scraper (creadas por NINA-WorkerService, EF Core 10)
> ⚠️ **No tienen script en `SQL/`** — su DDL vive solo como entidades EF en `NINA-WorkerService/Models/`. Nombres reales (singular) según `Data/Eiibd26Context.cs:26-34`.

| Tabla | Escrita por el Worker | Filas |
|---|---|---|
| `SourceSite` | Sí (padre) | 🔴 pendiente |
| `ScrapedPage` | **Sí (salida principal, HTML crudo)** | 🔴 pendiente |
| `Article` | No (costura futura) | 🔴 pendiente |
| `ArticleTranslation` | No | 🔴 pendiente |
| `ArticleSummary` | No | 🔴 pendiente |
| `ArticleBoost` | No | 🔴 pendiente |
| `ArticleSimilarity` | No | 🔴 pendiente |
| `ScrapingJob` | No | 🔴 pendiente |
| `ScrapingJobLog` | No | 🔴 pendiente |

### 1.b Tablas de Contenidos (Web, EF Core 8)
`contenidos`, `contenidosRespuestas`, `contenidosCategorias`, `contenidosCategoriasRelacion`, `contenidosRelacionados`, `contenidosCalificacion_ArticulosPreguntas`, `contenidosCalificacion_Respuestas`, `contenidosPreguntasRelacion`, `contenidosRespuestasRelacion`, `contenidoCondicionesRelacion`, `contenidoSintomasRelacion`, `contenidoTratamientosRelacion`, `ArticleRatings`.

### 1.c Calidad y validación
`ContenidoCalidad`, `ValidacionesContenidoProfesional`, `ValidacionesRespuestaProfesional`.

### 1.d Otras del Web (de scripts `SQL/` y contexto)
`AspNetUsers` (+ tablas Identity), `Perfil`, `MedicosDirectorio`, `MedicoPerfilExtendido`, `MedicoPerfilBadge`, `MedicoBadge`, `MedicoAreaEii`, `Preguntas`, `Respuestas`, `Etiqueta`, `PreguntaEtiqueta`, `GlossaryTerm`, `GlossaryTermMedicalLink`, `GlossaryValidation`, `SendGridEventLog`, `ShortUrls`, `ShortUrlClicks`, `AIRequestLogs`, catálogos de laboratorio/síntomas/condiciones/tratamientos, etc.

> ⚠️ La lista es la **conocida por código**; el inventario real puede tener tablas legacy/huérfanas adicionales que solo el `sys.tables` en vivo revelará.

---

## 2. Esquema de tablas relevantes al crawler (derivado de EF)

### `ScrapedPage` — salida principal del crawler
| Columna | Tipo SQL | Null | PK/FK/Índice |
|---|---|---|---|
| `ScrapedPageId` | int IDENTITY | NOT NULL | **PK** |
| `SourceSiteId` | int | NOT NULL | **FK → SourceSite** |
| `Url` | nvarchar(max) | NOT NULL | (candidato a índice — verificar en vivo) |
| `TitleRaw` | nvarchar(max) | NULL | siempre NULL hoy |
| `ContentRaw` | nvarchar(max) | NOT NULL | HTML crudo |
| `ContentText` | nvarchar(max) | NULL | siempre NULL hoy |
| `Language` | nvarchar(max) | NOT NULL | default `"es"` |
| `PublishedAt` | datetime2 | NULL | |
| `ScrapedAt` | datetime2 | NOT NULL | |
| `HashContent` | varbinary(max) | NULL | SHA-256 (32 bytes) |
| `Status` | nvarchar(max) | NOT NULL | `OK`/`ERROR` |
| `ErrorMessage` | nvarchar(max) | NULL | |

### `SourceSite`
| Columna | Tipo SQL | Null | PK/FK |
|---|---|---|---|
| `SourceSiteId` | int IDENTITY | NOT NULL | **PK** |
| `Name` | nvarchar(max) | NOT NULL | |
| `BaseUrl` | nvarchar(max) | NOT NULL | |
| `Description` | nvarchar(max) | NULL | |
| `IsActive` | bit | NOT NULL | |
| `CreatedAt` | datetime2 | NOT NULL | |
| `UpdatedAt` | datetime2 | NULL | |

### Costuras futuras (definidas, hoy vacías)
- `Article` (`ArticleId` PK, `ScrapedPageId?` FK **1:1** → ScrapedPage, `NormalizedTitle`, `NormalizedContent`, `Language`, `MainTopic?`, `CreatedAt`, `UpdatedAt?`, `IsActive`).
- `ArticleTranslation` (`QualityScore` decimal(4,3)), `ArticleSummary` (`ModelUsed?`, `Version?`), `ArticleBoost` (`BoostScore` decimal(6,3)), `ArticleSimilarity` (PK compuesta `(ArticleId1, ArticleId2)`, `SimilarityScore` decimal(5,4), FKs con `NoAction`), `ScrapingJob`/`ScrapingJobLog`.

> 🔴 **[PENDIENTE en vivo]** Confirmar que estas tablas existen físicamente (fueron creadas por SQL directo, no por migración) y que están efectivamente vacías (`COUNT(*) = 0`).

---

## 3. Esquema de tablas relevantes a Contenidos (derivado de EF + `SQL/`)

### `contenidos` (entidad `Contenido`)
Columnas y tipos: ver `AUDITORIA-SOLUCION-2026-07-07.md §3.1`. PK `Id`; query filter global `!Eliminado`; índices en `IdAutor`, `IdUser`, `ContenidoFechaInicio`, `ContenidoFechaFin`.
Confirmado por `SQL/export-contenidos-soporte-tecnico.sql`: `contenidos(Id, ContenidoTitulo, ContenidoTituloSlug, EstadoPublicacion, FechaCreado, ContenidoTextoC, ContenidoTextoL, Eliminado, ...)`.

### `ContenidoCalidad` — confirmada por SQL directo
De `SQL/create-contenido-calidad.sql` + `alter-contenido-calidad-gris*.sql`:
| Columna | Tipo SQL | Null |
|---|---|---|
| `Id` | int IDENTITY | NOT NULL (PK) |
| `ContenidoId` | int | NOT NULL (**UNIQUE**) |
| `NivelSemaforo` | TINYINT | NOT NULL (0=Critico,1=Mejorable,2=Ok) |
| `Senales` | NVARCHAR(MAX) | NULL |
| `DuplicadoDeIds` | NVARCHAR(MAX) | NULL |
| `FechaAnalisis` | DATETIME2 | NOT NULL |
| `GrisEvaluado` | BIT | NOT NULL |
| `GrisPuntajeGlobal` | TINYINT | NULL |
| `GrisResultado` | NVARCHAR(MAX) | NULL |
| `GrisSugerencias` | NVARCHAR(MAX) | NULL |
| `GrisCategoriasSugeridas` | NVARCHAR(MAX) | NULL |
| `GrisCategoriasAlerta` | NVARCHAR(MAX) | NULL |
| `GrisFechaEvaluacion` | DATETIME2 | NULL |

Índice en `NivelSemaforo`.

### `ValidacionesContenidoProfesional` — confirmada por SQL directo
De `SQL/validacion-contenido/01-crear-tabla.sql`:
| Columna | Tipo SQL | Null |
|---|---|---|
| `Id` | int IDENTITY | NOT NULL (PK) |
| `TipoContenido` | TINYINT | NOT NULL (1=Termino,2=Articulo,3=PerfilMedico) |
| `ContenidoId` | int | NOT NULL |
| `UsuarioMedicoId` | NVARCHAR(450) | NOT NULL |
| `Comentario` | NVARCHAR(800) | NULL |
| `Estado` | TINYINT | NOT NULL (1=Validado,2=EnRevision,3=Oculto) |
| `CreadoEn` | DATETIME2 | NOT NULL |
| `ActualizadoEn` | DATETIME2 | NULL |
| `ModeradoPorId` | NVARCHAR(450) | NULL |
| `FechaModeracion` | DATETIME2 | NULL |
| `NotaModeracion` | NVARCHAR(500) | NULL |

Índices: **único** `(TipoContenido, ContenidoId, UsuarioMedicoId)`; lookup por `UsuarioMedicoId`; lookup por `(TipoContenido, ContenidoId)`.

> Nota: `ValidacionContenidoProfesional` y `ContenidoCalidad` **no declaran FK a `contenidos`** — el vínculo es lógico por `ContenidoId`. Al borrar un contenido no hay cascada → posibles huérfanos (verificar en vivo si existen).

---

## 4. Fuentes de esquema en disco (`SQL/`) — 22 archivos

| Script | Qué crea/altera |
|---|---|
| `create-contenido-calidad.sql` | crea `ContenidoCalidad` |
| `alter-contenido-calidad-gris.sql` | +columnas GRIS a `ContenidoCalidad` |
| `alter-contenido-calidad-gris-categorias.sql` | +`GrisCategoriasSugeridas/Alerta` |
| `alter-aspnetusers-eliminado.sql` | +`Eliminado` BIT a `AspNetUsers` |
| `create-sendgrid-event-log.sql` | crea `SendGridEventLog` (+5 índices) |
| `short-urls-F1.sql` | crea `ShortUrls` + `ShortUrlClicks` |
| `setup-comunidad-user.sql` | inserta usuario sistema "Comunidad EIIBD" |
| `repair-badge-verificado-sync.sql` | reparación de badges (data) |
| `export-contenidos-soporte-tecnico.sql` | **SELECT-only** export (revela esquema `contenidos`) |
| `validacion-contenido/01-crear-tabla.sql` | crea `ValidacionesContenidoProfesional` |
| `validacion-contenido/02-migrar-datos.sql` | migra data de `GlossaryValidation` |
| `Migrations/2026-06-02_FixNINA-AIRequestLogs...` | crea `AIRequestLogs` (log Q&A IA del Web, FK→`Preguntas`) |
| `Migrations/2026-06-02_ValidacionRespuestaProfesional-Badge.sql` | crea `ValidacionRespuestaProfesional` |
| `Migrations/2026-06-01_NormalizarSlugsCategorias.sql` | data fix slugs |
| `Migrations/2026-06-02_BackfillAspNetUserId.sql` | data fix médicos |
| `Glossary/01_Create_GlossaryTables.sql` (+ install/reset variantes) | crea `GlossaryTerm` + `GlossaryTermMedicalLink` |

> ⚠️ `GlossaryValidation` **no tiene CREATE en `SQL/`** — se creó por otra vía (migración EF antigua o DDL manual). No es reconstruible 100% desde disco.

---

## 5. Candidatas a huérfanas / staging 🔴 [PENDIENTE — requiere conexión db_datareader]

Las candidatas del crawler ya están identificadas por código: **`ScrapedPage`, `SourceSite`, `Article` y familia `Article*`, `ScrapingJob*`**. En vivo hay que confirmar:
- ¿Existen físicamente? (creadas por SQL directo, sin migración).
- ¿`ScrapedPage` tiene filas? (¿el crawler llegó a correr en prod?).
- ¿`Article*` están vacías (`COUNT(*)=0`), confirmando que son costuras sin usar?
- ¿Hay tablas legacy no mapeadas por ningún proyecto? (solo `sys.tables` en vivo lo dirá).
- ¿Huérfanos en `ContenidoCalidad`/`ValidacionesContenidoProfesional` cuyo `ContenidoId` ya no existe en `contenidos`?

---

## 6. Consultas de solo lectura propuestas para la pasada en vivo

Todas metadata/conteo, sin `SELECT *`, sin DML/DDL, sin transacciones:

```sql
-- Inventario + nº columnas
SELECT t.name, (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id=t.object_id) AS Cols
FROM sys.tables t ORDER BY t.name;

-- Conteo por tabla (ejecutar por tabla)
SELECT COUNT(*) FROM dbo.ScrapedPage;      -- etc.

-- Esquema de una tabla (nombres/tipos/nullability, SIN datos)
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ScrapedPage' ORDER BY ORDINAL_POSITION;

-- FKs e índices
SELECT * FROM sys.foreign_keys;            -- solo metadatos de esquema
SELECT i.name, i.type_desc, OBJECT_NAME(i.object_id) FROM sys.indexes i WHERE i.object_id=OBJECT_ID('dbo.ScrapedPage');

-- Huérfanos (conteo, no filas)
SELECT COUNT(*) FROM dbo.ContenidoCalidad cc
  WHERE NOT EXISTS (SELECT 1 FROM dbo.contenidos c WHERE c.Id=cc.ContenidoId);
```

**Prohibido:** `INSERT/UPDATE/DELETE/DROP/ALTER/TRUNCATE/MERGE`, cualquier DDL/DML, `BEGIN TRAN`, `SELECT *` sobre tablas con datos de pacientes (`AspNetUsers`, `Perfil`, laboratorios, síntomas, tratamientos).

---
*Parte B en estado "derivada de código". Para completar la verificación en vivo, entregar cadena `db_datareader` de solo lectura (nunca `sa`).*
