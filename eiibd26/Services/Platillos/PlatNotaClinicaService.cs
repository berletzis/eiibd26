using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
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
