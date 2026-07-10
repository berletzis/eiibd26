using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Contenidos;
using eiibd26.Services.Cobertura;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services.Contenidos
{
    /// <inheritdoc/>
    public sealed class OportunidadesService : IOportunidadesService
    {
        private readonly ICoberturaVistaService _cobertura;
        private readonly ApplicationDbContext _db;

        // Estados canónicos que el servicio de cobertura ya clasificó (embeddings, 0.78/0.55).
        private const string EstadoHueco = "Hueco";  // sin match ≥ 0.55  → "Escribir nuevo"
        private const string EstadoDebil = "Débil";   // 0.55 ≤ score < 0.78 → "Ampliar"

        public OportunidadesService(ICoberturaVistaService cobertura, ApplicationDbContext db)
        {
            _cobertura = cobertura;
            _db = db;
        }

        public async Task<OportunidadesVistaDto> ObtenerAsync(string? fuente, bool verDescartados, CancellationToken ct = default)
        {
            IReadOnlyList<CoberturaTemaDto> temas;
            try
            {
                // Embeddings (motor por defecto) — MISMA fuente de verdad que el panel técnico.
                temas = await _cobertura.ObtenerCoberturaTemasAsync(orden: null, motor: null, ct);
            }
            catch (Exception ex)
            {
                return new OportunidadesVistaDto
                {
                    Disponible = false,
                    Aviso = "No se pudo leer la cobertura del motor de embeddings. " +
                            "¿Hay similitud calculada? Detalle: " + ex.Message
                };
            }

            // Estado del backlog de externos (una query).
            var estados = await _db.OportunidadEstado.AsNoTracking()
                .Where(o => o.Tipo == OportunidadEstado.TipoExterno)
                .Select(o => new { o.RefId, o.Estado })
                .ToDictionaryAsync(o => o.RefId, o => (EstadoBacklog)o.Estado, ct);

            // Recencia del externo (fallback de orden para "Escribir nuevo", ya que sin clustering
            // no sabemos "cuántas fuentes lo cubren"). EmbeddingCalculadoEn del ScrapedPage.
            var recencia = await _db.ScrapedPagesRef.AsNoTracking()
                .Where(s => s.Embedding != null && s.Embedding != "[]")
                .Select(s => new { s.ScrapedPageId, s.EmbeddingCalculadoEn })
                .ToDictionaryAsync(s => s.ScrapedPageId, s => s.EmbeddingCalculadoEn, ct);

            EstadoBacklog EstadoDe(int id) => estados.TryGetValue(id, out var e) ? e : EstadoBacklog.Nuevo;

            bool PasaFiltros(CoberturaTemaDto t)
            {
                if (!verDescartados && EstadoDe(t.ScrapedPageId) == EstadoBacklog.Descartado) return false;
                if (!string.IsNullOrWhiteSpace(fuente) &&
                    !string.Equals(t.SitioNombre, fuente, StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }

            OportunidadRowDto Fila(CoberturaTemaDto t, bool incluirCercano) => new()
            {
                ScrapedPageId = t.ScrapedPageId,
                Titulo = t.Titulo,
                Fuente = t.SitioNombre,
                Url = t.Url,
                PropioCercanoTitulo = incluirCercano ? t.MejorArticuloTitulo : null,
                PropioCercanoId = incluirCercano ? t.MejorArticuloId : null,
                EstadoBacklog = EstadoDe(t.ScrapedPageId)
            };

            // "Escribir nuevo" (huecos): sin propio cercano; orden por recencia del externo desc.
            var sinCubrir = temas
                .Where(t => t.Estado == EstadoHueco)
                .Where(PasaFiltros)
                .OrderByDescending(t => recencia.TryGetValue(t.ScrapedPageId, out var f) ? f : (DateTime?)null)
                .Select(t => Fila(t, incluirCercano: false))
                .ToList();

            // "Ampliar" (débiles): con propio cercano; orden por cercanía (mejor score) desc.
            var ampliar = temas
                .Where(t => t.Estado == EstadoDebil)
                .Where(PasaFiltros)
                .OrderByDescending(t => t.MejorScore ?? 0)
                .Select(t => Fila(t, incluirCercano: true))
                .ToList();

            // Fuentes para el filtro: todas las presentes en las dos lentes (antes de filtrar por fuente).
            var fuentes = temas
                .Where(t => t.Estado == EstadoHueco || t.Estado == EstadoDebil)
                .Select(t => t.SitioNombre)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new OportunidadesVistaDto
            {
                TotalSinCubrir = sinCubrir.Count,
                TotalAmpliar = ampliar.Count,
                SinCubrir = sinCubrir,
                Ampliar = ampliar,
                Fuentes = fuentes
            };
        }

        public async Task ActualizarEstadoAsync(string tipo, int refId, EstadoBacklog estado, string? editorUserId, CancellationToken ct = default)
        {
            var fila = await _db.OportunidadEstado
                .FirstOrDefaultAsync(o => o.Tipo == tipo && o.RefId == refId, ct);

            if (fila != null)
            {
                fila.Estado = (byte)estado;
                fila.EditorUserId = editorUserId;
                fila.FechaActualizada = DateTime.UtcNow;
            }
            else
            {
                _db.OportunidadEstado.Add(new OportunidadEstado
                {
                    Tipo = tipo,
                    RefId = refId,
                    Estado = (byte)estado,
                    EditorUserId = editorUserId,
                    FechaActualizada = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
