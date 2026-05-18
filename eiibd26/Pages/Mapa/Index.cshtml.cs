using eiibd26.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _config;

        public IndexModel(ApplicationDbContext db, IMemoryCache cache, ILogger<IndexModel> logger, IConfiguration config)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
            _config = config;
        }

        public List<(string Code, string Name)> Countries { get; set; } = new();
        public List<(int id, string nombre)> Conditions { get; set; } = new();

        public string UserCountry { get; set; } = "";
        public string GoogleMapsApiKey { get; private set; } = "";

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

            GoogleMapsApiKey = _config["GoogleMaps:ApiKey"] ?? "";
        }

        public async Task<IActionResult> OnGetProfilesAsync(string country = "", int? conditionId = null, string yearsRange = "", int skip = 0, int take = 100)
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


            // Obtener el ID del rol "Paciente"
            var pacienteRoleId = await _db.Roles
                .Where(r => r.Name == "Paciente")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (pacienteRoleId == null)
            {
                // Si no existe el rol Paciente, devolver lista vacía
                var emptyResult = new { total = 0, profiles = new List<object>() };
                if (_cache != null)
                {
                    var optEmpty = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(2));
                    _cache.Set(key, emptyResult, optEmpty);
                }
                return new JsonResult(emptyResult);
            }

            // Filtrar perfiles cuyo usuario tenga el rol Paciente (puede tener otros roles también)
            var usersWithPacienteRole = _db.UserRoles
                .Where(ur => ur.RoleId == pacienteRoleId)
                .Select(ur => ur.UserId);

            var basePerfil = _db.Perfil.AsNoTracking()
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.Latitud) &&
                    !string.IsNullOrWhiteSpace(p.Longitud) &&
                    usersWithPacienteRole.Contains(p.idUser)
                );

                if (!string.IsNullOrWhiteSpace(country))
                {
                    // ⚠️ Perfil.NombrePais almacena el código ISO (ej: "MX"), NO el nombre del país.
                    // Comparación directa código-a-código; no depende de la tabla Paises.
                    var code = country.Trim().ToUpperInvariant();
                    basePerfil = basePerfil.Where(p =>
                        p.NombrePais == code ||
                        p.NombrePais == code.ToUpper());
                }

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

                var projectedQuery = basePerfil.Select(p => new
                {
                    idUser = p.idUser,
                    name = p.Nombre,
                    country = p.NombrePais,
                    lat = p.Latitud,
                    lng = p.Longitud,
                    avatar = p.Avatar,
                    slug = p.slug,
                    fechaCreacion = (DateTime?)p.FechaCreacion,
                    acercaDe = p.AcercaDe,
                    nombreCiudad = p.NombreCiudad,

                    condFechaInicio = (DateTime?)_db.condicionUsuario
                        .Where(cu => !cu.Eliminado && cu.idUsuario == p.idUser && cu.fechaInicio != null && cu.fechaInicio > minValidDate && cu.fechaInicio <= now)
                        .OrderByDescending(cu => cu.fechaInicio)
                        .Select(cu => (DateTime?)cu.fechaInicio)
                        .FirstOrDefault(),

                    condId = (int?)_db.condicionUsuario
                        .Where(cu => !cu.Eliminado && cu.idUsuario == p.idUser
                            && (cu.fechaInicio == null || (cu.fechaInicio > minValidDate && cu.fechaInicio <= now)))
                        .OrderByDescending(cu => cu.fechaInicio)
                        .Select(cu => (int?)cu.idCondicion)
                        .FirstOrDefault(),

                    lastMoodDate = (DateTime?)_db.EstadoAnimoUsuario
                        .Where(m => !m.Eliminado && m.IdUsuario == p.idUser)
                        .OrderByDescending(m => m.FechaRegistro)
                        .Select(m => (DateTime?)m.FechaRegistro)
                        .FirstOrDefault()
                });

                if (acceptedConditionIds != null && acceptedConditionIds.Any())
                {
                    var acc = acceptedConditionIds;
                    projectedQuery = projectedQuery.Where(x => x.condId.HasValue && acc.Contains(x.condId.Value));
                }

                if (!string.IsNullOrWhiteSpace(yearsRange))
                {
                    if (yearsRange == "0-1")
                    {
                        var a = now.AddYears(-1);
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

                var scoredQuery = projectedQuery.Select(x => new
                {
                    x.idUser,
                    x.name,
                    x.country,
                    x.lat,
                    x.lng,
                    x.avatar,
                    x.slug,
                    x.fechaCreacion,
                    x.condFechaInicio,
                    x.condId,
                    x.lastMoodDate,
                    hasCond = x.condId.HasValue ? 1 : 0,
                    avatarReal = ((x.avatar != null) && !x.avatar.ToLower().Contains("default") && !x.avatar.ToLower().Contains("ui-avatars.com")) ? 1 : 0,
                    hasLastMood = x.lastMoodDate.HasValue ? 1 : 0,
                    hasAbout = (x.acercaDe != null && x.acercaDe != "") ? 1 : 0,
                    hasCity = (x.nombreCiudad != null && x.nombreCiudad != "") ? 1 : 0,
                    hasCountry = (x.country != null && x.country != "") ? 1 : 0,
                    hasLocation = (x.lat != null && x.lat != "" && x.lng != null && x.lng != "") ? 1 : 0,
                    hasRealName = (x.name != null && x.name != "" && x.name != "Usuario") ? 1 : 0
                })
                .Select(x => new
                {
                    x.idUser,
                    x.name,
                    x.country,
                    x.lat,
                    x.lng,
                    x.avatar,
                    x.slug,
                    x.fechaCreacion,
                    x.condFechaInicio,
                    x.condId,
                    x.lastMoodDate,
                    score = (x.condFechaInicio.HasValue ? 80 : 0)
                            + x.avatarReal * 200
                            + x.hasRealName * 25
                            + x.hasLastMood * 175
                            + x.hasCond * 30
                            + x.hasCountry * 20
                            + x.hasLocation * 10
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

                    // ✅ CAMBIO: Convertir el enum a int y luego a string para compatibilidad
                    int? lastMoodNum = lm != null ? (int?)lm.EstadoMood : null;
                    string lastMood = lastMoodNum.HasValue ? lastMoodNum.Value.ToString() : "";

                    // También podemos enviar el nombre legible del enum
                    string lastMoodLabel = lastMoodNum.HasValue ? lm.EstadoMood.ToString() : "";

                    string lastMoodText = lm != null ? (lm.Texto ?? "") : "";
                    string condNombre = o.condId.HasValue && condLookup.ContainsKey(o.condId.Value) ? condLookup[o.condId.Value] : "";

                    string diagnosisAgeText = null;
                    int? yearsSinceDiagnosis = null;
                    if (o.condFechaInicio.HasValue && o.condFechaInicio.Value > minValidDate && o.condFechaInicio.Value <= now)
                    {
                        var (anios, meses) = eiibd26.Helpers.DateCalculationHelper
                            .CalcularDuracion(o.condFechaInicio.Value, DateTime.Today);
                        yearsSinceDiagnosis = anios;

                        if (anios == 0 && meses == 0)
                            diagnosisAgeText = "Menos de 1 mes";
                        else if (anios == 0)
                            diagnosisAgeText = meses == 1 ? "1 mes" : $"{meses} meses";
                        else
                            diagnosisAgeText = anios == 1 ? "1 año" : $"{anios} años";
                    }

                    return new
                    {
                        id = o.idUser,
                        name = o.name ?? "",
                        country = o.country ?? "",
                        lat = o.lat,
                        lng = o.lng,
                        avatar = o.avatar ?? "",
                        slug = o.slug ?? "",
                        condicionId = o.condId,
                        condicionNombre = condNombre,
                        condFechaInicio = o.condFechaInicio.HasValue ? o.condFechaInicio.Value.ToString("o") : null,
                        hasFechaDiagnostico = o.condFechaInicio.HasValue,
                        yearsSinceDiagnosis = yearsSinceDiagnosis,
                        diagnosisAgeText = diagnosisAgeText,

                        // ✅ CAMBIO: Enviar el valor numérico (1-5)
                        lastMood = lastMood,
                        // Opcional: También enviar el nombre legible (MuyBien, Bien, etc.)
                        lastMoodLabel = lastMoodLabel,
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

        /// <summary>
        /// Devuelve la distribución de estados de ánimo para un país dado (o global).
        /// La ubicación se infiere del perfil actual del usuario; no representa la ubicación
        /// histórica al momento del registro del mood.
        /// </summary>
        public async Task<IActionResult> OnGetMoodDistributionAsync(string country = "")
        {
            try
            {
                // Roles de paciente
                var pacienteRoleId = await _db.Roles
                    .Where(r => r.Name == "Paciente")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                IQueryable<EstadoAnimoUsuario> moodQuery = _db.EstadoAnimoUsuario
                    .AsNoTracking()
                    .Where(m => !m.Eliminado);

                if (pacienteRoleId != null)
                {
                    var patientIds = _db.UserRoles
                        .Where(ur => ur.RoleId == pacienteRoleId)
                        .Select(ur => ur.UserId);

                    moodQuery = moodQuery.Where(m => patientIds.Contains(m.IdUsuario));
                }

                // JOIN Mood → Perfil → Paises para filtro exacto por código de país
                var joinedQuery = moodQuery
                    .Join(_db.Perfil,
                        m => m.IdUsuario,
                        p => p.idUser,
                        (m, p) => new { m.IdUsuario, m.EstadoMood, m.FechaRegistro, p.NombrePais })
                    // ⚠️ NombrePais contiene el código ISO (ej: "MX"); unir contra PaisCodigo.
                    .Join(_db.Paises,
                        mp => mp.NombrePais,
                        pa => pa.PaisCodigo,
                        (mp, pa) => new { mp.IdUsuario, mp.EstadoMood, mp.FechaRegistro, pa.PaisCodigo });

                if (!string.IsNullOrWhiteSpace(country))
                {
                    // NOTE: filtro exacto por código ISO (ej: "MX")
                    var code = country.Trim().ToUpperInvariant();
                    joinedQuery = joinedQuery.Where(x => x.PaisCodigo == code);
                }

                // Materializar una sola vez — el set filtrado es pequeño después de GroupBy
                var rows = await joinedQuery
                    .Select(x => new { x.IdUsuario, x.EstadoMood, x.FechaRegistro })
                    .ToListAsync();

                var usuariosUnicos = rows.Select(x => x.IdUsuario).Distinct().Count();

                // Distribución (GroupBy en memoria; rows ya está materializado y filtrado)
                var distribution = rows
                    .GroupBy(x => x.EstadoMood)
                    .Select(g => new { Estado = (int)g.Key, Conteo = g.Count() })
                    .ToList();

                var total = distribution.Sum(x => x.Conteo);

                var distribucion = distribution
                    .OrderBy(x => x.Estado)
                    .Select(x => new MoodDistributionDto
                    {
                        Estado = x.Estado,
                        EstadoLabel = MoodLabel(x.Estado),
                        Conteo = x.Conteo,
                        Porcentaje = total == 0 ? 0 : Math.Round(x.Conteo * 100.0 / total, 1)
                    })
                    .ToList();

                // Timeline (solo cuando hay pocos usuarios — últimos 7 días, máx 20 registros)
                var cutoff7d = DateTime.UtcNow.AddDays(-7);
                var timeline = usuariosUnicos <= 9
                    ? rows
                        .Where(x => x.FechaRegistro >= cutoff7d)
                        .OrderByDescending(x => x.FechaRegistro)
                        .Take(20)
                        .Select(x => new
                        {
                            fecha = x.FechaRegistro.ToString("dd/MM"),
                            estado = (int)x.EstadoMood,
                            estadoLabel = MoodLabel((int)x.EstadoMood)
                        })
                        .ToList<object>()
                    : new List<object>();

                // Insight automático: estado predominante
                var predominante = distribution
                    .OrderByDescending(x => x.Conteo)
                    .Select(x => MoodLabel(x.Estado))
                    .FirstOrDefault();

                var insight = predominante != null
                    ? new { estadoPredominante = predominante }
                    : null as object;

                return new JsonResult(new { usuariosUnicos, distribucion, timeline, insight });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener distribución de mood para mapa");
                return new JsonResult(new List<MoodDistributionDto>());
            }
        }

        private static string MoodLabel(int estado) => estado switch
        {
            1 => "Muy mal",
            2 => "Mal",
            3 => "Neutral",
            4 => "Bien",
            5 => "Muy bien",
            _ => "Desconocido"
        };
    }

    public class MoodDistributionDto
    {
        public int Estado { get; set; }
        public string EstadoLabel { get; set; }
        public int Conteo { get; set; }
        public double Porcentaje { get; set; }
    }
}