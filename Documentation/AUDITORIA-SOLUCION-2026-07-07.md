# AUDITORÍA DE SOLUCIÓN — eiibd26

**Fecha:** 2026-07-07
**Alcance:** Solo lectura y diagnóstico. No se modificó código, datos, ni se movió ningún archivo. No se ejecutaron migraciones. No se conectó a la base de datos para esta parte (se derivó del código en disco).
**Propósito:** Insumo para diseñar un futuro módulo de curaduría de contenido.

> ⚠️ Durante la auditoría se detectó una **exposición crítica de secretos** en `appsettings.Production.json`. Ver [§6 Seguridad](#6-hallazgo-crítico-de-seguridad). No es objeto de esta auditoría corregirlo, pero se documenta por su severidad.

---

## 1. Proyectos de la solución

La solución `eiibd.sln` contiene **3 proyectos totalmente independientes**. **Ninguno referencia a otro** (cero `<ProjectReference>` en los tres `.csproj`). El único acoplamiento es implícito: **comparten la misma base de datos SQL Server `eiibd26`**.

| Proyecto | Tipo (SDK) | Target framework | Propósito |
|---|---|---|---|
| **eiibd26** | Web (`Microsoft.NET.Sdk.Web`) — ASP.NET Core Razor Pages + MVC | `net8.0` | Aplicación web principal (~1000 usuarios) |
| **NINA-WorkerService** | Worker (`Microsoft.NET.Sdk.Worker`) | `net10.0` | **Web scraper / crawler** en background (HtmlAgilityPack) |
| **Conectar3eros** | Desktop WinExe (`UseWindowsForms=true`) | `net10.0-windows` | Utilidad de escritorio: exporta audiencia de Mailchimp a CSV |

### Grafo de dependencias

```
eiibd26 (Web)  ──┐
NINA-WorkerService ─┼──▶  [ SQL Server: base eiibd26 ]   (acoplamiento solo por DB compartida)
Conectar3eros ─────┘        └ Conectar3eros NO toca la DB (solo Mailchimp API → CSV)
```

- **Sin referencias entre proyectos.** Cada uno compila y se despliega por separado.
- **Frameworks mezclados:** el Web sigue en `net8.0`; el Worker y Conectar3eros ya en `net10.0`. Esto implica que el Worker usa EF Core **10.0.0** mientras el Web usa EF Core **8.0.21** contra la misma base — a considerar si el módulo de curaduría vive en el Web (EF 8) y consume tablas creadas por el Worker (EF 10).

### Paquetes NuGet notables (revelan el propósito)

- **eiibd26 (Web):** Hangfire 1.8.23 (colas `default` y `ai`), EF Core SqlServer 8.0.21 + Identity, SendGrid 9.29.3, Twilio 7.13.8, WebPush-NetCore, QuestPDF, SixLabors.ImageSharp, Markdig, LigerShark.WebOptimizer, Razor RuntimeCompilation.
- **NINA-WorkerService:** **HtmlAgilityPack 1.12.4** (parseo HTML → scraper), EF Core 10.0.0 + SqlServer, Extensions.Hosting. **No hay ningún paquete de IA / Anthropic / OpenAI.**
- **Conectar3eros:** solo `MailChimp.Net.V3 5.8.2` (aunque el código llama la API REST de Mailchimp por `HttpClient` directo, ignorando el SDK).

---

## 2. NINA-WorkerService — contrato de salida (SOLO LECTURA)

> Proyecto marcado como **"NO TOCAR"** en `CLAUDE.md`. Aquí solo se documenta su contrato, no se modificó nada.
> DbContext propio: `Data/Eiibd26Context.cs` (distinto del `ApplicationDbContext` del Web).

### 2.1 ¿Qué hace y con qué frecuencia?

- Un único servicio `ScrapingWorker : BackgroundService` (registrado en `Program.cs:15`), con toda la lógica en `Worker.cs:31` (`ExecuteAsync`).
- Es un **crawl BFS de una sola pasada**: corre **una vez al arrancar el proceso y termina** (log final `"ScrapingWorker finalizado (ejecución única)."`, `Worker.cs:217`). **No hay Timer, ni Hangfire, ni bucle recurrente.** La "frecuencia" es la que imponga quien reinicie el proceso.
- Politeness: `Task.Delay(1s)` fijo entre cada request (`Worker.cs:207`), timeout HTTP 30s, User-Agent de Chrome falseado (`Worker.cs:79-91`).

### 2.2 ¿De dónde saca las URLs?

- **Semilla única hardcodeada:** `https://funeiico.com/` (`Worker.cs:39-40`). No lee de tabla de config, ni appsettings, ni sitemap.
- Allow-list de host hardcodeada: `funeiico.com` (`Worker.cs:20-23`).
- Descubre enlaces siguiendo `<a href>` de cada página (`Services/HtmlLinkExtractor.cs`).
- Topes: `maxDepth = 10`, `maxPages = 3000` (`Worker.cs:41-42`).
- ⚠️ **Inconsistencia a notar:** el nombre amigable del `SourceSite` se inserta como literal `"crohns colitis foundation"` aunque la URL es `funeiico.com` (`Worker.cs:47-61`).

### 2.3 ¿Qué produce y dónde? (contrato real que consumirá la curaduría)

En tiempo de ejecución **solo escribe 2 tablas**: `SourceSite` (una fila padre) y **`ScrapedPage`** (la salida real).

**`ScrapedPage`** — tabla primaria de salida:

| Columna | Tipo CLR | Tipo SQL | Null | Notas |
|---|---|---|---|---|
| `ScrapedPageId` | int (PK, identity) | int | NOT NULL | |
| `SourceSiteId` | int (FK→SourceSite) | int | NOT NULL | |
| `Url` | string | nvarchar(max) | NOT NULL | |
| `TitleRaw` | string? | nvarchar(max) | NULL | **siempre NULL hoy** ("luego extraeremos `<title>`") |
| `ContentRaw` | string | nvarchar(max) | NOT NULL | **HTML crudo** — el contenido real |
| `ContentText` | string? | nvarchar(max) | NULL | **siempre NULL hoy** (texto limpio pendiente) |
| `Language` | string | nvarchar(max) | NOT NULL | hardcodeado `"es"` |
| `PublishedAt` | DateTime? | datetime2 | NULL | nunca lo setea el Worker |
| `ScrapedAt` | DateTime | datetime2 | NOT NULL | |
| `HashContent` | byte[]? | varbinary(max) | NULL | SHA-256 (32 bytes) del HTML |
| `Status` | string | nvarchar(max) | NOT NULL | `"OK"` / `"ERROR"` |
| `ErrorMessage` | string? | nvarchar(max) | NULL | |

**`SourceSite`** — padre: `SourceSiteId` (PK), `Name`, `BaseUrl`, `Description?`, `IsActive`, `CreatedAt`, `UpdatedAt?`.

> **Resumen del contrato:** hoy el Worker entrega **HTML crudo en `ScrapedPage.ContentRaw`**, deduplicado/versionado por URL vía SHA-256, con `Status ∈ {OK, ERROR}` y `TitleRaw`/`ContentText`/`PublishedAt` en NULL.

### 2.4 ¿Usa IA?

**No, definitivamente.** No hay paquetes ni `using` de Anthropic/OpenAI/Azure.AI; la única red es `HttpClient.GetStringAsync` para bajar HTML (`Worker.cs:112`). **Solo extrae y guarda HTML crudo.** No resume, no traduce, no clasifica, no embebe.

> Nota de nomenclatura: la "NINA" del Worker (scraper) es distinta de la "NINA" del Web (`Services/AI/NinaModelRouterService.cs`, router Anthropic para Q&A vía Hangfire). Comparten nombre, no código.

### 2.5 ¿Cómo marca lo procesado vs. lo nuevo?

- **No hay flag `Procesado`.** La detección de cambios es por **hash de contenido + versionado por URL**:
  1. Antes de bajar, carga la fila más reciente de esa URL (`Worker.cs:102-105`).
  2. Calcula `SHA256(html)`.
  3. Si el hash coincide con el anterior → **no inserta fila nueva**; solo actualiza `ScrapedAt`/`Status`.
  4. Si es URL nueva o cambió el hash → **inserta una fila versionada nueva**. La misma URL acumula múltiples filas; "la última" = mayor `ScrapedAt`.
- **Punto de handoff para la curaduría:** "lo nuevo por procesar" = filas de `ScrapedPage` sin `Article` asociado (ver §2.6).

### 2.6 Tablas definidas pero NUNCA pobladas (las costuras del módulo)

El modelo del Worker define tablas río abajo que **existen en el esquema pero ningún código las escribe** hoy:

- **`Article`** (1:1 opcional con `ScrapedPage` vía `Article.ScrapedPageId`): `ArticleId`, `ScrapedPageId?`, `NormalizedTitle`, `NormalizedContent`, `Language`, `MainTopic?`, `CreatedAt`, `UpdatedAt?`, `IsActive`.
- **`ArticleTranslation`**: traducciones (`SourceLanguage`, `TargetLanguage`, `TranslatedTitle/Content`, `TranslationProvider?`, `QualityScore` decimal(4,3)).
- **`ArticleSummary`**: resúmenes (`SummaryText`, `SummaryType`, `ModelUsed?`, `Version?`).
- **`ArticleBoost`**: `BoostScore` decimal(6,3), `Reason?`, `ValidUntil?`, `AlgorithmVersion?`.
- **`ArticleSimilarity`**: PK compuesta `(ArticleId1, ArticleId2)`, `SimilarityScore` decimal(5,4), `Method?`.
- **`ScrapingJob`** / **`ScrapingJobLog`**: cabecera de corrida + log por página (tampoco se escriben).

> **Implicación de diseño:** el módulo de curaduría es exactamente lo que debe llenar estas costuras — el flujo natural es **consumir `ScrapedPage` → producir `Article`** (vía el vínculo 1:1 hoy sin usar), y opcionalmente poblar resumen/traducción/similitud con IA.

### 2.7 Otras notas del crawler

- Dedup en corrida vía `HashSet<string>` de URLs normalizadas (`NormalizeUrl`: fuerza https, quita fragmento, recorta `/` final).
- **robots.txt: NO se maneja en absoluto** — y falsea User-Agent de navegador. A flaggear si el crawler se amplía.
- Sin concurrencia (BFS secuencial), sin throttling adaptativo.

---

## 3. Módulo de Contenidos existente (eiibd26.Web)

DbContext: `Data/ApplicationDbContext.cs` (`IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`). **Sin migraciones EF** — esquema por SQL directo.

### 3.1 Entidad principal `Contenido` → tabla `dbo.contenidos`

Archivo `Models/Contenido.cs`; mapeo en `ApplicationDbContext.cs:240-254`.

| Propiedad | Tipo | Null | Notas |
|---|---|---|---|
| `Id` | int | no | PK |
| `IdTipo` | int? | sí | tipo de contenido |
| `ContenidoTitulo` | string? | sí | título |
| `ContenidoTextoC` | string? | sí | texto corto / resumen |
| `ContenidoTextoL` | string? | sí | **cuerpo (HTML largo)** |
| `ContenidoTituloSlug` | string? | sí | **slug** |
| `URLImagenPrincipal` | string? | sí | imagen |
| `EstadoPublicacion` | int? | sí | **estado de publicación** (ver §3.5) |
| `ContenidoFechaInicio` / `...Fin` | DateTime? | sí | ventana de publicación |
| `IdAutor` | Guid | no | FK→`Perfil` (autor) |
| `Autor` | string? | sí | nombre desnormalizado |
| `IdEmpresa` | int? | sí | |
| `PaisClave` | string? | sí | |
| `UsuarioCreacion` / `UsuarioModificacion` | Guid / Guid? | | auditoría |
| `FechaCreado` / `FechaModificado` | DateTime / DateTime? | | auditoría |
| `Eliminado` | bool | no | **soft-delete** (query filter global `!c.Eliminado`) |
| `IdUser` | Guid? | sí | FK→`ApplicationUser` |

Config: PK `Id`; índices en `IdAutor`, `IdUser`, fechas; **query filter global `!Eliminado`**; relaciones hijas en `Cascade`.

### 3.2 Relaciones

- **Categorías (M:N):** catálogo `ContenidoCategoria` → `dbo.contenidosCategorias` (PK **`Sequence`**, auto-jerarquía `CategoriaPadre`); puente `ContenidoCategoriaRelacion` → `dbo.contenidosCategoriasRelacion` (FKs `IdContenido`→`contenidos.Id`, `IdCategoria`→`contenidosCategorias.Sequence`, con flag **`EsPrincipal`**).
- **Autor/Usuario:** doble vínculo — `IdAutor` (Guid, NOT NULL)→`Perfil`, e `IdUser` (Guid?)→`ApplicationUser`.
- **Tags:** ⚠️ **No existe sistema de tags para Contenido.** `Etiqueta`/`PreguntaEtiqueta` pertenece al módulo Preguntas/Q&A, no a Contenidos.
- **Relaciones clínicas** (patrón: PK `Id`, índice único, query filter `!Borrado`): `ContenidoCondicion`, `ContenidoSintoma`, `ContenidoTratamiento` (tablas `contenido*Relacion`).
- **Autorreferencia:** `ContenidoRelacionado` → `dbo.contenidosRelacionados`.
- Sub-respuestas: `ContenidoRespuesta` → `dbo.contenidosRespuestas`.
- Relaciones con Q&A: `ContenidoPreguntaRelacion`, `ContenidoRespuestaRelacion`.
- Calificaciones: `ArticleRating` → `dbo.ArticleRatings` (FK `ArticleId`→`Contenido.Id`).

### 3.3 Validación médica — `ValidacionContenidoProfesional` → `dbo.ValidacionesContenidoProfesional`

Archivo `Models/Validacion/ValidacionContenidoProfesional.cs`; config `ApplicationDbContext.cs:449-460`. **Diseño polimórfico** — valida distintos tipos de entidad, no solo Contenido.

| Columna | Tipo | Null | Notas |
|---|---|---|---|
| `Id` | int | no | PK |
| `TipoContenido` | enum `TipoContenidoValidado` | no | `Termino=1`, `Articulo=2`, `PerfilMedico=3` |
| `ContenidoId` | int | no | id de la entidad según tipo — **no es FK real** |
| `UsuarioMedicoId` | string(450) | no | id del médico validador (AspNetUsers.Id) |
| `Comentario` | string?(800) | sí | |
| `Estado` | enum `EstadoValidacion` | no | `Validado=1`, `EnRevision=2`, `Oculto=3` |
| `CreadoEn` / `ActualizadoEn` | DateTime / DateTime? | | |
| `ModeradoPorId` / `FechaModeracion` / `NotaModeracion` | | sí | moderación |

- Índice **único** `(TipoContenido, ContenidoId, UsuarioMedicoId)` — un médico valida una entidad una sola vez.
- Vínculo a `Contenido`: **lógico** (`TipoContenido==Articulo && ContenidoId==Contenido.Id`), sin FK EF.
- Hermana: `ValidacionRespuestaProfesional` → `dbo.ValidacionesRespuestaProfesional` (equivalente para respuestas Q&A).
- Servicio: `Services/Validacion/ValidacionContenidoService.cs`.

### 3.4 Semáforo de calidad "GRIS" — `ContenidoCalidad` → `dbo.ContenidoCalidad`

Archivo `Models/Calidad/ContenidoCalidad.cs`. Una fila por contenido (cache de análisis). Vínculo lógico por `ContenidoId` (sin FK EF).

| Columna | Tipo | Null | Notas |
|---|---|---|---|
| `Id` | int | no | PK |
| `ContenidoId` | int | no | vínculo lógico a `contenidos.Id` (UNIQUE) |
| `NivelSemaforo` | byte | no | `0=Critico 🔴`, `1=Mejorable 🟡`, `2=Ok 🟢` |
| `Senales` | string? (JSON) | sí | `[{Codigo, Descripcion, Gravedad}]` |
| `DuplicadoDeIds` | string? (JSON) | sí | ids de contenidos similares |
| `FechaAnalisis` | DateTime | no | |
| `GrisEvaluado` | bool | no | si corrió la evaluación editorial IA |
| `GrisPuntajeGlobal` | byte? | sí | 0–100 |
| `GrisResultado` | string? (JSON) | sí | 7 aspectos `[{nombre, puntaje, observacion}]` |
| `GrisSugerencias` | string? (JSON) | sí | |
| `GrisCategoriasSugeridas` | string? (JSON) | sí | `[{categoriaId, nombre, razon}]` |
| `GrisCategoriasAlerta` | string? (JSON) | sí | categorías actuales que no corresponden |
| `GrisFechaEvaluacion` | DateTime? | sí | |

**Dos capas de cómputo:**
1. **Mecánica** (`Services/Calidad/ContenidoCalidadService.cs`): señales `SIN_CUERPO`, `DUPLICADO` (Críticas), `SIN_IMAGEN`, `SIN_RESUMEN`, `CUERPO_CORTO`, `SIN_CATEGORIA`, `SIN_SLUG`, `BORRADOR_VIEJO` (Mejorables). Nivel = Crítico si hay alguna crítica, si no Mejorable, si no Ok.
2. **Editorial IA "GRIS"** (`Services/Calidad/GrisEvaluadorService.cs`): rúbrica 7 aspectos, puntaje 0–100, sugerencias, categorías sugeridas/alerta. Modelo configurado en `Gris:Model` = `claude-sonnet-4-6`.

> El GRIS ya trae **ganchos de curaduría editorial con IA** (categorías sugeridas/alerta) — base natural sobre la que apoyar el nuevo módulo.

### 3.5 Estado del contenido

**No hay enum dedicado.** El estado se fragmenta en dos campos de `Contenido`:
1. **`Eliminado`** (bool) — soft-delete (query filter global).
2. **`EstadoPublicacion`** (int?, valores mágicos, sin enum): `0`=Borrador, `1`=Publicado, `2`=Publicado+Inicio, `3`=Publicado+Popular; `null`≈borrador. Público filtra `EstadoPublicacion==1`.

No existen estados "moderado" ni "cerrado" en `Contenido` mismo; la moderación se expresa vía `EstadoValidacion.Oculto` y vía el semáforo GRIS.

---

## 4. Conectar3eros (SOLO LECTURA)

Utilidad de escritorio WinForms de un solo botón que **exporta miembros de una audiencia de Mailchimp a CSV** (`D:\mailchimp_emails.csv`), llamando la API REST de Mailchimp por `HttpClient` directo. **No toca la base de datos** (sin connection string, sin EF). API key en placeholder en el código (no filtra secreto real).

---

## 5. Reglas del proyecto relevantes (de `CLAUDE.md`)

- **NO tocar** `NINA-WorkerService/` ni `Conectar3eros/`.
- **NO migraciones EF Core** — esquema por SQL directo idempotente en `SQL/`.
- Razor Pages: parámetros `OnPost*Async` siempre `string?`/`DateTime?`.
- ~1000 usuarios en producción; estabilidad sobre velocidad.

---

## 6. Hallazgo crítico de seguridad

`eiibd26/appsettings.Production.json` **contiene secretos de producción reales en texto plano**, contradiciendo lo que afirma `SECRETS.md` (que dice que el archivo solo tiene placeholders `""`). Valores redactados aquí a propósito:

| Secreto | Estado |
|---|---|
| Cadena de conexión con cuenta **`sa`** + contraseña → `132.148.74.136\ybridio`, DB `eiibd26` | **EXPUESTO** |
| `GoogleMaps:ApiKey` | EXPUESTO |
| `SendGrid:ApiKey` | EXPUESTO |
| `Twilio:AccountSid` + `AuthToken` | EXPUESTO |
| `VapidKeys:PrivateKey` | EXPUESTO |
| `AiAnswer:AnthropicApiKey` (`sk-ant-...`) | EXPUESTO |

**Recomendación (fuera del alcance de esta auditoría, pero urgente):**
1. Tratar las 6 credenciales como **comprometidas** y **rotarlas todas**.
2. Sacar `appsettings.Production.json` del árbol de trabajo y del historial (procedimiento BFG/`git filter-repo` ya documentado en `SECRETS.md`).
3. Pasar producción a variables de entorno (`ConnectionStrings__DefaultConnection`, etc.), como el propio `SECRETS.md` describe.
4. Crear un usuario SQL **`db_datareader`** de solo lectura y otro de lectura/escritura con permisos mínimos; **rotar `sa`**. Esto habilita además una auditoría de BD segura (Parte B) sin usar `sa`.

---

## 7. Conclusiones para el módulo de curaduría

1. **Contrato de entrada = `ScrapedPage`** (HTML crudo, versionado por hash). El módulo consume filas sin `Article` asociado.
2. **Salida natural = poblar `Article`** (y opcionalmente `ArticleSummary`/`Translation`/`Similarity`) — las costuras ya existen en el esquema del Worker, hoy vacías.
3. **El Web ya tiene la maquinaria editorial:** `ContenidoCalidad`/GRIS (IA Sonnet), `ValidacionContenidoProfesional` (validación médica polimórfica), estados por `EstadoPublicacion`. La curaduría puede apoyarse en estas piezas en vez de reinventarlas.
4. **Fricciones a resolver en diseño:** (a) el crawler produce con EF Core 10 y el Web consume con EF Core 8; (b) no hay tags de contenido; (c) el "estado" está fragmentado sin enum; (d) validación y calidad están desacopladas de `Contenido` por diseño (sin FK → riesgo de huérfanos al borrar).

---
*Auditoría de solo lectura. No se modificó código ni datos. Pendiente: verificación en vivo de la BD (Parte B) con credenciales `db_datareader`.*
