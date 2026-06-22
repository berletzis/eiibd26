# Calidad de Contenido — Batches + Progreso + Menú

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar el análisis monolítico de calidad por batches de 10 con barra de progreso JS y mover el acceso al menú lateral admin.

**Architecture:** El servicio agrega `AnalizarBatchAsync(skip, take)` que carga todos los textos ligeros (para duplicados) + el batch completo (para señales) en 2 queries. El page model expone un handler JSON `OnPostAnalizarBatchAsync`. El front orquesta la secuencia de batches con barra de progreso y pinta resultados incrementalmente. El botón en Contenidos.cshtml se elimina y se agrega ítem al sidebar.

**Tech Stack:** ASP.NET Core 8 · Razor Pages · EF Core 8 · Bootstrap 5 · Vanilla JS (fetch + FormData) · `ISimilarQuestionDetector.CalcularSimilitud` (pre-filtro Jaccard preservado)

---

## File Map

| Acción | Archivo |
|--------|---------|
| Crear | `eiibd26/Services/Calidad/CalidadBatchResultDto.cs` — DTO de respuesta del batch |
| Modificar | `eiibd26/Services/Calidad/IContenidoCalidadService.cs` — agregar `AnalizarBatchAsync` |
| Modificar | `eiibd26/Services/Calidad/ContenidoCalidadService.cs` — implementar `AnalizarBatchAsync` |
| Modificar | `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml.cs` — reemplazar handler por `OnPostAnalizarBatchAsync` |
| Reemplazar | `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml` — nueva vista JS-driven |
| Modificar | `eiibd26/Areas/Identity/Pages/Shared/_SidebarMenu.cshtml` — agregar ítem bajo Contenidos |
| Modificar | `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Contenidos.cshtml` — quitar botón Calidad |

---

## Task 1: DTO de batch + interfaz actualizada

**Files:**
- Crear: `eiibd26/Services/Calidad/CalidadBatchResultDto.cs`
- Modificar: `eiibd26/Services/Calidad/IContenidoCalidadService.cs`

- [ ] **Step 1: Crear el DTO de resultado de batch**

```csharp
// eiibd26/Services/Calidad/CalidadBatchResultDto.cs
namespace eiibd26.Services.Calidad
{
    public class CalidadBatchResultDto
    {
        /// <summary>Total de contenidos NO eliminados (para calcular progreso en el front).</summary>
        public int Total { get; set; }
        /// <summary>Resultados del rango solicitado (skip/take).</summary>
        public List<ContenidoCalidadDto> Items { get; set; } = new();
    }
}
```

- [ ] **Step 2: Agregar el método a la interfaz**

Reemplazar el contenido de `IContenidoCalidadService.cs` con:

```csharp
namespace eiibd26.Services.Calidad
{
    /// <summary>
    /// Analiza señales de calidad sobre Contenidos (extensible a otros tipos).
    /// Usar AnalizarBatchAsync para análisis incremental — nunca AnalizarTodosAsync en producción.
    /// </summary>
    public interface IContenidoCalidadService
    {
        /// <summary>
        /// Analiza el rango [skip, skip+take) de contenidos no eliminados.
        /// Carga textos de TODOS para detección de duplicados, pero evalúa señales solo del batch.
        /// Cada petición es rápida (≤10 items, pre-filtro Jaccard elimina el 99%+ del Levenshtein).
        /// </summary>
        Task<CalidadBatchResultDto> AnalizarBatchAsync(int skip, int take);

        /// <summary>Analiza todos los contenidos en una sola llamada (solo para uso interno/tests).</summary>
        Task<List<ContenidoCalidadDto>> AnalizarTodosAsync();
    }
}
```

- [ ] **Step 3: Build**

```
cd eiibd26 && dotnet build --no-restore
```
Esperado: 1 error CS0535 (ContenidoCalidadService no implementa AnalizarBatchAsync) — confirma que la interfaz fue actualizada.

---

## Task 2: Implementar AnalizarBatchAsync en el servicio

**Files:**
- Modificar: `eiibd26/Services/Calidad/ContenidoCalidadService.cs`

El servicio ya tiene `AnalizarTodosAsync` con toda la lógica de señales y Jaccard. Agregar el nuevo método al final de la clase, antes del último `}`.

- [ ] **Step 1: Agregar el método `AnalizarBatchAsync` al servicio**

Insertar este código dentro de la clase `ContenidoCalidadService`, antes del último `}` del archivo (antes de los métodos privados — o después de `AnalizarTodosAsync`, antes de `ContarPalabras`):

```csharp
public async Task<CalidadBatchResultDto> AnalizarBatchAsync(int skip, int take)
{
    _logger.LogInformation("[CalidadContenido] Batch skip={Skip} take={Take}", skip, take);

    // Query 1 (ligera): todos los contenidos — necesarios para contexto de duplicados
    var todosLigeros = await _db.Contenidos
        .AsNoTracking()
        .Where(c => !c.Eliminado)
        .OrderBy(c => c.Id)
        .Select(c => new { c.Id, c.IdTipo, c.ContenidoTitulo, c.ContenidoTextoL })
        .ToListAsync();

    var total = todosLigeros.Count;

    // Query 2 (completa): solo el batch — con todos los campos y categorías
    var batchContenidos = await _db.Contenidos
        .AsNoTracking()
        .Where(c => !c.Eliminado)
        .Include(c => c.CategoriasRelacion.Where(r => !r.Borrado))
        .OrderBy(c => c.Id)
        .Skip(skip)
        .Take(take)
        .ToListAsync();

    // Pre-calcular textos y keyword-sets de TODOS (para comparación de duplicados)
    var todosTextos = todosLigeros
        .Select(c => new
        {
            c.Id,
            c.IdTipo,
            Texto = TruncarTexto($"{c.ContenidoTitulo} {StripHtml(c.ContenidoTextoL, 1500)}", 600)
        })
        .ToDictionary(t => t.Id);

    var kwSets = todosTextos.ToDictionary(
        kv => kv.Key,
        kv => ExtraerKeywordsLocal(kv.Value.Texto));

    // Duplicados: comparar cada item del batch contra TODOS los contenidos
    var duplicadosDeBatch = batchContenidos.ToDictionary(c => c.Id, _ => new List<int>());

    foreach (var batchItem in batchContenidos)
    {
        if (!todosTextos.TryGetValue(batchItem.Id, out var textoItem)) continue;
        var kwBatch = kwSets.GetValueOrDefault(batchItem.Id, new HashSet<string>());
        if (kwBatch.Count == 0) continue;

        foreach (var (otherId, otroTexto) in todosTextos)
        {
            if (otherId == batchItem.Id) continue;

            // Skip diferente IdTipo (cuando ambos lo tienen definido)
            if (textoItem.IdTipo.HasValue && otroTexto.IdTipo.HasValue
                && textoItem.IdTipo != otroTexto.IdTipo)
                continue;

            // Pre-filtro Jaccard: si Jaccard < 0.70, score combinado máximo = 0.79 < 0.80
            var kwOtro = kwSets.GetValueOrDefault(otherId, new HashSet<string>());
            if (JaccardLocal(kwBatch, kwOtro) < 0.70) continue;

            // Solo para el ~1% de pares que supera el pre-filtro
            var sim = _detector.CalcularSimilitud(textoItem.Texto, otroTexto.Texto);
            if (sim >= 0.80)
                duplicadosDeBatch[batchItem.Id].Add(otherId);
        }
    }

    // Evaluar señales para los items del batch
    var resultados = new List<ContenidoCalidadDto>();

    foreach (var c in batchContenidos)
    {
        var senales = new List<SenalCalidad>();
        var palabras = ContarPalabras(c.ContenidoTextoL);

        if (palabras < 50)
            senales.Add(new SenalCalidad("SIN_CUERPO",
                palabras == 0 ? "Sin cuerpo" : $"Cuerpo muy corto ({palabras} palabras, mínimo 50)",
                GravedadSenal.Critica));

        if (duplicadosDeBatch[c.Id].Count > 0)
            senales.Add(new SenalCalidad("DUPLICADO",
                $"Similar a {duplicadosDeBatch[c.Id].Count} contenido(s)",
                GravedadSenal.Critica));

        if (string.IsNullOrWhiteSpace(c.URLImagenPrincipal))
            senales.Add(new SenalCalidad("SIN_IMAGEN", "Sin imagen principal", GravedadSenal.Mejorable));

        if (string.IsNullOrWhiteSpace(c.ContenidoTextoC))
            senales.Add(new SenalCalidad("SIN_RESUMEN", "Sin resumen/descripción", GravedadSenal.Mejorable));

        if (palabras >= 50 && palabras <= 100)
            senales.Add(new SenalCalidad("CUERPO_CORTO", $"Cuerpo corto ({palabras} palabras)", GravedadSenal.Mejorable));

        if (!c.CategoriasRelacion.Any())
            senales.Add(new SenalCalidad("SIN_CATEGORIA", "Sin categoría asignada", GravedadSenal.Mejorable));

        if (string.IsNullOrWhiteSpace(c.ContenidoTituloSlug))
            senales.Add(new SenalCalidad("SIN_SLUG", "Sin slug", GravedadSenal.Mejorable));

        if (c.EstadoPublicacion == 0 && c.FechaCreado < DateTime.UtcNow.AddDays(-30))
            senales.Add(new SenalCalidad("BORRADOR_VIEJO",
                $"Borrador sin publicar hace {(DateTime.UtcNow - c.FechaCreado).Days} días",
                GravedadSenal.Mejorable));

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
            DuplicadoDeIds = duplicadosDeBatch[c.Id]
        });
    }

    return new CalidadBatchResultDto { Total = total, Items = resultados };
}
```

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores CS.

---

## Task 3: Actualizar page model con handler JSON de batch

**Files:**
- Modificar: `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml.cs`

Reemplazar el contenido completo del archivo con:

```csharp
using eiibd26.Services.Calidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    [Authorize(Roles = "Administrador")]
    public class CalidadModel : PageModel
    {
        private readonly IContenidoCalidadService _calidad;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        public CalidadModel(IContenidoCalidadService calidad)
            => _calidad = calidad;

        public void OnGet() { }

        /// <summary>
        /// Handler para análisis por batch. Devuelve JSON:
        /// { total: N, items: [ ContenidoCalidadDto... ] }
        /// Los enums se serializan como strings (ej. "Critico", "Mejorable", "Ok").
        /// </summary>
        public async Task<IActionResult> OnPostAnalizarBatchAsync(
            [FromForm] int skip,
            [FromForm] int take = 10)
        {
            if (take <= 0 || take > 50) take = 10;
            if (skip < 0) skip = 0;

            var resultado = await _calidad.AnalizarBatchAsync(skip, take);
            return new JsonResult(resultado, JsonOpts);
        }
    }
}
```

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores CS.

---

## Task 4: Reemplazar vista con barra de progreso JS

**Files:**
- Reemplazar: `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml`

Reemplazar el contenido completo del archivo con:

```cshtml
@page
@model eiibd26.Areas.Identity.Pages.Admin.Contenidos.CalidadModel

@{
    ViewData["Title"] = "Calidad de Contenido";
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

        .semaforo-critico   { color: #dc3545; font-size: 1.3rem; }
        .semaforo-mejorable { color: #ffc107; font-size: 1.3rem; }
        .semaforo-ok        { color: #198754; font-size: 1.3rem; }

        .counter-card {
            border-radius: 12px;
            padding: 18px 28px;
            text-align: center;
            min-width: 140px;
        }
        .counter-card.critico   { background: #fff5f5; border: 2px solid #f5c2c7; }
        .counter-card.mejorable { background: #fffbf0; border: 2px solid #ffd966; }
        .counter-card.ok        { background: #f0fff4; border: 2px solid #a8d5b5; }
        .counter-card.total-card { background: #f8f9fa; border: 2px solid #dee2e6; }
        .counter-card .numero   { font-size: 2.2rem; font-weight: 700; line-height: 1; }
        .counter-card .etiqueta { font-size: 0.85rem; color: #6b7280; margin-top: 4px; }

        .senal-badge {
            font-size: 0.72rem;
            padding: 3px 8px;
            border-radius: 20px;
            margin: 2px;
            display: inline-block;
            font-weight: 600;
        }
        .senal-critica   { background: #fde8e8; color: #b91c1c; border: 1px solid #fca5a5; }
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
                Analiza duplicados y señales de calidad — procesa en batches de 10 para no congelar la página.
            </div>
            <a asp-page="./Contenidos" class="text-decoration-none small">
                <i class="bi bi-arrow-left me-1"></i>Volver a Contenidos
            </a>
        </div>
        <div class="d-flex align-items-center gap-2">
            <!-- Token antiforgery para los fetch de batch -->
            <form id="formToken" style="display:none;">
                @Html.AntiForgeryToken()
            </form>
            <button id="btnAnalizar" onclick="iniciarAnalisis()" class="btn btn-primary">
                <i class="bi bi-search me-1"></i> Analizar contenido
            </button>
        </div>
    </div>

    <!-- Estado inicial -->
    <div id="estadoInicial" class="block-admin-uniform text-center py-5">
        <i class="bi bi-bar-chart-line" style="font-size:3rem;color:#adb5bd;"></i>
        <div class="mt-3 text-muted">
            Haz clic en <strong>Analizar contenido</strong> para iniciar el diagnóstico por batches.
        </div>
    </div>

    <!-- Barra de progreso (oculta al inicio) -->
    <div id="barraProgresoContainer" class="block-admin-uniform py-3" style="display:none;">
        <div class="d-flex justify-content-between mb-2">
            <span id="textoProgreso" class="text-muted small fw-semibold"></span>
            <span id="pctProgreso" class="text-muted small">0%</span>
        </div>
        <div class="progress" style="height:10px; border-radius:8px;">
            <div id="barraInterna"
                 class="progress-bar progress-bar-striped progress-bar-animated bg-primary"
                 role="progressbar"
                 style="width:0%; border-radius:8px;"
                 aria-valuenow="0" aria-valuemin="0" aria-valuemax="100">
            </div>
        </div>
        <div id="alertaBatchFallido" class="alert alert-warning mt-2 py-2 small" style="display:none;">
            <i class="bi bi-exclamation-triangle me-1"></i>
            <span id="mensajeBatchFallido"></span>
        </div>
    </div>

    <!-- Resumen contadores (oculto hasta completar) -->
    <div id="resumenContadores" style="display:none;">
        <div class="d-flex gap-3 flex-wrap mb-4">
            <div class="counter-card critico">
                <div class="numero semaforo-critico" id="numCriticos">🔴 0</div>
                <div class="etiqueta">Críticos</div>
            </div>
            <div class="counter-card mejorable">
                <div class="numero semaforo-mejorable" id="numMejorables">🟡 0</div>
                <div class="etiqueta">Mejorables</div>
            </div>
            <div class="counter-card ok">
                <div class="numero semaforo-ok" id="numOk">🟢 0</div>
                <div class="etiqueta">OK</div>
            </div>
            <div class="counter-card total-card">
                <div class="numero" id="numTotal" style="font-size:2.2rem;">0</div>
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
    </div>

    <!-- Tabla (visible desde el primer batch) -->
    <div id="tablaContainer" style="display:none;" class="block-admin-uniform p-0 overflow-hidden">
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
            <tbody id="tablaCalidadBody"></tbody>
        </table>
    </div>

</div>

@section Scripts {
<script>
    const TAKE = 10;
    let allItems = [];
    let filtroActivo = 'todos';

    function token() {
        return document.querySelector('#formToken [name="__RequestVerificationToken"]').value;
    }

    async function fetchBatch(skip) {
        const fd = new FormData();
        fd.append('__RequestVerificationToken', token());
        fd.append('skip', skip);
        fd.append('take', TAKE);
        const res = await fetch('?handler=AnalizarBatch', { method: 'POST', body: fd });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return await res.json();
    }

    async function iniciarAnalisis() {
        const btn = document.getElementById('btnAnalizar');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status"></span> Iniciando...';

        // Reset
        allItems = [];
        document.getElementById('estadoInicial').style.display = 'none';
        document.getElementById('resumenContadores').style.display = 'none';
        document.getElementById('tablaContainer').style.display = 'none';
        document.getElementById('tablaCalidadBody').innerHTML = '';
        document.getElementById('alertaBatchFallido').style.display = 'none';
        document.getElementById('barraProgresoContainer').style.display = 'block';
        actualizarBarra(0, 1, 'Iniciando análisis...');

        try {
            // Primer batch — obtiene el total
            const primerBatch = await fetchBatch(0);
            const total = primerBatch.total;

            if (total === 0) {
                actualizarBarra(1, 1, 'Sin contenidos para analizar.');
                btn.disabled = false;
                btn.innerHTML = '<i class="bi bi-search me-1"></i> Analizar contenido';
                return;
            }

            const totalBatches = Math.ceil(total / TAKE);
            appendItems(primerBatch.items);
            actualizarBarra(Math.min(TAKE, total), total, `Analizando ${Math.min(TAKE, total)} de ${total}...`);

            // Batches restantes
            for (let i = TAKE; i < total; i += TAKE) {
                try {
                    const batch = await fetchBatch(i);
                    appendItems(batch.items);
                } catch {
                    // Reintentar una vez
                    try {
                        const batch = await fetchBatch(i);
                        appendItems(batch.items);
                    } catch (e2) {
                        const msg = document.getElementById('mensajeBatchFallido');
                        const alerta = document.getElementById('alertaBatchFallido');
                        msg.textContent = `El batch ${i + 1}-${Math.min(i + TAKE, total)} falló dos veces y se omitió.`;
                        alerta.style.display = 'block';
                    }
                }
                const analizados = Math.min(i + TAKE, total);
                actualizarBarra(analizados, total, `Analizando ${analizados} de ${total}...`);
            }

            // Completado
            actualizarBarra(total, total, `Análisis completo — ${total} contenidos revisados.`);
            mostrarResumen(total);

        } catch (e) {
            actualizarBarra(0, 1, `Error al iniciar: ${e.message}`);
            document.getElementById('barraInterna').classList.add('bg-danger');
            document.getElementById('barraInterna').classList.remove('progress-bar-animated', 'bg-primary');
        }

        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-search me-1"></i> Analizar de nuevo';
    }

    function actualizarBarra(hecho, total, texto) {
        const pct = total > 0 ? Math.round((hecho / total) * 100) : 0;
        document.getElementById('textoProgreso').textContent = texto;
        document.getElementById('pctProgreso').textContent = pct + '%';
        const barra = document.getElementById('barraInterna');
        barra.style.width = pct + '%';
        barra.setAttribute('aria-valuenow', pct);
        if (pct >= 100) {
            barra.classList.remove('progress-bar-animated');
        }
    }

    function appendItems(items) {
        allItems = allItems.concat(items);
        const tbody = document.getElementById('tablaCalidadBody');
        document.getElementById('tablaContainer').style.display = 'block';

        items.forEach(item => {
            const nivel = item.nivelSemaforo;
            if (filtroActivo !== 'todos' && nivel !== filtroActivo) {
                // agregar igualmente al DOM pero oculto
                const tr = crearFila(item);
                tr.style.display = 'none';
                tbody.appendChild(tr);
            } else {
                tbody.appendChild(crearFila(item));
            }
        });
    }

    function crearFila(item) {
        const nivel = item.nivelSemaforo;
        const emoji = { Critico: '🔴', Mejorable: '🟡', Ok: '🟢' }[nivel] || '❓';
        const nivelClass = { Critico: 'critico', Mejorable: 'mejorable', Ok: 'ok' }[nivel] || '';

        const senalesBadges = item.senales.map(s => {
            const cls = s.gravedad === 'Critica' ? 'senal-critica' : 'senal-mejorable';
            return `<span class="senal-badge ${cls}" title="${esc(s.descripcion)}">${esc(s.codigo)}</span>`;
        }).join('');

        const dupLinks = item.duplicadoDeIds.length > 0
            ? `<div class="dup-link mt-1">Similares: ${item.duplicadoDeIds.map(id =>
                `<a href="./Detalle?id=${id}" target="_blank" class="me-1">#${id}</a>`).join('')}</div>`
            : '';

        const pubBadge = item.estadoPublicacion === 0
            ? '<span class="badge bg-secondary">Borrador</span>'
            : item.estadoPublicacion === 1
                ? '<span class="badge bg-success">Publicado</span>'
                : `<span class="badge bg-light text-muted">${item.estadoPublicacion ?? '-'}</span>`;

        const sinSeñales = item.senales.length === 0
            ? '<span class="text-muted small">Sin problemas detectados</span>'
            : '';

        const tr = document.createElement('tr');
        tr.dataset.nivel = nivel;
        tr.innerHTML = `
            <td class="text-center semaforo-${nivelClass}" title="${nivel}">${emoji}</td>
            <td>
                <div class="titulo-contenido">${esc(item.titulo)}</div>
                ${item.slug ? `<div class="text-muted" style="font-size:0.75rem;">/${esc(item.slug)}</div>` : ''}
            </td>
            <td class="text-center">${pubBadge}</td>
            <td>${senalesBadges}${dupLinks}${sinSeñales}</td>
            <td class="text-center">
                <a href="./Detalle?id=${item.id}" class="btn btn-sm btn-outline-primary" title="Editar">
                    <i class="bi bi-pencil-square"></i>
                </a>
            </td>`;
        return tr;
    }

    function mostrarResumen(total) {
        const criticos = allItems.filter(i => i.nivelSemaforo === 'Critico').length;
        const mejorables = allItems.filter(i => i.nivelSemaforo === 'Mejorable').length;
        const ok = allItems.filter(i => i.nivelSemaforo === 'Ok').length;

        document.getElementById('numCriticos').textContent = `🔴 ${criticos}`;
        document.getElementById('numMejorables').textContent = `🟡 ${mejorables}`;
        document.getElementById('numOk').textContent = `🟢 ${ok}`;
        document.getElementById('numTotal').textContent = total;
        document.getElementById('resumenContadores').style.display = 'block';
    }

    function esc(s) {
        return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    // Filtro por nivel
    document.addEventListener('click', e => {
        const btn = e.target.closest('.btn-filtro');
        if (!btn) return;
        document.querySelectorAll('.btn-filtro').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        filtroActivo = btn.dataset.nivel;
        document.querySelectorAll('#tablaCalidadBody tr').forEach(tr => {
            tr.style.display = (filtroActivo === 'todos' || tr.dataset.nivel === filtroActivo) ? '' : 'none';
        });
    });
</script>
}
```

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores CS.

---

## Task 5: Agregar ítem al sidebar admin

**Files:**
- Modificar: `eiibd26/Areas/Identity/Pages/Shared/_SidebarMenu.cshtml`

El submenu "Contenidos" está entre las líneas ~76-97. Agregar el nuevo ítem **después** de la entrada `ContenidosCategorias` (≈línea 89-95) y **antes** de `</ul>` (≈línea 96).

- [ ] **Step 1: Agregar el ítem en el sidebar**

Buscar la sección que contiene `bi bi-tags` (Categorías de Contenido) y agregar **debajo** de ese `</li>`:

```html
                        <li>
                            <a href="/Identity/Admin/Contenidos/Calidad"
                               class="nav-link ms-4 @(IsActive("/Identity/Admin/Contenidos/Calidad") ? "active" : "")">
                                <i class="bi bi-patch-check"></i> Calidad de Contenido
                            </a>
                        </li>
```

El bloque del submenuContenidos debe quedar así:

```html
                <div class="collapse @(contenidosOpen ? "show" : "")" id="submenuContenidos">
                    <ul class="btn-toggle-nav list-unstyled fw-normal pb-1 small">
                        <li>
                            <a href="/Identity/Admin/Contenidos/Contenidos"
                               class="nav-link ms-4 @(IsActive("/Identity/Admin/Contenidos/Contenidos") ? "active" : "")">
                                <i class="bi bi-file-earmark-text"></i> Contenidos
                            </a>
                        </li>
                        <li>
                            <a href="/Identity/Admin/Contenidos/ContenidosCategorias"
                               class="nav-link ms-4 @(IsActive("/Identity/Admin/Contenidos/ContenidosCategorias") ? "active" : "")">
                                <i class="bi bi-tags"></i> Categorías de Contenido
                            </a>
                        </li>
                        <li>
                            <a href="/Identity/Admin/Contenidos/Calidad"
                               class="nav-link ms-4 @(IsActive("/Identity/Admin/Contenidos/Calidad") ? "active" : "")">
                                <i class="bi bi-patch-check"></i> Calidad de Contenido
                            </a>
                        </li>
                        <li>
                            <a href="/Identity/Admin/BannersInicio/Index"
                               class="nav-link ms-4 @(IsActive("/Identity/Admin/BannersInicio/Index") ? "active" : "")">
                                <i class="bi bi-images"></i> Banners Inicio
                            </a>
                        </li>
                    </ul>
                </div>
```

- [ ] **Step 2: Build**

```
dotnet build --no-restore
```
Esperado: 0 errores CS.

---

## Task 6: Quitar botón de Contenidos.cshtml

**Files:**
- Modificar: `eiibd26/Areas/Identity/Pages/Admin/Contenidos/Contenidos.cshtml`

- [ ] **Step 1: Eliminar el bloque del botón Calidad**

Buscar y eliminar exactamente estas 3 líneas (≈líneas 276-278):

```html
<a asp-page="./Calidad" class="btn btn-outline-warning ms-2">
    <i class="bi bi-bar-chart-line me-1"></i> Calidad de contenido
</a>
```

- [ ] **Step 2: Build final**

```
dotnet build --no-restore
```
Esperado: 0 errores CS, 0 warnings nuevos.

---

## Task 7: Verificación en browser + commit

- [ ] **Step 1: Detener debugger y reiniciar la aplicación**

```
dotnet run
```

- [ ] **Step 2: Verificar menú**

Entrar como Administrador. El sidebar debe mostrar "Calidad de Contenido" bajo el menú "Contenidos". El botón ya NO aparece en la página de grid de Contenidos.

- [ ] **Step 3: Verificar análisis por batches**

Ir a `/Identity/Admin/Contenidos/Calidad`. Clic en "Analizar contenido". Verificar:
- La barra de progreso aparece inmediatamente
- El texto dice "Analizando 10 de N..."
- Las filas se pintan de a 10 mientras avanza la barra
- Al terminar: contadores 🔴/🟡/🟢 aparecen arriba
- Los filtros funcionan
- Cada fila tiene link a Detalle
- Duplicados muestran links con IDs

- [ ] **Step 4: Verificar que cada petición es rápida**

Con DevTools Network abierto, verificar que cada request a `?handler=AnalizarBatch` responde en < 2s. Ningún request se cuelga.

- [ ] **Step 5: Commit**

```bash
git add eiibd26/Services/Calidad/CalidadBatchResultDto.cs \
        eiibd26/Services/Calidad/IContenidoCalidadService.cs \
        eiibd26/Services/Calidad/ContenidoCalidadService.cs \
        eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml \
        eiibd26/Areas/Identity/Pages/Admin/Contenidos/Calidad.cshtml.cs \
        eiibd26/Areas/Identity/Pages/Shared/_SidebarMenu.cshtml \
        eiibd26/Areas/Identity/Pages/Admin/Contenidos/Contenidos.cshtml

git commit -m "feat(contenidos): análisis de calidad por batches de 10 con barra de progreso + mover a menú"
```

---

## Self-Review

### Spec coverage

| Requisito | Task |
|-----------|------|
| `AnalizarBatchAsync(skip, take)` | Task 1+2 |
| Duplicados contra TODOS (no solo el batch) | Task 2 |
| Pre-filtro Jaccard preservado | Task 2 |
| Handler `OnPostAnalizarBatchAsync` devuelve JSON | Task 3 |
| Enums como strings en JSON | Task 3 |
| First batch devuelve `total` | Task 2+3 |
| JS llama batches secuencialmente | Task 4 |
| Barra de progreso con texto "Analizando X de Y" | Task 4 |
| Resultados pintados incrementalmente | Task 4 |
| Manejo de error (retry 1 vez, avisar, continuar) | Task 4 |
| Resumen final 🔴/🟡/🟢 | Task 4 |
| Filtro por nivel | Task 4 |
| Links a Detalle | Task 4 |
| Links a contenidos duplicados | Task 4 |
| Solo admin | Task 3 (`[Authorize(Roles = "Administrador")]`) |
| "Calidad de Contenido" en sidebar admin | Task 5 |
| Ítem activo cuando se está en esa página | Task 5 (usa `IsActive(...)`) |
| Quitar botón de Contenidos.cshtml | Task 6 |
| Sin esquema nuevo | ✅ todo en memoria |
| Cada petición rápida < 2s | ✅ 10 items × Jaccard pre-filtro |

### Verificación de tipos

- `CalidadBatchResultDto.Total` (int) ↔ `primerBatch.total` en JS (camelCase via `JsonNamingPolicy.CamelCase`) ✅
- `CalidadBatchResultDto.Items` (List<ContenidoCalidadDto>) ↔ `primerBatch.items` en JS ✅
- `ContenidoCalidadDto.NivelSemaforo` (enum→"Critico") ↔ `item.nivelSemaforo` en JS ✅
- `SenalCalidad.Gravedad` (enum→"Critica") ↔ `s.gravedad === 'Critica'` en JS ✅
- `ContenidoCalidadDto.Id` (int) ↔ `item.id` en JS (camelCase) ✅
- `AnalizarBatchAsync(int skip, int take)` en interfaz ↔ implementación en servicio ↔ `[FromForm] int skip, int take` en handler ✅
