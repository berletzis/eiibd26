using eiibd26.Models;
using eiibd26.Services.Glossary.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eiibd26.Services.Community
{
    /// <summary>
    /// Integración READ-ONLY de estados de ánimo para el módulo Glosario.
    /// No modifica ninguna tabla existente.
    /// Cache de 60 segundos para evitar lecturas repetidas en páginas populares.
    /// </summary>
    public class CommunityExperienceService : ICommunityExperienceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CommunityExperienceService> _logger;

        // Rutas de iconos SVG usadas en UsuarioEstadoAnimo.cshtml
        private static readonly Dictionary<int, (string Icon, string Nombre)> _moodMeta = new()
        {
            { 1, ("/img/muymal.svg",  "Muy mal")  },
            { 2, ("/img/mal.svg",     "Mal")       },
            { 3, ("/img/neutral.svg", "Neutral")   },
            { 4, ("/img/bien.svg",    "Bien")      },
            { 5, ("/img/muybien.svg", "Muy bien")  }
        };

        public CommunityExperienceService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<CommunityExperienceService> logger)
        {
            _db    = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<CommunityExperienceDto>> GetRecentExperiencesBySymptomAsync(
            int symptomId,
            int maxResults = 5,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"community_symptom_{symptomId}_{maxResults}";
            if (_cache.TryGetValue(cacheKey, out List<CommunityExperienceDto>? cached) && cached is not null)
                return cached;

            try
            {
                // Obtener todas las relaciones usuario->sintoma (ej. usuarios que tienen el síntoma en su perfil)
                var relaciones = await _db.sintomasUsuario
                    .AsNoTracking()
                    .Where(su => su.idSintoma == symptomId && !su.Eliminado)
                    .Select(su => new { su.id, su.idUsuario, su.fechaCreado })
                    .ToListAsync(cancellationToken);

                if (relaciones.Count == 0)
                    return new List<CommunityExperienceDto>();

                var relationIds = relaciones.Select(r => r.id).ToList();
                var userIds = relaciones.Select(r => r.idUsuario).Distinct().ToList();

                // Traer los estados de ánimo asociados a estas relaciones (si existen)
                var estados = await _db.EstadoAnimoUsuario
                    .AsNoTracking()
                    .Where(e => !e.Eliminado
                        && userIds.Contains(e.IdUsuario)
                        && e.IdSintomaUsuario != null
                        && relationIds.Contains(e.IdSintomaUsuario.Value))
                    .OrderByDescending(e => e.FechaRegistro)
                    .Select(e => new RawEntry
                    {
                        EstadoMood        = (int)e.EstadoMood,
                        Texto             = e.Texto,
                        FechaRegistro     = e.FechaRegistro,
                        IdUsuario         = e.IdUsuario,
                        CondicionNombre   = e.CondicionUsuario != null && e.CondicionUsuario.Condicion != null
                                            ? e.CondicionUsuario.Condicion.nombre : null,
                        SintomaNombre     = e.SintomaUsuario != null && e.SintomaUsuario.Sintoma != null
                                            ? e.SintomaUsuario.Sintoma.nombre : null,
                        TratamientoNombre = e.TratamientoUsuario != null && e.TratamientoUsuario.Tratamiento != null
                                            ? e.TratamientoUsuario.Tratamiento.nombre : null
                    })
                    .ToListAsync(cancellationToken);

                // Por usuario, quedarnos con el registro más reciente (si existe)
                var latestByUser = estados
                    .GroupBy(e => e.IdUsuario)
                    .Select(g => g.First())
                    .ToDictionary(r => r.IdUsuario, r => r);

                // Para usuarios sin estado, construir un RawEntry sintético usando la fecha de creación de la relación
                var missingUsers = userIds.Where(uid => !latestByUser.ContainsKey(uid)).ToList();
                var synthetic = new List<RawEntry>();
                foreach (var uid in missingUsers)
                {
                    var rel = relaciones.FirstOrDefault(r => r.idUsuario == uid);
                    if (rel == null) continue;
                    synthetic.Add(new RawEntry
                    {
                        EstadoMood = (int)EstadoAnimoEnum.Neutral, // valor por defecto
                        Texto = null,
                        FechaRegistro = rel.fechaCreado,
                        IdUsuario = rel.idUsuario,
                        CondicionNombre = null,
                        SintomaNombre = null,
                        TratamientoNombre = null
                    });
                }

                // Combinar: estados más recientes primero, luego sintéticos. Limitar a maxResults.
                var deduped = estados
                    .GroupBy(e => e.IdUsuario)
                    .Select(g => g.First())
                    .Concat(synthetic)
                    .OrderByDescending(r => r.FechaRegistro)
                    .Take(maxResults)
                    .ToList();

                var result = await MapToDtoAsync(deduped, cancellationToken);
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(60));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener experiencias para síntoma {Id}", symptomId);
                return new List<CommunityExperienceDto>();
            }
        }

        public async Task<List<CommunityExperienceDto>> GetRecentExperiencesByTreatmentAsync(
            int treatmentId,
            int maxResults = 5,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"community_treatment_{treatmentId}_{maxResults}";
            if (_cache.TryGetValue(cacheKey, out List<CommunityExperienceDto>? cached) && cached is not null)
                return cached;

            try
            {
                // Obtener todas las relaciones usuario->tratamiento
                var relaciones = await _db.tratamientoUsuario
                    .AsNoTracking()
                    .Where(tu => tu.idTratamiento == treatmentId && !tu.Eliminado)
                    .Select(tu => new { tu.id, tu.idUsuario, tu.fechaCreado, TratamientoNombre = tu.Tratamiento != null ? tu.Tratamiento.nombre : null })
                    .ToListAsync(cancellationToken);

                if (relaciones.Count == 0)
                    return new List<CommunityExperienceDto>();

                var relationIds = relaciones.Select(r => r.id).ToList();
                var userIds = relaciones.Select(r => r.idUsuario).Distinct().ToList();

                var estados = await _db.EstadoAnimoUsuario
                    .AsNoTracking()
                    .Where(e => !e.Eliminado
                        && userIds.Contains(e.IdUsuario)
                        && e.IdTratamientoUsuario != null
                        && relationIds.Contains(e.IdTratamientoUsuario.Value))
                    .OrderByDescending(e => e.FechaRegistro)
                    .Select(e => new RawEntry
                    {
                        EstadoMood        = (int)e.EstadoMood,
                        Texto             = e.Texto,
                        FechaRegistro     = e.FechaRegistro,
                        IdUsuario         = e.IdUsuario,
                        CondicionNombre   = e.CondicionUsuario != null && e.CondicionUsuario.Condicion != null
                                            ? e.CondicionUsuario.Condicion.nombre : null,
                        SintomaNombre     = e.SintomaUsuario != null && e.SintomaUsuario.Sintoma != null
                                            ? e.SintomaUsuario.Sintoma.nombre : null,
                        TratamientoNombre = e.TratamientoUsuario != null && e.TratamientoUsuario.Tratamiento != null
                                            ? e.TratamientoUsuario.Tratamiento.nombre : null
                    })
                    .ToListAsync(cancellationToken);

                var latestByUser = estados
                    .GroupBy(e => e.IdUsuario)
                    .Select(g => g.First())
                    .ToDictionary(r => r.IdUsuario, r => r);

                var missingUsers = userIds.Where(uid => !latestByUser.ContainsKey(uid)).ToList();
                var synthetic = new List<RawEntry>();
                foreach (var uid in missingUsers)
                {
                    var rel = relaciones.FirstOrDefault(r => r.idUsuario == uid);
                    if (rel == null) continue;
                    synthetic.Add(new RawEntry
                    {
                        EstadoMood = (int)EstadoAnimoEnum.Neutral,
                        Texto = null,
                        FechaRegistro = rel.fechaCreado,
                        IdUsuario = rel.idUsuario,
                        CondicionNombre = null,
                        SintomaNombre = null,
                        TratamientoNombre = rel.TratamientoNombre
                    });
                }

                var deduped = estados
                    .GroupBy(e => e.IdUsuario)
                    .Select(g => g.First())
                    .Concat(synthetic)
                    .OrderByDescending(r => r.FechaRegistro)
                    .Take(maxResults)
                    .ToList();

                var result = await MapToDtoAsync(deduped, cancellationToken);
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(60));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener experiencias para tratamiento {Id}", treatmentId);
                return new List<CommunityExperienceDto>();
            }
        }

        public async Task<bool> HasExperiencesBySymptomAsync(int symptomId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.EstadoAnimoUsuario
                    .AsNoTracking()
                    .AnyAsync(e => !e.Eliminado
                        && e.IdSintomaUsuario != null
                        && e.SintomaUsuario != null
                        && e.SintomaUsuario.idSintoma == symptomId
                        && !e.SintomaUsuario.Eliminado,
                        cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar experiencias para síntoma {Id}", symptomId);
                return false;
            }
        }

        public async Task<bool> HasExperiencesByTreatmentAsync(int treatmentId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.EstadoAnimoUsuario
                    .AsNoTracking()
                    .AnyAsync(e => !e.Eliminado
                        && e.IdTratamientoUsuario != null
                        && e.TratamientoUsuario != null
                        && e.TratamientoUsuario.idTratamiento == treatmentId
                        && !e.TratamientoUsuario.Eliminado,
                        cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar experiencias para tratamiento {Id}", treatmentId);
                return false;
            }
        }

        // ─────────────────────────────────────────
        // Helpers privados
        // ─────────────────────────────────────────

        private async Task<List<CommunityExperienceDto>> MapToDtoAsync(
            List<RawEntry> raw,
            CancellationToken cancellationToken)
        {
            if (raw.Count == 0)
                return new List<CommunityExperienceDto>();

            // Un solo query para perfiles: alias + avatar + slug (no N+1)
            var userIds = raw.Select(r => r.IdUsuario).Distinct().ToList();
            var perfiles = await _db.Perfil
                .AsNoTracking()
                .Where(p => userIds.Contains(p.idUser))
                .Select(p => new { p.idUser, p.Nombre, p.Avatar, p.slug })
                .ToDictionaryAsync(p => p.idUser, cancellationToken);

            var now = DateTime.UtcNow;
            return raw.Select(r =>
            {
                perfiles.TryGetValue(r.IdUsuario, out var perfil);
                _moodMeta.TryGetValue(r.EstadoMood, out var meta);

                return new CommunityExperienceDto
                {
                    EstadoMood                = r.EstadoMood,
                    EstadoMoodNombre          = meta.Nombre ?? r.EstadoMood.ToString(),
                    MoodIconUrl               = meta.Icon   ?? "/img/neutral.svg",
                    TextoPreview              = Truncate(r.Texto, 160),
                    FechaRelativa             = ToRelativeTime(r.FechaRegistro, now),
                    AliasUsuario              = BuildAlias(perfil?.Nombre),
                    UserSlug                  = string.IsNullOrWhiteSpace(perfil?.slug) ? null : perfil.slug,
                    AvatarUrl                 = string.IsNullOrWhiteSpace(perfil?.Avatar) ? null : perfil.Avatar,
                    CondicionNombre           = r.CondicionNombre,
                    SintomaContextoNombre     = r.SintomaNombre,
                    TratamientoContextoNombre = r.TratamientoNombre
                };
            }).ToList();
        }

        private static string? Truncate(string? text, int max)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return text.Length <= max ? text : text[..max].TrimEnd() + "…";
        }

        private static string ToRelativeTime(DateTime fecha, DateTime now)
        {
            var diff = now - fecha.ToUniversalTime();
            return diff.TotalMinutes < 60  ? $"hace {(int)diff.TotalMinutes} min"
                 : diff.TotalHours   < 24  ? $"hace {(int)diff.TotalHours} h"
                 : diff.TotalDays    < 7   ? $"hace {(int)diff.TotalDays} días"
                 : diff.TotalDays    < 30  ? $"hace {(int)(diff.TotalDays / 7)} sem."
                 : diff.TotalDays    < 365 ? $"hace {(int)(diff.TotalDays / 30)} meses"
                 : $"hace {(int)(diff.TotalDays / 365)} años";
        }

        private static string BuildAlias(string? nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "Paciente anónimo";
            var parts = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1 ? parts[0] : $"{parts[0]} {parts[1][0]}.";
        }

        // Proyección interna para evitar materializar la entidad completa
        private sealed class RawEntry
        {
            public int      EstadoMood        { get; init; }
            public string?  Texto             { get; init; }
            public DateTime FechaRegistro     { get; init; }
            public Guid     IdUsuario         { get; init; }
            public string?  CondicionNombre   { get; init; }
            public string?  SintomaNombre     { get; init; }
            public string?  TratamientoNombre { get; init; }
        }
    }
}
