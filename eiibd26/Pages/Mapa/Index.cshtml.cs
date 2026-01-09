using eiibd26.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Pages.Mapa
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext db, IMemoryCache cache, ILogger<IndexModel> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public List<(string Code, string Name)> Countries { get; set; } = new();
        public List<(int id, string nombre)> Conditions { get; set; } = new();

        public string UserCountry { get; set; } = "";

        public async Task OnGetAsync()
        {
            const string countriesKey = "Mapa:Countries";
            const string conditionsKey = "Mapa:Conditions";

            if (_cache != null && _cache.TryGetValue(countriesKey, out List<(string, string)> cachedCountries))
            {
                Countries = cachedCountries;
            }
            else
            {
                try
                {
                    var paises = await _db.Paises
                        .AsNoTracking()
                        .Where(p => !p.Borrado && p.VIsibleBuscador == true)
                        .OrderBy(p => p.PaisNombre)
                        .Select(p => new { Code = p.PaisCodigo, Name = p.PaisNombre })
                        .ToListAsync();
                    var list = paises.Select(x => (x.Code ?? "", x.Name ?? "")).ToList();
                    Countries = list;
                    if (_cache != null)
                    {
                        var opt = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)).SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                        _cache.Set(countriesKey, list, opt);
                    }
                }
                catch
                {
                    Countries = new List<(string, string)>();
                }
            }

            if (_cache != null && _cache.TryGetValue(conditionsKey, out List<(int id, string nombre)> cachedConds))
            {
                Conditions = cachedConds;
            }
            else
            {
                try
                {
                    var conds = await _db.condiciones.AsNoTracking()
                        .Where(c => !c.Eliminado && c.idPadre == null)
                        .OrderBy(c => c.nombre)
                        .Select(c => new { c.id, c.nombre })
                        .ToListAsync();
                    var list = conds.Select(c => (c.id, c.nombre ?? "")).ToList();
                    Conditions = list;
                    if (_cache != null)
                    {
                        var opt2 = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)).SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                        _cache.Set(conditionsKey, list, opt2);
                    }
                }
                catch
                {
                    Conditions = new List<(int, string)>();
                }
            }

            try
            {
                var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var g))
                {
                    var perfil = await _db.Perfil.AsNoTracking().FirstOrDefaultAsync(p => p.idUser == g);
                    if (perfil != null)
                    {
                        UserCountry = perfil.NombrePais ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "No se pudo obtener UserCountry desde perfil actual");
            }
        }

        // Score-based ordering (marketing) + filtering por yearsRange correctamente aplicada
        public async Task<IActionResult> OnGetProfilesAsync(string country = "", int? conditionId = null, string yearsRange = "", int skip = 0, int take = 48)
        {
            int version = 0;
            if (_cache != null) _cache.TryGetValue<int>("Mapa:CacheVersion", out version);

            var key = $"Mapa:Profiles:v{version}:c={country?.Trim().ToUpper()}|cond={(conditionId.HasValue ? conditionId.Value.ToString() : "")}|yr={yearsRange}|skip={skip}|take={take}";
            if (_cache != null && _cache.TryGetValue(key, out object cachedObj))
            {
                return new JsonResult(cachedObj);
            }

            try
            {
                var now = DateTime.UtcNow;
                var minValidDate = new DateTime(1900, 1, 1);

                var basePerfil = _db.Perfil.AsNoTracking()
                    .Where(p => !string.IsNullOrWhiteSpace(p.Latitud) && !string.IsNullOrWhiteSpace(p.Longitud));

                if (!string.IsNullOrWhiteSpace(country))
                {
                    var cUp = country.Trim().ToUpperInvariant();
                    basePerfil = basePerfil.Where(p => (p.NombrePais ?? "").ToUpper().Contains(cUp));
                }

                // compute acceptedConditionIds if conditionId supplied (include descendants)
                List<int> acceptedConditionIds = null;
                if (conditionId.HasValue)
                {
                    var allConditions = await _db.condiciones
                        .AsNoTracking()
                        .Where(c => !c.Eliminado)
                        .Select(c => new { c.id, c.idPadre })
                        .ToListAsync();

                    var set = new HashSet<int>();
                    var q = new Queue<int>();
                    q.Enqueue(conditionId.Value);
                    set.Add(conditionId.Value);

                    while (q.Count > 0)
                    {
                        var parent = q.Dequeue();
                        var children = allConditions.Where(c => c.idPadre.HasValue && c.idPadre.Value == parent).Select(c => c.id);
                        foreach (var child in children)
                        {
                            if (set.Add(child)) q.Enqueue(child);
                        }
                    }

                    acceptedConditionIds = set.ToList();
                }

                // projected query: includes condFechaInicio (nullable) and condId
                var projectedQuery = basePerfil.Select(p => new
                {
                    idUser = p.idUser,
                    name = p.Nombre,
                    country = p.NombrePais,
                    lat = p.Latitud,
                    lng = p.Longitud,
                    avatar = p.Avatar,
                    fechaCreacion = (DateTime?)p.FechaCreacion,
                    acercaDe = p.AcercaDe,
                    nombreCiudad = p.NombreCiudad,

                    condFechaInicio = (DateTime?)_db.condicionUsuario
                        .Where(cu => !cu.Eliminado && cu.idUsuario == p.idUser && cu.fechaInicio != null && cu.fechaInicio > minValidDate && cu.fechaInicio <= now)
                        .OrderByDescending(cu => cu.fechaInicio)
                        .Select(cu => (DateTime?)cu.fechaInicio)
                        .FirstOrDefault(),

                    condId = (int?)_db.condicionUsuario
                        .Where(cu => !cu.Eliminado && cu.idUsuario == p.idUser && cu.fechaInicio != null && cu.fechaInicio > minValidDate && cu.fechaInicio <= now)
                        .OrderByDescending(cu => cu.fechaInicio)
                        .Select(cu => (int?)cu.idCondicion)
                        .FirstOrDefault(),

                    lastMoodDate = (DateTime?)_db.EstadoAnimoUsuario
                        .Where(m => !m.Eliminado && m.IdUsuario == p.idUser)
                        .OrderByDescending(m => m.FechaRegistro)
                        .Select(m => (DateTime?)m.FechaRegistro)
                        .FirstOrDefault()
                });

                // apply condition filter (include descendants) if requested
                if (acceptedConditionIds != null && acceptedConditionIds.Any())
                {
                    var acc = acceptedConditionIds; // materialize to list
                    projectedQuery = projectedQuery.Where(x => x.condId.HasValue && acc.Contains(x.condId.Value));
                }

                // --- NEW: apply yearsRange filter ---
                if (!string.IsNullOrWhiteSpace(yearsRange))
                {
                    // compute boundaries based on "now"
                    if (yearsRange == "0-1")
                    {
                        var a = now.AddYears(-1);
                        // include those with condDate within last year OR without condDate (as your label indicated)
                        projectedQuery = projectedQuery.Where(x => x.condFechaInicio == null || (x.condFechaInicio <= now && x.condFechaInicio > a));
                    }
                    else if (yearsRange == "1-3")
                    {
                        var upper = now.AddYears(-1);
                        var lower = now.AddYears(-3);
                        projectedQuery = projectedQuery.Where(x => x.condFechaInicio.HasValue && x.condFechaInicio <= upper && x.condFechaInicio > lower);
                    }
                    else if (yearsRange == "3-5")
                    {
                        var upper = now.AddYears(-3);
                        var lower = now.AddYears(-5);
                        projectedQuery = projectedQuery.Where(x => x.condFechaInicio.HasValue && x.condFechaInicio <= upper && x.condFechaInicio > lower);
                    }
                    else if (yearsRange == "5-10")
                    {
                        var upper = now.AddYears(-5);
                        var lower = now.AddYears(-10);
                        projectedQuery = projectedQuery.Where(x => x.condFechaInicio.HasValue && x.condFechaInicio <= upper && x.condFechaInicio > lower);
                    }
                    else if (yearsRange == "10+")
                    {
                        var cutoff = now.AddYears(-10);
                        projectedQuery = projectedQuery.Where(x => x.condFechaInicio.HasValue && x.condFechaInicio <= cutoff);
                    }
                }

                // For marketing: compute simple score (translated to SQL-friendly expressions)
                var scoredQuery = projectedQuery.Select(x => new
                {
                    x.idUser,
                    x.name,
                    x.country,
                    x.lat,
                    x.lng,
                    x.avatar,
                    x.fechaCreacion,
                    x.condFechaInicio,
                    x.condId,
                    x.lastMoodDate,
                    hasCond = x.condFechaInicio.HasValue ? 1 : 0,
                    avatarReal = ((x.avatar != null) && !x.avatar.ToLower().Contains("default-avatar") && !x.avatar.ToLower().Contains("ui-avatars.com")) ? 1 : 0,
                    hasLastMood = x.lastMoodDate.HasValue ? 1 : 0,
                    hasAbout = (x.acercaDe != null && x.acercaDe != "") ? 1 : 0,
                    hasCity = (x.nombreCiudad != null && x.nombreCiudad != "") ? 1 : 0
                })
                .Select(x => new
                {
                    x.idUser,
                    x.name,
                    x.country,
                    x.lat,
                    x.lng,
                    x.avatar,
                    x.fechaCreacion,
                    x.condFechaInicio,
                    x.condId,
                    x.lastMoodDate,
                    score = x.hasCond * 100 + x.avatarReal * 20 + x.hasLastMood * 15 + x.hasAbout * 5 + x.hasCity * 5
                });

                var ordered = scoredQuery
                    .OrderByDescending(x => x.score)
                    .ThenByDescending(x => x.condFechaInicio)
                    .ThenByDescending(x => x.lastMoodDate)
                    .ThenByDescending(x => x.fechaCreacion);

                var total = await ordered.CountAsync();
                var page = await ordered.Skip(skip).Take(take).ToListAsync();

                var userIds = page.Select(x => x.idUser).Distinct().ToList();

                var latestMoods = new List<EstadoAnimoUsuario>();
                if (userIds.Any())
                {
                    latestMoods = await _db.EstadoAnimoUsuario
                        .AsNoTracking()
                        .Where(m => userIds.Contains(m.IdUsuario) && !m.Eliminado)
                        .GroupBy(m => m.IdUsuario)
                        .Select(g => g.OrderByDescending(x => x.FechaRegistro).FirstOrDefault())
                        .ToListAsync();
                }
                var latestMoodByUser = latestMoods.Where(x => x != null).ToDictionary(x => x.IdUsuario, x => x);

                var conditionIds = page.Where(x => x.condId.HasValue).Select(x => x.condId.Value).Distinct().ToList();
                var condLookup = new Dictionary<int, string>();
                if (conditionIds.Any())
                {
                    var condRows = await _db.condiciones
                        .AsNoTracking()
                        .Where(c => conditionIds.Contains(c.id))
                        .Select(c => new { c.id, c.nombre })
                        .ToListAsync();
                    condLookup = condRows.ToDictionary(c => c.id, c => c.nombre ?? "");
                }

                var items = page.Select(o =>
                {
                    var lm = latestMoodByUser.ContainsKey(o.idUser) ? latestMoodByUser[o.idUser] : null;
                    string lastMood = lm != null ? (lm.EstadoMood ?? "") : "";
                    string lastMoodText = lm != null ? (lm.Texto ?? "") : "";
                    string condNombre = o.condId.HasValue && condLookup.ContainsKey(o.condId.Value) ? condLookup[o.condId.Value] : "";

                    int? yearsSinceDiagnosis = null;
                    if (o.condFechaInicio.HasValue && o.condFechaInicio.Value > minValidDate && o.condFechaInicio.Value <= now)
                    {
                        var span = now - o.condFechaInicio.Value;
                        if (span.TotalDays >= 0)
                            yearsSinceDiagnosis = (int)Math.Floor(span.TotalDays / 365.25);
                    }

                    return new
                    {
                        id = o.idUser,
                        name = o.name ?? "",
                        country = o.country ?? "",
                        lat = o.lat,
                        lng = o.lng,
                        avatar = o.avatar ?? "",
                        condicionId = o.condId,
                        condicionNombre = condNombre,
                        condFechaInicio = o.condFechaInicio.HasValue ? o.condFechaInicio.Value.ToString("o") : null,
                        hasFechaDiagnostico = o.condFechaInicio.HasValue,
                        yearsSinceDiagnosis = yearsSinceDiagnosis,
                        lastMood = lastMood,
                        lastMoodText = lastMoodText
                    };
                }).ToList();

                var payload = new { total = total, items = items };
                if (_cache != null)
                {
                    var opt = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(2)).SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                    _cache.Set(key, payload, opt);
                }

                return new JsonResult(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error construyendo perfiles para mapa");
                return new JsonResult(new { total = 0, items = new object[0] });
            }
        }
    }
}