using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Platillos;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services.Platillos
{
    /// <inheritdoc cref="IPlatNotaAdminService"/>
    public class PlatNotaAdminService : IPlatNotaAdminService
    {
        private readonly ApplicationDbContext _db;
        public PlatNotaAdminService(ApplicationDbContext db) => _db = db;

        // Mismo guard que el candado de lectura (EsVisible): una nota solo puede publicarse / contar
        // como publicada si tiene ≥1 sección con contenido real. Un solo criterio, sin drift.
        private static bool TieneContenido(IEnumerable<PlatNotaSeccion> secciones) =>
            secciones.Any(s => !string.IsNullOrWhiteSpace(s.Contenido));

        public async Task<PlatNotaEditVm> CargarAsync(string tipoDestino, int destinoId, string destinoNombre, string tipoNota = "Tolerancia")
        {
            var nota = await _db.PlatNotasClinicas.AsNoTracking()
                .Include(n => n.Secciones)
                .Include(n => n.Referencias)
                .FirstOrDefaultAsync(n => n.TipoDestino == tipoDestino && n.DestinoId == destinoId && n.TipoNota == tipoNota);

            var vm = new PlatNotaEditVm
            {
                TipoDestino = tipoDestino,
                DestinoId = destinoId,
                DestinoNombre = destinoNombre,
                TipoNota = tipoNota
            };

            if (nota == null)
            {
                vm.Estado = PlatNotaEstado.SinNota;
                vm.PuedePublicar = false;
                return vm;
            }

            vm.NotaId = nota.Id;
            vm.Titulo = nota.Titulo;
            vm.Publicado = nota.Publicado;
            vm.Secciones = nota.Secciones
                .OrderBy(s => s.Orden).ThenBy(s => s.Id)
                .Select(s => new PlatNotaSeccionInput { Titulo = s.Titulo, Contenido = s.Contenido })
                .ToList();
            vm.Referencias = nota.Referencias
                .OrderBy(r => r.Orden).ThenBy(r => r.Id)
                .Select(r => new PlatNotaReferenciaInput { Titulo = r.Titulo, Url = r.Url })
                .ToList();

            var conContenido = TieneContenido(nota.Secciones);
            vm.PuedePublicar = conContenido;
            vm.Estado = (nota.Publicado && nota.Activo && conContenido)
                ? PlatNotaEstado.Publicada
                : PlatNotaEstado.Borrador;
            return vm;
        }

        public async Task<Dictionary<int, PlatNotaEstado>> ObtenerEstadosAsync(string tipoDestino, string tipoNota = "Tolerancia")
        {
            // Una sola consulta: por destino, ¿publicada+activa? y ¿tiene contenido? El mapeo a estado
            // se hace en memoria para no depender de traducir el enum en SQL.
            var filas = await _db.PlatNotasClinicas.AsNoTracking()
                .Where(n => n.TipoDestino == tipoDestino && n.TipoNota == tipoNota)
                .Select(n => new
                {
                    n.DestinoId,
                    n.Publicado,
                    n.Activo,
                    ConContenido = n.Secciones.Any(s => s.Contenido != null && s.Contenido.Trim() != "")
                })
                .ToListAsync();

            var dict = new Dictionary<int, PlatNotaEstado>();
            foreach (var f in filas)
            {
                // Fila existente = al menos borrador (aunque esté vacía, alguien la empezó);
                // Publicada solo si pasa el candado (publicada + activa + con contenido).
                dict[f.DestinoId] = (f.Publicado && f.Activo && f.ConContenido)
                    ? PlatNotaEstado.Publicada
                    : PlatNotaEstado.Borrador;
            }
            return dict;
        }

        public async Task<(bool Ok, string Mensaje)> GuardarBorradorAsync(
            string tipoDestino, int destinoId,
            string? titulo,
            List<PlatNotaSeccionInput> secciones,
            List<PlatNotaReferenciaInput> referencias,
            string tipoNota = "Tolerancia")
        {
            var nota = await _db.PlatNotasClinicas
                .Include(n => n.Secciones)
                .Include(n => n.Referencias)
                .FirstOrDefaultAsync(n => n.TipoDestino == tipoDestino && n.DestinoId == destinoId && n.TipoNota == tipoNota);

            var tituloLimpio = string.IsNullOrWhiteSpace(titulo) ? "" : titulo!.Trim();

            if (nota == null)
            {
                nota = new PlatNotaClinica
                {
                    TipoDestino = tipoDestino,
                    DestinoId = destinoId,
                    TipoNota = tipoNota,
                    Titulo = tituloLimpio,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _db.PlatNotasClinicas.Add(nota);
            }
            else
            {
                nota.Titulo = tituloLimpio;
                // Contenido a punto de cambiar → regresa a borrador (regla de oro). Limpia la estampa.
                nota.Publicado = false;
                nota.PublicadaPorUserId = null;
                nota.FechaPublicacion = null;
                _db.PlatNotaSecciones.RemoveRange(nota.Secciones);
                _db.PlatNotaReferencias.RemoveRange(nota.Referencias);
            }

            // Secciones: descartar filas totalmente vacías; orden = posición en el form.
            var seccLimpias = (secciones ?? new List<PlatNotaSeccionInput>())
                .Where(s => !string.IsNullOrWhiteSpace(s.Titulo) || !string.IsNullOrWhiteSpace(s.Contenido))
                .ToList();
            var orden = 0;
            foreach (var s in seccLimpias)
            {
                nota.Secciones.Add(new PlatNotaSeccion
                {
                    Orden = orden++,
                    Titulo = string.IsNullOrWhiteSpace(s.Titulo) ? null : s.Titulo!.Trim(),
                    Contenido = string.IsNullOrWhiteSpace(s.Contenido) ? null : s.Contenido!.Trim()
                });
            }

            // Referencias: necesitan título; URL opcional.
            var refsLimpias = (referencias ?? new List<PlatNotaReferenciaInput>())
                .Where(r => !string.IsNullOrWhiteSpace(r.Titulo))
                .ToList();
            orden = 0;
            foreach (var r in refsLimpias)
            {
                nota.Referencias.Add(new PlatNotaReferencia
                {
                    Orden = orden++,
                    Titulo = r.Titulo!.Trim(),
                    Url = string.IsNullOrWhiteSpace(r.Url) ? null : r.Url!.Trim()
                });
            }

            await _db.SaveChangesAsync();

            var msg = TieneContenido(nota.Secciones)
                ? "Nota guardada como borrador. Publícala cuando esté revisada."
                : "Nota guardada como borrador (aún sin contenido: agrega al menos una sección para poder publicarla).";
            return (true, msg);
        }

        public async Task<(bool Ok, string Mensaje)> PublicarAsync(string tipoDestino, int destinoId, Guid userId, string tipoNota = "Tolerancia")
        {
            var nota = await _db.PlatNotasClinicas
                .Include(n => n.Secciones)
                .FirstOrDefaultAsync(n => n.TipoDestino == tipoDestino && n.DestinoId == destinoId && n.TipoNota == tipoNota);

            if (nota == null)
                return (false, "No hay nota que publicar. Guarda primero el contenido.");

            if (!TieneContenido(nota.Secciones))
                return (false, "No puedes publicar una nota sin contenido. Agrega al menos una sección con texto.");

            nota.Publicado = true;
            nota.PublicadaPorUserId = userId;
            nota.FechaPublicacion = DateTime.UtcNow;
            nota.Activo = true;
            await _db.SaveChangesAsync();
            return (true, "Nota publicada. Ya es visible para los pacientes.");
        }

        public async Task<(bool Ok, string Mensaje)> DespublicarAsync(string tipoDestino, int destinoId, string tipoNota = "Tolerancia")
        {
            var nota = await _db.PlatNotasClinicas
                .FirstOrDefaultAsync(n => n.TipoDestino == tipoDestino && n.DestinoId == destinoId && n.TipoNota == tipoNota);

            if (nota == null)
                return (false, "No hay nota que despublicar.");

            nota.Publicado = false;
            nota.PublicadaPorUserId = null;
            nota.FechaPublicacion = null;
            await _db.SaveChangesAsync();
            return (true, "Nota despublicada. Dejó de ser visible para los pacientes.");
        }
    }
}
