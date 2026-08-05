using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models.Validacion;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services.Platillos
{
    /// <inheritdoc cref="IPlatNotaClinicaService"/>
    public class PlatNotaClinicaService : IPlatNotaClinicaService
    {
        private readonly ApplicationDbContext _db;
        public PlatNotaClinicaService(ApplicationDbContext db) => _db = db;

        // Predicado del candado, en un solo lugar. Cualquier consulta de lectura lo comparte:
        // publicada + activa + al menos una sección con contenido real (Contenido no vacío).
        // "Ausencia de datos ≠ seguridad": una nota publicada pero vacía NO es visible.
        private static bool EsVisible(Models.Platillos.PlatNotaClinica n) =>
            n.Publicado && n.Activo
            && n.Secciones.Any(s => s.Contenido != null && s.Contenido.Trim() != "");

        public async Task<PlatNotaVisibleDto?> ObtenerNotaVisibleParaPacienteAsync(string tipoDestino, int destinoId, string tipoNota = "Tolerancia")
        {
            if (string.IsNullOrWhiteSpace(tipoDestino)) return null;

            // EL CANDADO en la consulta: publicada + activa. El filtro de contenido se remata en memoria
            // (parseo de bloques), pero la consulta ya exige ≥1 sección con contenido → paridad con bulk.
            var nota = await _db.PlatNotasClinicas.AsNoTracking()
                .Where(n => n.TipoDestino == tipoDestino && n.DestinoId == destinoId && n.TipoNota == tipoNota
                            && n.Publicado && n.Activo
                            && n.Secciones.Any(s => s.Contenido != null && s.Contenido.Trim() != ""))
                .Select(n => new
                {
                    n.Id,
                    n.Titulo,
                    Secciones = n.Secciones
                        .OrderBy(s => s.Orden).ThenBy(s => s.Id)
                        .Select(s => new { s.Titulo, s.Contenido })
                        .ToList(),
                    Referencias = n.Referencias
                        .OrderBy(r => r.Orden).ThenBy(r => r.Id)
                        .Select(r => new { r.Titulo, r.Url })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (nota == null) return null;

            var secciones = nota.Secciones
                .Where(s => !string.IsNullOrWhiteSpace(s.Contenido))
                .Select(s => new PlatNotaSeccionDto
                {
                    Titulo = string.IsNullOrWhiteSpace(s.Titulo) ? null : s.Titulo!.Trim(),
                    Bloques = ParsearBloques(s.Contenido!)
                })
                .ToList();

            // Doble llave: si tras el parseo no quedó nada visible, es "nada".
            if (secciones.Count == 0) return null;

            var referencias = nota.Referencias
                .Where(r => !string.IsNullOrWhiteSpace(r.Titulo))
                .Select(r => new PlatNotaReferenciaDto
                {
                    Titulo = r.Titulo.Trim(),
                    Url = string.IsNullOrWhiteSpace(r.Url) ? null : r.Url!.Trim()
                })
                .ToList();

            return new PlatNotaVisibleDto
            {
                Id = nota.Id,
                Titulo = nota.Titulo,
                Secciones = secciones,
                Referencias = referencias
            };
        }

        public async Task<HashSet<int>> ObtenerDestinosConNotaVisibleAsync(string tipoDestino)
        {
            if (string.IsNullOrWhiteSpace(tipoDestino)) return new HashSet<int>();

            var ids = await _db.PlatNotasClinicas.AsNoTracking()
                .Where(n => n.TipoDestino == tipoDestino && n.Publicado && n.Activo
                            && n.Secciones.Any(s => s.Contenido != null && s.Contenido.Trim() != ""))
                .Select(n => n.DestinoId)
                .ToListAsync();

            return ids.ToHashSet();
        }

        public async Task<List<PlatNotaParaValidarDto>> ObtenerNotasParaValidarAsync(
            string usuarioMedicoId,
            int limite = 10,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(usuarioMedicoId) || limite <= 0)
                return new List<PlatNotaParaValidarDto>();

            // 1) Notas visibles — MISMO candado que la lectura individual.
            var notas = await _db.PlatNotasClinicas.AsNoTracking()
                .Where(n => n.Publicado && n.Activo
                            && n.Secciones.Any(s => s.Contenido != null && s.Contenido.Trim() != ""))
                .Select(n => new { n.Id, n.Titulo, n.TipoDestino, n.TipoNota, n.DestinoId })
                .ToListAsync(cancellationToken);

            if (notas.Count == 0) return new List<PlatNotaParaValidarDto>();

            // 2) Catálogo de ingredientes ACTIVOS. Se materializa porque PlatIngrediente no
            //    persiste slug: se genera desde el nombre, igual que en Ingrediente.cshtml.cs.
            //    Si el ingrediente no está activo su ficha da 404, así que no sirve de destino.
            var ingredientes = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => i.Activo)
                .Select(i => new { i.Id, i.Nombre, i.GrupoId })
                .ToListAsync(cancellationToken);

            var ingredientePorId = ingredientes.ToDictionary(i => i.Id);

            // Representante por grupo: el primer ingrediente activo por nombre. Determinista,
            // para que la fila no salte de destino entre recargas.
            var representantePorGrupo = ingredientes
                .GroupBy(i => i.GrupoId)
                .ToDictionary(g => g.Key, g => g.OrderBy(i => i.Nombre, StringComparer.OrdinalIgnoreCase).First());

            var gruposNombre = await _db.PlatGrupos.AsNoTracking()
                .Select(g => new { g.Id, g.Nombre })
                .ToDictionaryAsync(g => g.Id, g => g.Nombre, cancellationToken);

            // 3) Conteo de validaciones y estado propio — 2 queries batched, sin N+1.
            var notaIds = notas.Select(n => n.Id).ToList();

            var conteos = await _db.ValidacionesContenidoProfesional.AsNoTracking()
                .Where(v => v.TipoContenido == TipoContenidoValidado.NotaClinicaIngrediente
                            && notaIds.Contains(v.ContenidoId)
                            && v.Estado == EstadoValidacion.Validado)
                .GroupBy(v => v.ContenidoId)
                .Select(g => new { ContenidoId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.ContenidoId, x => x.Total, cancellationToken);

            // Las mías: cualquier estado (también en revisión u oculta — es su propio historial).
            var mias = await _db.ValidacionesContenidoProfesional.AsNoTracking()
                .Where(v => v.TipoContenido == TipoContenidoValidado.NotaClinicaIngrediente
                            && notaIds.Contains(v.ContenidoId)
                            && v.UsuarioMedicoId == usuarioMedicoId)
                .Select(v => new { v.ContenidoId, v.Estado })
                .ToListAsync(cancellationToken);

            var miEstadoPorNota = mias
                .GroupBy(x => x.ContenidoId)
                .ToDictionary(g => g.Key, g => g.Min(x => x.Estado)); // Validado(1) gana sobre EnRevision(2)

            // 4) Resolver cada nota al ingrediente por el que se valida.
            var filas = new List<PlatNotaParaValidarDto>();
            foreach (var n in notas)
            {
                string destinoNombre;
                string nombreDestinoIng;

                if (n.TipoDestino == "Ingrediente")
                {
                    if (!ingredientePorId.TryGetValue(n.DestinoId, out var ing)) continue; // inactivo/inexistente
                    destinoNombre    = ing.Nombre;
                    nombreDestinoIng = ing.Nombre;
                }
                else if (n.TipoDestino == "Grupo")
                {
                    if (!representantePorGrupo.TryGetValue(n.DestinoId, out var rep)) continue; // grupo sin ingredientes → enlace muerto
                    destinoNombre    = gruposNombre.TryGetValue(n.DestinoId, out var gn) ? gn : "Grupo";
                    nombreDestinoIng = rep.Nombre;
                }
                else continue;

                filas.Add(new PlatNotaParaValidarDto
                {
                    NotaId                      = n.Id,
                    Titulo                      = n.Titulo,
                    TipoDestino                 = n.TipoDestino,
                    TipoNota                    = n.TipoNota,
                    DestinoNombre               = destinoNombre,
                    NombreIngredienteParaValidar = nombreDestinoIng,
                    SlugIngredienteParaValidar  = SlugHelper.GenerateSlug(nombreDestinoIng),
                    TotalValidaciones           = conteos.TryGetValue(n.Id, out var c) ? c : 0,
                    MiEstado                    = miEstadoPorNota.TryGetValue(n.Id, out var e) ? e : null
                });
            }

            // 5) Lo que le falta primero; luego lo que menos respaldo tiene.
            return filas
                .OrderBy(f => f.MiEstado.HasValue ? 1 : 0)
                .ThenBy(f => f.TotalValidaciones)
                .ThenBy(f => f.DestinoNombre, StringComparer.OrdinalIgnoreCase)
                .Take(limite)
                .ToList();
        }

        /// <summary>
        /// Convierte el texto de una sección en bloques: líneas que empiezan con "- " se agrupan en
        /// una lista de viñetas; el resto son párrafos. Garantía: contenido no-vacío ⇒ ≥1 bloque
        /// (mantiene la paridad con el predicado <see cref="EsVisible"/> / la consulta bulk).
        /// </summary>
        private static List<PlatNotaBloqueDto> ParsearBloques(string contenido)
        {
            var bloques = new List<PlatNotaBloqueDto>();
            PlatNotaBloqueDto? listaActual = null;

            foreach (var raw in contenido.Replace("\r", "").Split('\n'))
            {
                var linea = raw.Trim();
                if (linea.Length == 0) { listaActual = null; continue; }

                if (linea.StartsWith("- ", StringComparison.Ordinal))
                {
                    var texto = linea.Substring(2).Trim();
                    if (texto.Length == 0) continue;
                    if (listaActual == null)
                    {
                        listaActual = new PlatNotaBloqueDto { EsLista = true };
                        bloques.Add(listaActual);
                    }
                    listaActual.Lineas.Add(texto);
                }
                else
                {
                    listaActual = null;
                    bloques.Add(new PlatNotaBloqueDto { EsLista = false, Lineas = { linea } });
                }
            }

            if (bloques.Count == 0)
                bloques.Add(new PlatNotaBloqueDto { EsLista = false, Lineas = { contenido.Trim() } });

            return bloques;
        }
    }
}
