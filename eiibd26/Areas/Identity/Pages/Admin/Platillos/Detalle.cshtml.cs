using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Admin.Platillos
{
    [Authorize(Roles = "Administrador")]
    public class DetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DetalleModel(ApplicationDbContext db) => _db = db;

        public static readonly string[] Dificultades = { "Fácil", "Media", "Difícil" };

        // ---- Datos generales del platillo ----
        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string? Codigo { get; set; }
        [BindProperty] public string? Nombre { get; set; }
        [BindProperty] public int CategoriaId { get; set; }
        [BindProperty] public int? Porciones { get; set; }
        [BindProperty] public int? TiempoPrepMin { get; set; }
        [BindProperty] public string? Dificultad { get; set; }
        [BindProperty] public string? PasosResumidos { get; set; }
        [BindProperty] public string? FuenteNombre { get; set; }
        [BindProperty] public string? FuenteUrl { get; set; }
        [BindProperty] public string? Notas { get; set; }
        [BindProperty] public bool Activo { get; set; }   // Publicado (visible para pacientes). Requiere ≥1 ingrediente.

        // ---- Renglones de ingredientes (delete-all + insert al guardar) ----
        [BindProperty] public List<RowInput> Rows { get; set; } = new();

        public class RowInput
        {
            public int IngredienteId { get; set; }
            public string? IngredienteTexto { get; set; }   // lo que el admin tecleó en el buscador (para detectar id=0 con texto)
            public string? TextoOriginal { get; set; }
            public decimal? Cantidad { get; set; }
            public int? UnidadId { get; set; }
            public bool EsAlGusto { get; set; }
            public string? NotaPreparacion { get; set; }
            public List<int> AtributoUsoIds { get; set; } = new();
        }

        // ---- Combos / catálogos para la vista ----
        public List<PlatCategoria> Categorias { get; set; } = new();
        public List<PlatUnidad> Unidades { get; set; } = new();
        public List<PlatAtributo> AtributosUso { get; set; } = new();          // Ambito='Uso' (crudo/frito/en jugo)
        public List<PlatGrupo> GruposParaAlta { get; set; } = new();           // alta de ingrediente en línea
        public List<PlatAtributo> IntrinsecosParaAlta { get; set; } = new();   // alta de ingrediente en línea
        public string SuggestedCodigo { get; set; } = "";
        // nombre + estado de cada ingrediente ya referenciado por un renglón (para pintar "(inactivo)")
        public Dictionary<int, (string Nombre, bool Activo)> IngredienteInfo { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var p = await _db.PlatPlatillos.AsNoTracking()
                    .Include(x => x.Ingredientes).ThenInclude(r => r.Atributos)
                    .FirstOrDefaultAsync(x => x.Id == id.Value);
                if (p == null) { ErrorMessage = "Platillo no encontrado."; await LoadLookupsAsync(); return Page(); }

                Id = p.Id;
                Codigo = p.Codigo;
                Nombre = p.Nombre;
                CategoriaId = p.CategoriaId;
                Porciones = p.Porciones;
                TiempoPrepMin = p.TiempoPrepMin;
                Dificultad = p.Dificultad;
                PasosResumidos = p.PasosResumidos;
                FuenteNombre = p.FuenteNombre;
                FuenteUrl = p.FuenteUrl;
                Notas = p.Notas;
                Activo = p.Activo;

                Rows = p.Ingredientes
                    .OrderBy(r => r.Id)
                    .Select(r => new RowInput
                    {
                        IngredienteId = r.IngredienteId,
                        TextoOriginal = r.TextoOriginal,
                        Cantidad = r.Cantidad,
                        UnidadId = r.UnidadId,
                        EsAlGusto = r.EsAlGusto,
                        NotaPreparacion = r.NotaPreparacion,
                        AtributoUsoIds = r.Atributos.Select(a => a.AtributoId).ToList()
                    })
                    .ToList();
            }

            if (!id.HasValue) Activo = true;   // nuevo platillo: por defecto publicado (el guard exige ingredientes)

            await LoadLookupsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            var codigo = (Codigo ?? "").Trim();
            var nombre = (Nombre ?? "").Trim();
            var fuente = (FuenteNombre ?? "").Trim();

            var errores = new List<string>();
            if (string.IsNullOrWhiteSpace(codigo)) errores.Add("El código es obligatorio.");
            if (string.IsNullOrWhiteSpace(nombre)) errores.Add("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(fuente)) errores.Add("La fuente es obligatoria (siempre se acredita el origen).");

            var categoriaOk = await _db.PlatCategorias.AnyAsync(c => c.Id == CategoriaId);
            if (!categoriaOk) errores.Add("Debes seleccionar una categoría válida.");

            if (!string.IsNullOrWhiteSpace(Dificultad) && !Dificultades.Contains(Dificultad))
                errores.Add("Dificultad inválida.");

            var idActual = Id ?? 0;
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var dupCodigo = await _db.PlatPlatillos.AnyAsync(x => x.Codigo.ToLower() == codigo.ToLower() && x.Id != idActual);
                if (dupCodigo) errores.Add($"Ya existe un platillo con el código \"{codigo}\".");
            }

            // Renglones: NUNCA descartar en silencio un renglón con contenido.
            //  - Con ingrediente seleccionado (IngredienteId > 0) → válido.
            //  - Con contenido pero sin ingrediente (id = 0) → se RECHAZA el guardado con mensaje claro.
            //  - Completamente vacío (sin texto ni datos) → se ignora en silencio (fila que el admin agregó y no llenó).
            var rowsPost = Rows ?? new List<RowInput>();
            var rowsValidas = new List<RowInput>();
            for (int idx = 0; idx < rowsPost.Count; idx++)
            {
                var r = rowsPost[idx];
                if (r.IngredienteId > 0) { rowsValidas.Add(r); continue; }

                bool tieneContenido =
                    !string.IsNullOrWhiteSpace(r.IngredienteTexto) ||
                    !string.IsNullOrWhiteSpace(r.TextoOriginal) ||
                    r.Cantidad.HasValue || r.UnidadId.HasValue || r.EsAlGusto ||
                    !string.IsNullOrWhiteSpace(r.NotaPreparacion) ||
                    (r.AtributoUsoIds?.Any() ?? false);

                if (tieneContenido)
                {
                    var quePuso = !string.IsNullOrWhiteSpace(r.IngredienteTexto)
                        ? $"\"{r.IngredienteTexto!.Trim()}\""
                        : "(sin nombre)";
                    errores.Add($"El renglón {idx + 1} {quePuso} no tiene un ingrediente válido: selecciónalo de la lista, o créalo con el botón.");
                }
            }

            // Validar que cada ingrediente exista.
            var ingIds = rowsValidas.Select(r => r.IngredienteId).Distinct().ToList();
            var ingExistentes = await _db.PlatIngredientes
                .Where(i => ingIds.Contains(i.Id))
                .Select(i => i.Id)
                .ToListAsync();
            if (ingIds.Any(x => !ingExistentes.Contains(x)))
                errores.Add("Uno de los renglones referencia un ingrediente que no existe.");

            // No se puede publicar un platillo sin ingredientes: el motor lo mostraría al paciente
            // como compatible por ausencia de datos. Como borrador (Activo=false) sí se permite.
            if (Activo && rowsValidas.Count == 0)
                errores.Add("No puedes publicar un platillo sin ingredientes: el filtro no puede analizarlo. Agrega al menos uno, o desmarca \"Publicado\" para guardarlo como borrador.");

            if (errores.Any())
            {
                ErrorMessage = "Corrige: " + string.Join(" ", errores);
                await LoadLookupsAsync();
                return Page();
            }

            // ---- Guardar cabecera ----
            PlatPlatillo platillo;
            if (idActual > 0)
            {
                platillo = (await _db.PlatPlatillos.FirstOrDefaultAsync(x => x.Id == idActual))!;
                if (platillo == null) { ErrorMessage = "Platillo no encontrado."; await LoadLookupsAsync(); return Page(); }
            }
            else
            {
                platillo = new PlatPlatillo { FechaCreacion = DateTime.UtcNow };
                var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(uid, out var g)) platillo.CreadoPorUserId = g;
                _db.PlatPlatillos.Add(platillo);
            }

            platillo.Activo = Activo;
            platillo.Codigo = codigo;
            platillo.Nombre = nombre;
            platillo.CategoriaId = CategoriaId;
            platillo.Porciones = Porciones;
            platillo.TiempoPrepMin = TiempoPrepMin;
            platillo.Dificultad = string.IsNullOrWhiteSpace(Dificultad) ? null : Dificultad;
            platillo.PasosResumidos = string.IsNullOrWhiteSpace(PasosResumidos) ? null : PasosResumidos.Trim();
            platillo.FuenteNombre = fuente;
            platillo.FuenteUrl = string.IsNullOrWhiteSpace(FuenteUrl) ? null : FuenteUrl.Trim();
            platillo.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();

            await _db.SaveChangesAsync();   // asegura Id para altas

            // ---- Renglones: delete-all + insert. Los atributos de uso se borran solos
            //      por ON DELETE CASCADE (paso 1): un solo paso de borrado, nunca dos. ----
            var existentes = await _db.PlatPlatilloIngredientes
                .Where(x => x.PlatilloId == platillo.Id)
                .ToListAsync();
            _db.PlatPlatilloIngredientes.RemoveRange(existentes);

            // Atributos de uso válidos (evita colar ids de otro ámbito o inexistentes).
            var usoValidos = await _db.PlatAtributos
                .Where(a => a.Ambito == "Uso")
                .Select(a => a.Id)
                .ToListAsync();

            foreach (var r in rowsValidas)
            {
                var pi = new PlatPlatilloIngrediente
                {
                    PlatilloId = platillo.Id,
                    IngredienteId = r.IngredienteId,
                    TextoOriginal = string.IsNullOrWhiteSpace(r.TextoOriginal) ? null : r.TextoOriginal.Trim(),
                    Cantidad = r.EsAlGusto ? null : r.Cantidad,
                    UnidadId = r.UnidadId,
                    EsAlGusto = r.EsAlGusto,
                    NotaPreparacion = string.IsNullOrWhiteSpace(r.NotaPreparacion) ? null : r.NotaPreparacion.Trim(),
                    Atributos = (r.AtributoUsoIds ?? new List<int>())
                        .Distinct()
                        .Where(aid => usoValidos.Contains(aid))
                        .Select(aid => new PlatPlatilloIngredienteAtributo { AtributoId = aid })
                        .ToList()
                };
                _db.PlatPlatilloIngredientes.Add(pi);
            }

            await _db.SaveChangesAsync();

            SuccessMessage = (idActual > 0 ? "Platillo actualizado." : "Platillo creado.")
                + (Activo ? "" : " Guardado como borrador: no aparece para pacientes hasta publicarlo.");
            return RedirectToPage(new { id = platillo.Id });
        }

        // ---- Alta de ingrediente EN LÍNEA (AJAX): no recarga la página, no pierde el form ----
        public async Task<IActionResult> OnPostCrearIngredienteAsync(string? nombre, int grupoId, List<int>? atributoIds)
        {
            var nom = (nombre ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(nom))
                return new JsonResult(new { ok = false, mensaje = "El nombre es obligatorio." }) { StatusCode = 400 };

            var grupoOk = await _db.PlatGrupos.AnyAsync(g => g.Id == grupoId);
            if (!grupoOk)
                return new JsonResult(new { ok = false, mensaje = "Debes seleccionar un grupo válido." }) { StatusCode = 400 };

            var existente = await _db.PlatIngredientes.FirstOrDefaultAsync(i => i.Nombre.ToLower() == nom);
            if (existente != null)
            {
                // Ya existe: lo devolvemos para que el admin lo use en vez de duplicar.
                return new JsonResult(new
                {
                    ok = true,
                    yaExistia = true,
                    id = existente.Id,
                    nombre = existente.Nombre,
                    mensaje = $"\"{existente.Nombre}\" ya existía en el catálogo; se usó ese."
                });
            }

            var ing = new PlatIngrediente
            {
                Nombre = nom,
                GrupoId = grupoId,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            _db.PlatIngredientes.Add(ing);
            await _db.SaveChangesAsync();

            var intrinsecosValidos = await _db.PlatAtributos
                .Where(a => a.Ambito == "Ingrediente")
                .Select(a => a.Id)
                .ToListAsync();

            foreach (var aid in (atributoIds ?? new List<int>()).Distinct().Where(x => intrinsecosValidos.Contains(x)))
            {
                _db.PlatIngredienteAtributos.Add(new PlatIngredienteAtributo { IngredienteId = ing.Id, AtributoId = aid });
            }
            await _db.SaveChangesAsync();

            return new JsonResult(new { ok = true, yaExistia = false, id = ing.Id, nombre = ing.Nombre });
        }

        private async Task LoadLookupsAsync()
        {
            Dificultad ??= null;

            // Categorías: activas + (edición) la del platillo aunque esté inactiva (§7.2).
            Categorias = await _db.PlatCategorias.AsNoTracking()
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
                .ToListAsync();
            if (CategoriaId > 0 && !Categorias.Any(c => c.Id == CategoriaId))
            {
                var extra = await _db.PlatCategorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == CategoriaId);
                if (extra != null) Categorias.Add(extra);
            }

            // Unidades: activas + las referenciadas por algún renglón aunque estén inactivas (§7.2).
            Unidades = await _db.PlatUnidades.AsNoTracking()
                .Where(u => u.Activo)
                .OrderBy(u => u.Nombre)
                .ToListAsync();
            var unidadesEnUso = (Rows ?? new List<RowInput>())
                .Where(r => r.UnidadId.HasValue).Select(r => r.UnidadId!.Value).Distinct().ToList();
            var faltantesU = unidadesEnUso.Where(uid => !Unidades.Any(u => u.Id == uid)).ToList();
            if (faltantesU.Any())
            {
                var extraU = await _db.PlatUnidades.AsNoTracking().Where(u => faltantesU.Contains(u.Id)).ToListAsync();
                Unidades.AddRange(extraU);
                Unidades = Unidades.OrderBy(u => u.Nombre).ToList();
            }

            // Atributos de uso: activos + los referenciados por algún renglón aunque estén inactivos (§7.2).
            AtributosUso = await _db.PlatAtributos.AsNoTracking()
                .Where(a => a.Activo && a.Ambito == "Uso")
                .OrderBy(a => a.Nombre)
                .ToListAsync();
            var usoEnUso = (Rows ?? new List<RowInput>())
                .SelectMany(r => r.AtributoUsoIds ?? new List<int>()).Distinct().ToList();
            var faltantesA = usoEnUso.Where(aid => !AtributosUso.Any(a => a.Id == aid)).ToList();
            if (faltantesA.Any())
            {
                var extraA = await _db.PlatAtributos.AsNoTracking().Where(a => faltantesA.Contains(a.Id)).ToListAsync();
                AtributosUso.AddRange(extraA);
                AtributosUso = AtributosUso.OrderBy(a => a.Nombre).ToList();
            }

            // Alta de ingrediente en línea: grupos + atributos intrínsecos activos.
            GruposParaAlta = await _db.PlatGrupos.AsNoTracking()
                .Where(g => g.Activo).OrderBy(g => g.Orden).ThenBy(g => g.Nombre).ToListAsync();
            IntrinsecosParaAlta = await _db.PlatAtributos.AsNoTracking()
                .Where(a => a.Activo && a.Ambito == "Ingrediente").OrderBy(a => a.Nombre).ToListAsync();

            // Nombre + estado de los ingredientes ya referenciados por renglones (para "(inactivo)").
            var ingIds = (Rows ?? new List<RowInput>()).Where(r => r.IngredienteId > 0)
                .Select(r => r.IngredienteId).Distinct().ToList();
            if (ingIds.Any())
            {
                IngredienteInfo = await _db.PlatIngredientes.AsNoTracking()
                    .Where(i => ingIds.Contains(i.Id))
                    .ToDictionaryAsync(i => i.Id, i => (i.Nombre, i.Activo));
            }

            // Código sugerido para altas nuevas (siguiente P###).
            if (!(Id > 0))
            {
                var codes = await _db.PlatPlatillos.AsNoTracking().Select(p => p.Codigo).ToListAsync();
                int max = 0;
                foreach (var c in codes)
                {
                    if (!string.IsNullOrEmpty(c) && (c[0] == 'P' || c[0] == 'p') && int.TryParse(c.Substring(1), out var n))
                        max = Math.Max(max, n);
                }
                SuggestedCodigo = "P" + (max + 1).ToString("D3");
            }
        }
    }
}
