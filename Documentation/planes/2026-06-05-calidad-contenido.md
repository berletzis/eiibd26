# Calidad de Contenido — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Vista admin nueva que analiza todos los contenidos con semáforo 🔴🟡🟢 según señales de calidad (cuerpo, imagen, duplicados, categoría, slug, etc.), bajo demanda.

**Architecture:** Servicio `ContenidoCalidadService` independiente en `Services/Calidad/`, reutiliza `ISimilarQuestionDetector.CalcularSimilitud()` para detección de duplicados O(n²) con mitigaciones de performance. Razor Page nueva en el área admin. Sin schema nuevo — todo calculado sobre datos existentes.

**Tech Stack:** ASP.NET Core 8 · Razor Pages · EF Core 8 · Bootstrap 5 · `ISimilarQuestionDetector` (ya registrado en DI como Scoped)

---

## File Map

| Acción | Archivo |
|--------|---------|
| Crear | `eiibd26/Services/Calidad/ContenidoCalidadDtos.cs` |
| Crear | `eiibd26/Services/Calidad/IContenidoCalidadService.cs` |
| Crear | `eiibd26/Services/Calidad/ContenidoCalidadService.cs` |
| Crear | `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml.cs` |
| Crear | `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml` |
| Modificar | `eiibd26/Program.cs` — registrar DI |
| Modificar | `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Contenidos.cshtml` — agregar link |

---

## Task 1: DTOs y Enums

**Files:**
- Crear: `eiibd26/Services/Calidad/ContenidoCalidadDtos.cs`

- [ ] **Step 1: Crear el archivo de DTOs**

```csharp
// eiibd26/Services/Calidad/ContenidoCalidadDtos.cs
namespace eiibd26.Services.Calidad
{
    public enum GravedadSenal { Critica, Mejorable }

    public enum NivelSemaforo
    {
        Critico = 0,   // ordena primero
        Mejorable = 1,
        Ok = 2
    }

    public record SenalCalidad(string Codigo, string Descripcion, GravedadSenal Gravedad);

    public class ContenidoCalidadDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public int? EstadoPublicacion { get; set; }
        public DateTime FechaCreado { get; set; }
        public List<SenalCalidad> Senales { get; set; } = new();
        public NivelSemaforo NivelSemaforo { get; set; }
        public List<int> DuplicadoDeIds { get; set; } = new();
    }
}
```

- [ ] **Step 2: Verificar que compila**

```
cd eiibd26
dotnet build --no-restore
```
Esperado: 0 errores.

---

## Task 2: Interface del Servicio

**Files:**
- Crear: `eiibd26/Services/Calidad/IContenidoCalidadService.cs`

- [ ] **Step 1: Crear la interfaz**

```csharp
// eiibd26/Services/Calidad/IContenidoCalidadService.cs
namespace eiibd26.Services.Calidad
{
    /// <summary>
    /// Analiza señales de calidad sobre Contenidos (extensible a otros tipos).
    /// Análisis bajo demanda — no llamar en cada carga de página.
    /// </summary>
    public interface IContenidoCalidadService
    {
        /// <summary>
        /// Evalúa todos los contenidos no eliminados y devuelve la lista
        /// con señales de calidad y nivel de semáforo por cada uno.
        /// O(n²) para duplicados — solo invocar bajo demanda.
        /// </summary>
        Task<List<ContenidoCalidadDto>> AnalizarTodosAsync();
    }
}
```

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores.

---

## Task 3: Implementación del Servicio

**Files:**
- Crear: `eiibd26/Services/Calidad/ContenidoCalidadService.cs`

- [ ] **Step 1: Crear la implementación**

```csharp
// eiibd26/Services/Calidad/ContenidoCalidadService.cs
using eiibd26.Data;
using eiibd26.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace eiibd26.Services.Calidad
{
    public class ContenidoCalidadService : IContenidoCalidadService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISimilarQuestionDetector _detector;
        private readonly ILogger<ContenidoCalidadService> _logger;

        public ContenidoCalidadService(
            ApplicationDbContext db,
            ISimilarQuestionDetector detector,
            ILogger<ContenidoCalidadService> logger)
        {
            _db = db;
            _detector = detector;
            _logger = logger;
        }

        public async Task<List<ContenidoCalidadDto>> AnalizarTodosAsync()
        {
            _logger.LogInformation("[CalidadContenido] Iniciando análisis de calidad");

            var contenidos = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado)
                .Include(c => c.CategoriasRelacion.Where(r => !r.Borrado))
                .OrderBy(c => c.Id)
                .ToListAsync();

            _logger.LogInformation("[CalidadContenido] {Count} contenidos a evaluar", contenidos.Count);

            // Pre-calcular textos para comparación de similitud
            // Truncar a 600 chars (ISimilarQuestionDetector ya trunca Levenshtein a 300 internamente)
            var textos = contenidos.Select(c => new
            {
                c.Id,
                c.IdTipo,
                Texto = TruncarTexto($"{c.ContenidoTitulo} {StripHtml(c.ContenidoTextoL)}", 600)
            }).ToList();

            // Detectar duplicados O(n²) — comparar dentro del mismo IdTipo si está definido
            var duplicados = contenidos.ToDictionary(c => c.Id, _ => new List<int>());

            for (int i = 0; i < textos.Count; i++)
            {
                for (int j = i + 1; j < textos.Count; j++)
                {
                    // Mitigación de performance: solo comparar dentro del mismo IdTipo
                    if (textos[i].IdTipo.HasValue && textos[j].IdTipo.HasValue
                        && textos[i].IdTipo != textos[j].IdTipo)
                        continue;

                    var sim = _detector.CalcularSimilitud(textos[i].Texto, textos[j].Texto);
                    if (sim >= 0.80)
                    {
                        duplicados[textos[i].Id].Add(textos[j].Id);
                        duplicados[textos[j].Id].Add(textos[i].Id);
                    }
                }
            }

            var resultados = new List<ContenidoCalidadDto>();

            foreach (var c in contenidos)
            {
                var senales = new List<SenalCalidad>();
                var palabras = ContarPalabras(c.ContenidoTextoL);

                // --- Señales CRÍTICAS ---
                if (palabras < 50)
                    senales.Add(new SenalCalidad("SIN_CUERPO",
                        palabras == 0 ? "Sin cuerpo" : $"Cuerpo muy corto ({palabras} palabras, mínimo 50)",
                        GravedadSenal.Critica));

                if (duplicados[c.Id].Count > 0)
                    senales.Add(new SenalCalidad("DUPLICADO",
                        $"Similar a {duplicados[c.Id].Count} contenido(s)",
                        GravedadSenal.Critica));

                // --- Señales MEJORABLES ---
                if (string.IsNullOrWhiteSpace(c.URLImagenPrincipal))
                    senales.Add(new SenalCalidad("SIN_IMAGEN", "Sin imagen principal", GravedadSenal.Mejorable));

                if (string.IsNullOrWhiteSpace(c.ContenidoTextoC))
                    senales.Add(new SenalCalidad("SIN_RESUMEN", "Sin resumen/descripción", GravedadSenal.Mejorable));

                if (palabras >= 50 && palabras <= 100)
                    senales.Add(new SenalCalidad("CUERPO_CORTO",
                        $"Cuerpo corto ({palabras} palabras)",
                        GravedadSenal.Mejorable));

                if (!c.CategoriasRelacion.Any(r => !r.Borrado))
                    senales.Add(new SenalCalidad("SIN_CATEGORIA", "Sin categoría asignada", GravedadSenal.Mejorable));

                if (string.IsNullOrWhiteSpace(c.ContenidoTituloSlug))
                    senales.Add(new SenalCalidad("SIN_SLUG", "Sin slug", GravedadSenal.Mejorable));

                if (c.EstadoPublicacion == 0 && c.FechaCreado < DateTime.UtcNow.AddDays(-30))
                    senales.Add(new SenalCalidad("BORRADOR_VIEJO",
                        $"Borrador sin publicar hace {(DateTime.UtcNow - c.FechaCreado).Days} días",
                        GravedadSenal.Mejorable));

                // Nivel del semáforo
                NivelSemaforo nivel;
                if (senales.Any(s => s.Gravedad == GravedadSenal.Critica))
                    nivel = NivelSemaforo.Critico;
                else if (senales.Any())
                    nivel = NivelSemaforo.Mejorable;
                else
                    nivel = NivelSemaforo.Ok;

                resultados.Add(new ContenidoCalidadDto
                {
                    Id = c.Id,
                    Titulo = string.IsNullOrWhiteSpace(c.ContenidoTitulo) ? "(sin título)" : c.ContenidoTitulo,
                    Slug = c.ContenidoTituloSlug,
                    EstadoPublicacion = c.EstadoPublicacion,
                    FechaCreado = c.FechaCreado,
                    Senales = senales,
                    NivelSemaforo = nivel,
                    DuplicadoDeIds = duplicados[c.Id]
                });
            }

            _logger.LogInformation(
                "[CalidadContenido] Análisis completo — 🔴 {Criticos} críticos / 🟡 {Mejorables} mejorables / 🟢 {Ok} ok",
                resultados.Count(r => r.NivelSemaforo == NivelSemaforo.Critico),
                resultados.Count(r => r.NivelSemaforo == NivelSemaforo.Mejorable),
                resultados.Count(r => r.NivelSemaforo == NivelSemaforo.Ok));

            return resultados.OrderBy(r => r.NivelSemaforo).ToList();
        }

        private static int ContarPalabras(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;
            var limpio = StripHtml(texto);
            return limpio.Split(new[] { ' ', '\n', '\r', '\t' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static string StripHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            return Regex.Replace(html, "<[^>]+>", " ", RegexOptions.Compiled)
                        .Trim();
        }

        private static string TruncarTexto(string texto, int maxChars)
            => texto.Length > maxChars ? texto[..maxChars] : texto;
    }
}
```

**Nota de escalabilidad**: Para volúmenes grandes (>500 contenidos), el O(n²) de similitud puede tardar. En ese caso, mover el análisis a un Hangfire job en background y mostrar el resultado de la última ejecución. Actualmente bajo demanda es suficiente para ~1000 usuarios.

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores.

---

## Task 4: Registro DI

**Files:**
- Modificar: `eiibd26/Program.cs` — agregar una línea cerca de los otros `AddScoped` de servicios

- [ ] **Step 1: Agregar registro**

En `Program.cs`, después de la línea con `IValidacionRespuestaService` (≈línea 312), agregar:

```csharp
builder.Services.AddScoped<eiibd26.Services.Calidad.IContenidoCalidadService, eiibd26.Services.Calidad.ContenidoCalidadService>();
```

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores.

---

## Task 5: Page Model (backend)

**Files:**
- Crear: `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml.cs`

- [ ] **Step 1: Crear el page model**

```csharp
// eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml.cs
using eiibd26.Services.Calidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    [Authorize(Roles = "Administrador")]
    public class CalidadModel : PageModel
    {
        private readonly IContenidoCalidadService _calidad;

        public CalidadModel(IContenidoCalidadService calidad)
            => _calidad = calidad;

        public List<ContenidoCalidadDto>? Resultados { get; private set; }
        public bool Analizado { get; private set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAnalizarAsync()
        {
            Resultados = await _calidad.AnalizarTodosAsync();
            Analizado = true;
            return Page();
        }
    }
}
```

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores.

---

## Task 6: Vista Razor (frontend)

**Files:**
- Crear: `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml`

- [ ] **Step 1: Crear la vista**

```cshtml
@page
@model eiibd26.Areas.Identity.Pages.Admin.Contenidos.CalidadModel
@using eiibd26.Services.Calidad

@{
    ViewData["Title"] = "Calidad de Contenido";

    int totalCriticos = Model.Resultados?.Count(r => r.NivelSemaforo == NivelSemaforo.Critico) ?? 0;
    int totalMejorables = Model.Resultados?.Count(r => r.NivelSemaforo == NivelSemaforo.Mejorable) ?? 0;
    int totalOk = Model.Resultados?.Count(r => r.NivelSemaforo == NivelSemaforo.Ok) ?? 0;
}

@section Styles {
    <style>
        body { background: #f5f7fb; }

        .block-admin-uniform {
            background: #fff;
            border-radius: 14px;
            box-shadow: 0 2px 10px rgba(40,60,120,0.08);
            padding: 30px 32px 18px 32px;
            margin: 0 0 24px 0;
            border: 1px solid #dde6f1;
        }

        .semaforo-critico  { color: #dc3545; font-size: 1.3rem; }
        .semaforo-mejorable { color: #ffc107; font-size: 1.3rem; }
        .semaforo-ok       { color: #198754; font-size: 1.3rem; }

        .counter-card {
            border-radius: 12px;
            padding: 18px 28px;
            text-align: center;
            min-width: 140px;
        }

        .counter-card.critico   { background: #fff5f5; border: 2px solid #f5c2c7; }
        .counter-card.mejorable { background: #fffbf0; border: 2px solid #ffd966; }
        .counter-card.ok        { background: #f0fff4; border: 2px solid #a8d5b5; }

        .counter-card .numero  { font-size: 2.2rem; font-weight: 700; line-height: 1; }
        .counter-card .etiqueta { font-size: 0.85rem; color: #6b7280; margin-top: 4px; }

        .senal-badge {
            font-size: 0.72rem;
            padding: 3px 8px;
            border-radius: 20px;
            margin: 2px;
            display: inline-block;
            font-weight: 600;
        }

        .senal-critica  { background: #fde8e8; color: #b91c1c; border: 1px solid #fca5a5; }
        .senal-mejorable { background: #fef9c3; color: #92400e; border: 1px solid #fde68a; }

        .tabla-calidad td { vertical-align: middle; }
        .titulo-contenido { font-weight: 500; font-size: 0.92rem; }
        .dup-link { font-size: 0.78rem; color: #6c757d; }
    </style>
}

<div class="container-fluid px-4 py-4">

    <!-- Encabezado -->
    <div class="block-admin-uniform d-flex justify-content-between align-items-start flex-wrap gap-3">
        <div>
            <h4 class="mb-1 fw-bold">
                <i class="bi bi-bar-chart-line me-2 text-primary"></i>Calidad de Contenido
            </h4>
            <div class="text-muted mb-2">
                Analiza duplicados y señales de calidad de todos los contenidos.
                El análisis corre bajo demanda — puede tardar unos segundos.
            </div>
            <a asp-page="./Contenidos" class="text-decoration-none small">
                <i class="bi bi-arrow-left me-1"></i>Volver a Contenidos
            </a>
        </div>
        <div>
            <form method="post" asp-page-handler="Analizar">
                @Html.AntiForgeryToken()
                <button id="btnAnalizar" type="submit" class="btn btn-primary">
                    <i class="bi bi-search me-1"></i> Analizar contenido
                </button>
            </form>
        </div>
    </div>

    @if (!Model.Analizado)
    {
        <!-- Estado inicial -->
        <div class="block-admin-uniform text-center py-5">
            <i class="bi bi-bar-chart-line" style="font-size:3rem;color:#adb5bd;"></i>
            <div class="mt-3 text-muted">Haz clic en <strong>Analizar contenido</strong> para iniciar el diagnóstico.</div>
        </div>
    }
    else
    {
        <!-- Resumen de contadores -->
        <div class="d-flex gap-3 flex-wrap mb-4">
            <div class="counter-card critico">
                <div class="numero semaforo-critico">🔴 @totalCriticos</div>
                <div class="etiqueta">Críticos</div>
            </div>
            <div class="counter-card mejorable">
                <div class="numero semaforo-mejorable">🟡 @totalMejorables</div>
                <div class="etiqueta">Mejorables</div>
            </div>
            <div class="counter-card ok">
                <div class="numero semaforo-ok">🟢 @totalOk</div>
                <div class="etiqueta">OK</div>
            </div>
            <div class="counter-card" style="border: 2px solid #dee2e6; background:#f8f9fa;">
                <div class="numero" style="font-size:2.2rem;">@Model.Resultados!.Count</div>
                <div class="etiqueta">Total analizados</div>
            </div>
        </div>

        <!-- Filtro rápido -->
        <div class="mb-3 d-flex gap-2 flex-wrap align-items-center">
            <span class="text-muted small fw-semibold me-1">Filtrar:</span>
            <button class="btn btn-sm btn-outline-secondary btn-filtro active" data-nivel="todos">Todos</button>
            <button class="btn btn-sm btn-outline-danger btn-filtro" data-nivel="Critico">🔴 Críticos</button>
            <button class="btn btn-sm btn-outline-warning btn-filtro" data-nivel="Mejorable">🟡 Mejorables</button>
            <button class="btn btn-sm btn-outline-success btn-filtro" data-nivel="Ok">🟢 OK</button>
        </div>

        <!-- Tabla de resultados -->
        <div class="block-admin-uniform p-0 overflow-hidden">
            <table class="table table-hover mb-0 tabla-calidad" id="tablaCalidad">
                <thead class="table-light">
                    <tr>
                        <th style="width:60px;" class="text-center">Estado</th>
                        <th>Título</th>
                        <th style="width:80px;" class="text-center">Pub.</th>
                        <th>Señales detectadas</th>
                        <th style="width:110px;" class="text-center">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in Model.Resultados!)
                    {
                        var nivelClass = item.NivelSemaforo switch
                        {
                            NivelSemaforo.Critico   => "critico",
                            NivelSemaforo.Mejorable => "mejorable",
                            _                       => "ok"
                        };
                        var emoji = item.NivelSemaforo switch
                        {
                            NivelSemaforo.Critico   => "🔴",
                            NivelSemaforo.Mejorable => "🟡",
                            _                       => "🟢"
                        };
                        <tr data-nivel="@item.NivelSemaforo">
                            <td class="text-center semaforo-@nivelClass" title="@item.NivelSemaforo">
                                @emoji
                            </td>
                            <td>
                                <div class="titulo-contenido">@item.Titulo</div>
                                @if (!string.IsNullOrWhiteSpace(item.Slug))
                                {
                                    <div class="text-muted" style="font-size:0.75rem;">/@item.Slug</div>
                                }
                            </td>
                            <td class="text-center">
                                @if (item.EstadoPublicacion == 0)
                                {
                                    <span class="badge bg-secondary">Borrador</span>
                                }
                                else if (item.EstadoPublicacion == 1)
                                {
                                    <span class="badge bg-success">Publicado</span>
                                }
                                else
                                {
                                    <span class="badge bg-light text-muted">@item.EstadoPublicacion</span>
                                }
                            </td>
                            <td>
                                @foreach (var senal in item.Senales)
                                {
                                    var badgeClass = senal.Gravedad == GravedadSenal.Critica
                                        ? "senal-critica" : "senal-mejorable";
                                    <span class="senal-badge @badgeClass" title="@senal.Descripcion">
                                        @senal.Codigo
                                    </span>
                                }
                                @if (item.DuplicadoDeIds.Any())
                                {
                                    <div class="dup-link mt-1">
                                        Similares:
                                        @foreach (var dupId in item.DuplicadoDeIds)
                                        {
                                            <a href="./Detalle?id=@dupId" target="_blank" class="me-1">#@dupId</a>
                                        }
                                    </div>
                                }
                                @if (!item.Senales.Any())
                                {
                                    <span class="text-muted small">Sin problemas detectados</span>
                                }
                            </td>
                            <td class="text-center">
                                <a href="./Detalle?id=@item.Id"
                                   class="btn btn-sm btn-outline-primary"
                                   title="Editar contenido">
                                    <i class="bi bi-pencil-square"></i>
                                </a>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>

@section Scripts {
    <script>
        // Spinner al analizar
        document.getElementById('btnAnalizar')?.addEventListener('click', function () {
            this.disabled = true;
            this.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Analizando...';
        });

        // Filtro por nivel
        document.querySelectorAll('.btn-filtro').forEach(btn => {
            btn.addEventListener('click', function () {
                document.querySelectorAll('.btn-filtro').forEach(b => b.classList.remove('active'));
                this.classList.add('active');

                const nivel = this.dataset.nivel;
                document.querySelectorAll('#tablaCalidad tbody tr').forEach(tr => {
                    tr.style.display = (nivel === 'todos' || tr.dataset.nivel === nivel) ? '' : 'none';
                });
            });
        });
    </script>
}
```

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores.

---

## Task 7: Agregar link en Contenidos.cshtml

**Files:**
- Modificar: `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Contenidos.cshtml` ≈ línea 275

- [ ] **Step 1: Agregar el botón de Calidad**

Buscar la línea que contiene `Actualizar XML de Preguntas` (aprox. línea 275) y agregar después, antes del `</div>` que cierra los controles:

```html
<a asp-page="./Calidad" class="btn btn-outline-warning ms-2">
    <i class="bi bi-bar-chart-line me-1"></i> Calidad de contenido
</a>
```

El bloque de botones al final debe quedar así:

```html
<button id="btnRefreshSitemapContents" type="button" class="btn btn-outline-info ms-3">Actualizar XML de Contenidos</button>
<button id="btnRefreshSitemapPreguntas" type="button" class="btn btn-outline-info ms-2">Actualizar XML de Preguntas</button>
<a asp-page="./Calidad" class="btn btn-outline-warning ms-2">
    <i class="bi bi-bar-chart-line me-1"></i> Calidad de contenido
</a>
<span id="refreshSitemapStatus" class="ms-2 text-muted">&nbsp;</span>
```

- [ ] **Step 2: Build final**

```
dotnet build --no-restore
```
Esperado: 0 errores, 0 warnings nuevos.

---

## Task 8: Verificación en browser

- [ ] **Step 1: Reiniciar la aplicación**

```
dotnet run
```
(o reiniciar el debugger en VS)

- [ ] **Step 2: Entrar como Administrador**

Navegar a `/Identity/Admin/Contenidos/Contenidos`. Verificar que aparece el botón **"Calidad de contenido"**.

- [ ] **Step 3: Abrir la vista de Calidad**

Clic en el botón. Verificar que carga la página en blanco con el botón "Analizar contenido".

- [ ] **Step 4: Correr el análisis**

Clic en "Analizar contenido". El botón debe mostrar spinner. Esperar resultado.

- [ ] **Step 5: Verificar resultados**

Confirmar:
- Los contadores 🔴/🟡/🟢 suman el total de contenidos
- Contenidos sin imagen aparecen 🟡 con badge `SIN_IMAGEN`
- Contenidos sin cuerpo (o < 50 palabras) aparecen 🔴 con badge `SIN_CUERPO`
- Si hay contenidos similares, aparecen 🔴 `DUPLICADO` con links a los IDs similares
- El botón "✏️" de cada fila lleva a `./Detalle?id=X`
- Los filtros 🔴/🟡/🟢 ocultan/muestran filas correctamente

- [ ] **Step 6: Commit**

```bash
git add eiibd26/Services/Calidad/ContenidoCalidadDtos.cs \
        eiibd26/Services/Calidad/IContenidoCalidadService.cs \
        eiibd26/Services/Calidad/ContenidoCalidadService.cs \
        eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml \
        eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml.cs \
        eiibd26/Program.cs \
        eiibd26/Areas/Identity/Pages/Admin/Contenidos/Contenidos.cshtml

git commit -m "feat(contenidos): módulo Calidad de Contenido — duplicados + señales + semáforo"
```

---

## Self-Review

### Spec coverage
| Requisito | Task |
|-----------|------|
| Servicio escalable en `Services/Calidad/` | Task 1–3 |
| DTO `ContenidoCalidadDto` con Senales + NivelSemaforo + DuplicadoDeIds | Task 1 |
| Señal `SIN_CUERPO` (< 50 palabras) | Task 3 |
| Señal `DUPLICADO` (≥ 0.80) reusando `CalcularSimilitud` | Task 3 |
| Señal `SIN_IMAGEN` | Task 3 |
| Señal `SIN_RESUMEN` | Task 3 |
| Señal `CUERPO_CORTO` (50–100 palabras) | Task 3 |
| Señal `SIN_CATEGORIA` (sin relación no borrada) | Task 3 |
| Señal `SIN_SLUG` | Task 3 |
| Señal `BORRADOR_VIEJO` (estado 0 + > 30 días) | Task 3 |
| Solo contenidos `!Eliminado` | Task 3 |
| Nivel: Crítico / Mejorable / Ok | Task 1 + 3 |
| DI registration | Task 4 |
| Razor Page `[Authorize(Roles="Administrador")]` | Task 5 |
| Análisis bajo demanda (botón POST) | Task 5–6 |
| Spinner mientras analiza | Task 6 |
| Resumen contadores 🔴/🟡/🟢 | Task 6 |
| Tabla con semáforo, título link, chips de señales | Task 6 |
| Links a duplicados por ID | Task 6 |
| Filtro por nivel | Task 6 |
| Link directo a `./Detalle?id=` | Task 6 |
| Botón "Calidad de contenido" en grid Contenidos | Task 7 |
| No tocar `SimilarQuestionDetector` (solo consume interfaz) | Task 3 |
| No schema nuevo | ✅ todo calculado en memoria |
| Solo lectura — no modifica contenidos | ✅ |
| Performance: truncar texto + comparar por IdTipo + bajo demanda | Task 3 |

### Consistencia de tipos
- `NivelSemaforo` enum definido en Task 1, usado en Task 3 y 6 con mismo nombre.
- `GravedadSenal` enum definido en Task 1, usado en Task 3 con `GravedadSenal.Critica` y `GravedadSenal.Mejorable`.
- `SenalCalidad` record: `(string Codigo, string Descripcion, GravedadSenal Gravedad)` — consistente en Task 3.
- `ContenidoCalidadDto` propiedades: consistentes entre Task 1 (definición), Task 3 (construcción) y Task 6 (uso en vista).
- `ISimilarQuestionDetector.CalcularSimilitud(string, string)` — firma verificada en el archivo real.
- `ContenidoCategoriaRelacion.Borrado` — campo verificado en el modelo real (no `Eliminado`).
