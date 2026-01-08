using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.Pages.Mapa
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache? _cache;

        public IndexModel(ApplicationDbContext db, IMemoryCache? cache)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _cache = cache;
        }

        public List<(string Code, string Name)> Countries { get; set; } = new();
        public List<(int id, string nombre)> Conditions { get; set; } = new();

        public async Task OnGetAsync()
        {
            int version = 0;
            if (_cache != null) _cache.TryGetValue<int>("Mapa:CacheVersion", out version);

            var countriesKey = $"Mapa:Countries:v{version}";
            var conditionsKey = $"Mapa:Conditions:children:v{version}";

            if (_cache != null && _cache.TryGetValue(countriesKey, out List<(string Code, string Name)> cachedCountries))
            {
                Countries = cachedCountries;
            }
            else
            {
                try
                {
                    var paises = await _db.Paises.AsNoTracking()
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
                        .Where(c => !c.Eliminado && c.idPadre != null)
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
        }

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
                var latestConds = await _db.condicionUsuario
                    .AsNoTracking()
                    .Where(cu => !cu.Eliminado && cu.fechaInicio != null)
                    .GroupBy(cu => cu.idUsuario)
                    .Select(g => g.OrderByDescending(x => x.fechaInicio).FirstOrDefault())
                    .ToListAsync();
                var latestCondByUser = latestConds.Where(x => x != null).ToDictionary(x => x.idUsuario, x => x);

                var latestMoods = await _db.EstadoAnimoUsuario
                    .AsNoTracking()
                    .Where(m => !m.Eliminado)
                    .GroupBy(m => m.IdUsuario)
                    .Select(g => g.OrderByDescending(x => x.FechaRegistro).FirstOrDefault())
                    .ToListAsync();
                var latestMoodByUser = latestMoods.Where(x => x != null).ToDictionary(x => x.IdUsuario, x => x);

                var profilesQuery = _db.Perfil.AsNoTracking()
                    .Where(p => !string.IsNullOrWhiteSpace(p.Latitud) && !string.IsNullOrWhiteSpace(p.Longitud));

                if (!string.IsNullOrWhiteSpace(country))
                {
                    var cUp = country.Trim().ToUpperInvariant();
                    profilesQuery = profilesQuery.Where(p => (p.NombrePais ?? "").ToUpper().Contains(cUp));
                }

                var profiles = await profilesQuery
                    .Select(p => new
                    {
                        idUser = p.idUser,
                        name = p.Nombre ?? "",
                        country = p.NombrePais ?? "",
                        lat = p.Latitud,
                        lng = p.Longitud,
                        avatar = p.Avatar ?? ""
                    })
                    .ToListAsync();

                var projected = profiles.Select(p =>
                {
                    condicionUsuario cu = null;
                    latestCondByUser.TryGetValue(p.idUser, out cu);
                    int? condId = cu?.idCondicion;

                    DateTime? rawFecha = cu?.fechaInicio;
                    DateTime? condFechaInicio = null;
                    if (rawFecha.HasValue)
                    {
                        var r = rawFecha.Value;
                        if (r != DateTime.MinValue && r <= DateTime.UtcNow)
                        {
                            condFechaInicio = r;
                        }
                    }

                    bool hasFecha = condFechaInicio.HasValue;
                    int? yearsSinceDiagnosis = null;
                    if (hasFecha)
                    {
                        var now = DateTime.UtcNow;
                        yearsSinceDiagnosis = now.Year - condFechaInicio.Value.Year;
                        var m = now.Month - condFechaInicio.Value.Month;
                        if (m < 0 || (m == 0 && now.Day < condFechaInicio.Value.Day)) yearsSinceDiagnosis--;
                        if (yearsSinceDiagnosis < 0) yearsSinceDiagnosis = null;
                    }

                    EstadoAnimoUsuario mood = null;
                    latestMoodByUser.TryGetValue(p.idUser, out mood);

                    // compute first word for safe single-word display (server-side) using p.name
                    string displaySingle = null;
                    if (!string.IsNullOrWhiteSpace(p.name))
                    {
                        var parts = p.name.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0) displaySingle = parts[0];
                    }

                    return new
                    {
                        id = p.idUser.ToString(),
                        name = p.name,
                        displayNameSingle = displaySingle,
                        country = p.country,
                        lat = p.lat,
                        lng = p.lng,
                        avatar = p.avatar,
                        condicionId = condId,
                        condicionFechaInicio = condFechaInicio.HasValue ? condFechaInicio.Value.ToString("o") : null,
                        yearsSinceDiagnosis = yearsSinceDiagnosis,
                        hasFechaDiagnostico = hasFecha,
                        lastMood = mood?.EstadoMood,
                        lastMoodText = mood?.Texto,
                        lastMoodDate = mood != null ? mood.FechaRegistro.ToString("o") : null
                    };
                }).ToList();

                var condIds = projected.Where(x => x.condicionId.HasValue).Select(x => x.condicionId.Value).Distinct().ToList();
                var condMap = new Dictionary<int, string>();
                if (condIds.Any())
                {
                    var conds = await _db.condiciones.AsNoTracking()
                        .Where(c => condIds.Contains(c.id))
                        .Select(c => new { c.id, c.nombre })
                        .ToListAsync();
                    condMap = conds.ToDictionary(x => x.id, x => x.nombre ?? "");
                }

                var withNames = projected.Select(p => new
                {
                    id = p.id,
                    name = p.name,
                    displayNameSingle = p.displayNameSingle,
                    country = p.country,
                    lat = p.lat,
                    lng = p.lng,
                    avatar = string.IsNullOrWhiteSpace(p.avatar) ? "" : p.avatar,
                    condicionId = p.condicionId,
                    condicionNombre = p.condicionId.HasValue && condMap.ContainsKey(p.condicionId.Value) ? condMap[p.condicionId.Value] : "",
                    fechaDiagnostico = p.condicionFechaInicio,
                    yearsSinceDiagnosis = p.yearsSinceDiagnosis,
                    hasFechaDiagnostico = p.hasFechaDiagnostico,
                    lastMood = p.lastMood,
                    lastMoodText = p.lastMoodText,
                    lastMoodDate = p.lastMoodDate
                }).ToList();

                var filtered = withNames.AsEnumerable();

                if (conditionId.HasValue)
                {
                    filtered = filtered.Where(p => p.condicionId.HasValue && p.condicionId.Value == conditionId.Value);
                }

                if (!string.IsNullOrWhiteSpace(yearsRange))
                {
                    filtered = filtered.Where(p =>
                    {
                        var y = p.yearsSinceDiagnosis;
                        var hasFecha = p.hasFechaDiagnostico;
                        switch (yearsRange)
                        {
                            case "0-1":
                                if (!hasFecha) return true;
                                return y >= 0 && y <= 1;
                            case "1-3":
                                return hasFecha && y >= 1 && y <= 3;
                            case "3-5":
                                return hasFecha && y >= 3 && y <= 5;
                            case "5-10":
                                return hasFecha && y >= 5 && y <= 10;
                            case "10+":
                                return hasFecha && y >= 10;
                            default:
                                return true;
                        }
                    });
                }

                var total = filtered.Count();
                var page = filtered.Skip(skip).Take(take).ToList();

                var resultItems = page.Select(p => new
                {
                    id = p.id,
                    name = p.name,
                    displayNameSingle = p.displayNameSingle,
                    country = p.country,
                    lat = p.lat,
                    lng = p.lng,
                    avatar = p.avatar,
                    condicionId = p.condicionId,
                    condicionNombre = p.condicionNombre,
                    fechaDiagnostico = p.fechaDiagnostico,
                    yearsSinceDiagnosis = p.yearsSinceDiagnosis,
                    hasFechaDiagnostico = p.hasFechaDiagnostico,
                    lastMood = p.lastMood,
                    lastMoodText = p.lastMoodText,
                    lastMoodDate = p.lastMoodDate
                }).ToList();

                var payload = new { total = total, items = resultItems };

                if (_cache != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                    _cache.Set(key, payload, cacheOptions);
                }

                return new JsonResult(payload);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return new JsonResult(new { error = "Error interno", detail = ex.Message });
            }
        }

        public IActionResult OnPostInvalidateCache()
        {
            if (_cache == null) return new JsonResult(new { ok = false, message = "Cache no configurada" });

            _cache.TryGetValue<int>("Mapa:CacheVersion", out var version);
            version++;
            _cache.Set("Mapa:CacheVersion", version);
            return new JsonResult(new { ok = true, version });
        }
    }
}