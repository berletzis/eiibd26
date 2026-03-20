using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using eiibd26.Services;

namespace eiibd26.Areas.Identity.Pages.Admin.Usuarios // <-- namespace corregido si usas Areas
{
    [Authorize(Roles = "Administrador")]
    [IgnoreAntiforgeryToken]
    public class UsuariosIndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsuariosIndexModel> _logger;
        private readonly SendGridEmailSender _emailSender;

        public UsuariosIndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db, ILogger<UsuariosIndexModel> logger, SendGridEmailSender emailSender)
        {
            _userManager = userManager;
            _db = db;
            _logger = logger;
            _emailSender = emailSender;
        }

        public void OnGet() { }

        private string Param(string key)
            => Request.HasFormContentType && Request.Form.ContainsKey(key)
                ? Request.Form[key].ToString()
                : Request.Query[key].ToString();

        public Task<IActionResult> OnPostGridDataAsync()
            => GridDataCoreAsync();

        public Task<IActionResult> OnGetGridDataAsync()
            => GridDataCoreAsync();

        private async Task<IActionResult> GridDataCoreAsync()
        {
            var draw = int.TryParse(Param("draw"), out var dVal) ? dVal : 1;
            var start = int.TryParse(Param("start"), out var sVal) ? sVal : 0;
            var length = int.TryParse(Param("length"), out var lVal) ? lVal : 10;
            var searchValue = Param("search[value]");

            var filterHash = Param("filterHash");
            var filterLockout = Param("filterLockout");
            var filterCondicion = Param("filterCondicion");
            var filterPais = Param("filterPais");
            var filterScoring = Param("filterScoring");

            var orderColumn = Param("order[0][column]");
            var orderDir = Param("order[0][dir]");

            // Field names in JS columns[] must be same order!
            string[] columnNames = { "email", "userName", "nombre", "avatar", "fechaRegistro", "condicion", "pais", "hashIsValid", "isLockedOut" };

            var usersQuery = _userManager.Users.AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                usersQuery = usersQuery.Where(u => u.Email.Contains(searchValue) || u.UserName.Contains(searchValue));
            }

            // ⭐ NUEVO: Aplicar filtros personalizados
            if (!string.IsNullOrWhiteSpace(filterHash))
            {
                bool hashValid = filterHash == "true";
                usersQuery = usersQuery.Where(u => 
                    (hashValid && u.PasswordHash != null && u.PasswordHash.Length >= 50 && u.PasswordHash.StartsWith("AQAAAA")) ||
                    (!hashValid && (u.PasswordHash == null || u.PasswordHash.Length < 50 || !u.PasswordHash.StartsWith("AQAAAA")))
                );
            }

            if (!string.IsNullOrWhiteSpace(filterLockout))
            {
                bool isLocked = filterLockout == "true";
                if (isLocked)
                {
                    usersQuery = usersQuery.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow);
                }
                else
                {
                    usersQuery = usersQuery.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow);
                }
            }

            // Filtro por condición (solo padres) - incluye hijos
            if (!string.IsNullOrWhiteSpace(filterCondicion) && int.TryParse(filterCondicion, out int condicionId))
            {
                // ⭐ REPLICAR LÓGICA DEL MAPA: Incluir condición padre + todas las hijas
                var allConditions = await _db.condiciones
                    .AsNoTracking()
                    .Where(c => !c.Eliminado)
                    .Select(c => new { c.id, c.idPadre })
                    .ToListAsync();

                var acceptedConditionIds = new HashSet<int>();
                var queue = new Queue<int>();
                queue.Enqueue(condicionId);
                acceptedConditionIds.Add(condicionId);

                // Recorrer recursivamente para encontrar todos los hijos
                while (queue.Count > 0)
                {
                    var parent = queue.Dequeue();
                    var children = allConditions.Where(c => c.idPadre.HasValue && c.idPadre.Value == parent).Select(c => c.id);
                    foreach (var child in children)
                    {
                        if (acceptedConditionIds.Add(child))
                        {
                            queue.Enqueue(child);
                        }
                    }
                }

                var acceptedIds = acceptedConditionIds.ToList();
                usersQuery = usersQuery.Where(u => 
                    _db.condicionUsuario.Any(cu => 
                        cu.idUsuario == u.Id && 
                        !cu.Eliminado && 
                        acceptedIds.Contains(cu.idCondicion.Value)
                    )
                );
            }

            // Filtro por país
            if (!string.IsNullOrWhiteSpace(filterPais))
            {
                usersQuery = usersQuery.Where(u => 
                    _db.Perfil.Any(p => p.idUser == u.Id && p.NombrePais == filterPais)
                );
            }

            var recordsFiltered = await usersQuery.CountAsync();

            // ⭐ FILTRO POR SCORING: Calcular en memoria (EF no puede traducir expresiones complejas)
            if (!string.IsNullOrWhiteSpace(filterScoring))
            {
                var now = DateTime.UtcNow;
                var minValidDate = new DateTime(1900, 1, 1);

                // PASO 1: Traer los datos básicos a memoria
                var scoredUsers = await (from u in usersQuery
                                         join p in _db.Perfil on u.Id equals p.idUser into perfilGroup
                                         from perfil in perfilGroup.DefaultIfEmpty()
                                         select new
                                         {
                                             UserId = u.Id,
                                             Avatar = perfil != null ? perfil.Avatar : null,
                                             Country = perfil != null ? perfil.NombrePais : null,
                                             Lat = perfil != null ? perfil.Latitud : null,
                                             Lng = perfil != null ? perfil.Longitud : null,

                                             CondFechaInicio = (DateTime?)_db.condicionUsuario
                                                 .Where(cu => !cu.Eliminado && cu.idUsuario == u.Id && cu.fechaInicio != null && cu.fechaInicio > minValidDate && cu.fechaInicio <= now)
                                                 .OrderByDescending(cu => cu.fechaInicio)
                                                 .Select(cu => (DateTime?)cu.fechaInicio)
                                                 .FirstOrDefault(),

                                             CondId = (int?)_db.condicionUsuario
                                                 .Where(cu => !cu.Eliminado && cu.idUsuario == u.Id)
                                                 .OrderByDescending(cu => cu.fechaInicio)
                                                 .Select(cu => (int?)cu.idCondicion)
                                                 .FirstOrDefault(),

                                             LastMoodDate = (DateTime?)_db.EstadoAnimoUsuario
                                                 .Where(m => !m.Eliminado && m.IdUsuario == u.Id)
                                                 .OrderByDescending(m => m.FechaRegistro)
                                                 .Select(m => (DateTime?)m.FechaRegistro)
                                                 .FirstOrDefault()
                                         }).ToListAsync();

                // PASO 2: Calcular scoring EN MEMORIA (del lado del cliente)
                var scoredWithCalc = scoredUsers.Select(x => new
                {
                    x.UserId,
                    Score = ((x.CondFechaInicio.HasValue ? 1 : 0) * 80)
                            + ((!string.IsNullOrEmpty(x.Avatar) && 
                                !x.Avatar.ToLower().Contains("default") && 
                                !x.Avatar.ToLower().Contains("ui-avatars.com")) ? 40 : 0)
                            + ((x.LastMoodDate.HasValue ? 1 : 0) * 30)
                            + ((x.CondId.HasValue ? 1 : 0) * 30)
                            + ((!string.IsNullOrEmpty(x.Country)) ? 20 : 0)
                            + ((!string.IsNullOrEmpty(x.Lat) && !string.IsNullOrEmpty(x.Lng)) ? 10 : 0)
                }).ToList();

                // PASO 3: Filtrar según el criterio
                List<Guid> filteredUserIds;
                if (filterScoring == "completos") // >= 180
                {
                    filteredUserIds = scoredWithCalc.Where(x => x.Score >= 180).Select(x => x.UserId).ToList();
                }
                else if (filterScoring == "basicos") // 80-179
                {
                    filteredUserIds = scoredWithCalc.Where(x => x.Score >= 80 && x.Score < 180).Select(x => x.UserId).ToList();
                }
                else if (filterScoring == "minimos") // < 80
                {
                    filteredUserIds = scoredWithCalc.Where(x => x.Score < 80).Select(x => x.UserId).ToList();
                }
                else
                {
                    filteredUserIds = new List<Guid>();
                }

                // PASO 4: Aplicar filtro a la query principal
                if (filteredUserIds.Any())
                {
                    usersQuery = usersQuery.Where(u => filteredUserIds.Contains(u.Id));
                    recordsFiltered = filteredUserIds.Count;
                }
                else
                {
                    // Si no hay usuarios que cumplan el criterio, devolver vacío
                    return new JsonResult(new
                    {
                        draw,
                        recordsTotal = await _userManager.Users.CountAsync(),
                        recordsFiltered = 0,
                        data = new List<object>()
                    });
                }
            }

            // PROYECCIÓN SIMPLIFICADA: JOIN con Perfil solamente
            var listQuery = from u in usersQuery
                            join p in _db.Perfil on u.Id equals p.idUser into perfilGroup
                            from perfil in perfilGroup.DefaultIfEmpty()
                            select new
                            {
                                id = u.Id,
                                email = u.Email,
                                userName = u.UserName,
                                nombre = perfil != null ? perfil.Nombre : null,
                                avatar = perfil != null ? perfil.Avatar : null,
                                fechaRegistro = perfil != null ? perfil.FechaCreacion : (DateTime?)null,
                                pais = perfil != null ? perfil.NombrePais : null,
                                hashIsValid = u.PasswordHash != null && u.PasswordHash.Length >= 50 && u.PasswordHash.StartsWith("AQAAAA"),
                                isLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow
                            };

            // Ordenamiento sobre la proyección
            if (int.TryParse(orderColumn, out int colIndex))
            {
                switch (columnNames.ElementAtOrDefault(colIndex)?.ToLowerInvariant())
                {
                    case "email":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.email) : listQuery.OrderBy(u => u.email);
                        break;
                    case "username":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.userName) : listQuery.OrderBy(u => u.userName);
                        break;
                    case "nombre":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.nombre) : listQuery.OrderBy(u => u.nombre);
                        break;
                    case "avatar":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.avatar) : listQuery.OrderBy(u => u.avatar);
                        break;
                    case "fecharegistro":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.fechaRegistro) : listQuery.OrderBy(u => u.fechaRegistro);
                        break;
                    case "pais":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.pais) : listQuery.OrderBy(u => u.pais);
                        break;
                    case "hashisvalid":
                        listQuery = orderDir == "desc"
                            ? listQuery.OrderByDescending(u => u.hashIsValid)
                            : listQuery.OrderBy(u => u.hashIsValid);
                        break;
                    case "islockedout":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.isLockedOut) : listQuery.OrderBy(u => u.isLockedOut);
                        break;
                    default:
                        listQuery = listQuery.OrderByDescending(u => u.fechaRegistro);
                        break;
                }
            }
            else
            {
                listQuery = listQuery.OrderByDescending(u => u.fechaRegistro);
            }

            // Paging - TRAER A MEMORIA PRIMERO
            var pagedData = await listQuery
                .Skip(start)
                .Take(length)
                .ToListAsync();

            // OBTENER CONDICIONES EN MEMORIA (para cada usuario en la página)
            var userIds = pagedData.Select(x => x.id).ToList();
            var condicionesUsuario = await _db.condicionUsuario
                .Where(cu => !cu.Eliminado && userIds.Contains(cu.idUsuario))
                .OrderByDescending(cu => cu.fechaInicio ?? cu.fechaCreado)
                .GroupBy(cu => cu.idUsuario)
                .Select(g => new
                {
                    UserId = g.Key,
                    CondicionId = g.FirstOrDefault().idCondicion
                })
                .ToListAsync();

            var condicionIds = condicionesUsuario.Where(x => x.CondicionId.HasValue).Select(x => x.CondicionId.Value).Distinct().ToList();

            var condiciones = await _db.condiciones
                .Where(c => !c.Eliminado && condicionIds.Contains(c.id))
                .Select(c => new
                {
                    c.id,
                    c.nombre,
                    c.idPadre
                })
                .ToListAsync();

            var condicionesPadre = await _db.condiciones
                .Where(c => !c.Eliminado && condiciones.Select(x => x.idPadre).Contains(c.id))
                .Select(c => new
                {
                    c.id,
                    c.nombre
                })
                .ToListAsync();

            // COMBINAR DATOS EN MEMORIA
            var paged = pagedData.Select(u =>
            {
                var condUsuario = condicionesUsuario.FirstOrDefault(cu => cu.UserId == u.id);
                string nombreCondicion = null;

                if (condUsuario?.CondicionId.HasValue == true)
                {
                    var condicion = condiciones.FirstOrDefault(c => c.id == condUsuario.CondicionId.Value);
                    if (condicion != null)
                    {
                        if (condicion.idPadre.HasValue)
                        {
                            var padre = condicionesPadre.FirstOrDefault(cp => cp.id == condicion.idPadre.Value);
                            nombreCondicion = padre?.nombre ?? condicion.nombre;
                        }
                        else
                        {
                            nombreCondicion = condicion.nombre;
                        }
                    }
                }

                return new
                {
                    u.id,
                    u.email,
                    u.userName,
                    u.nombre,
                    u.avatar,
                    u.fechaRegistro,
                    condicion = nombreCondicion,
                    u.pais,
                    u.hashIsValid,
                    u.isLockedOut
                };
            }).ToList();

            return new JsonResult(new
            {
                draw,
                recordsTotal = await _userManager.Users.CountAsync(),
                recordsFiltered,
                data = paged
            });
        }

        // Ya no se usa IsHashValid, pero lo dejo si lo necesitas en el futuro.
        private static bool IsHashValid(string hash) =>
            !string.IsNullOrEmpty(hash)
            && hash.Length >= 50
            && hash.StartsWith("AQAAAA");

        public async Task<IActionResult> OnPostFillMissingSlugsAsync()
        {
            try
            {
                var toFix = await _db.Perfil
                    .Where(p => string.IsNullOrWhiteSpace(p.slug) && !string.IsNullOrWhiteSpace(p.Nombre))
                    .ToListAsync();

                var modified = 0;
                foreach (var perfil in toFix)
                {
                    var baseText = perfil.Nombre + (string.IsNullOrWhiteSpace(perfil.Apellidos) ? "" : " " + perfil.Apellidos);
                    var baseSlug = eiibd26.Helpers.SlugHelper.GenerateSlug(baseText);
                    var slug = baseSlug;
                    var counter = 1;
                    while (await _db.Perfil.AsNoTracking().AnyAsync(x => x.slug == slug))
                    {
                        slug = $"{baseSlug}-{counter}";
                        counter++;
                    }

                    perfil.slug = slug;
                    perfil.FechaModificado = DateTime.UtcNow;
                    modified++;
                }

                if (modified > 0)
                    await _db.SaveChangesAsync();

                return new JsonResult(new { modified });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generando slugs para usuarios");
                return new JsonResult(new { error = ex.Message });
            }
        }

        // ⭐ NUEVO: Obtener condiciones padre para filtro
        [IgnoreAntiforgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> OnGetCondicionesPadreAsync()
        {
            var condiciones = await _db.condiciones
                .Where(c => !c.Eliminado && c.idPadre == null)
                .OrderBy(c => c.nombre)
                .Select(c => new { id = c.id, nombre = c.nombre })
                .ToListAsync();

            return new JsonResult(condiciones);
        }

        // ⭐ NUEVO: Obtener países para filtro
        [IgnoreAntiforgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> OnGetPaisesAsync()
        {
            var paises = await _db.Perfil
                .Where(p => !string.IsNullOrWhiteSpace(p.NombrePais))
                .Select(p => p.NombrePais)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            return new JsonResult(paises);
        }

        // ⭐ NUEVO: Obtener estadísticas basadas en SCORING de perfiles (igual que Mapa)
        [IgnoreAntiforgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> OnGetEstadisticasAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var minValidDate = new DateTime(1900, 1, 1);

                // Obtener rol Paciente (igual que el Mapa)
                var pacienteRoleId = await _db.Roles
                    .Where(r => r.Name == "Paciente")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (pacienteRoleId == Guid.Empty)
                {
                    return new JsonResult(new { total = 0, perfilesCompletos = 0, perfilesBasicos = 0, perfilesMínimos = 0 });
                }

                // Usuarios con rol Paciente que tienen perfil con ubicación válida
                var basePerfil = _db.Perfil.AsNoTracking()
                    .Where(p =>
                        !string.IsNullOrWhiteSpace(p.Latitud) &&
                        !string.IsNullOrWhiteSpace(p.Longitud) &&
                        _db.UserRoles.Any(ur => ur.UserId == p.idUser && ur.RoleId == pacienteRoleId)
                    );

                // Proyección con SCORING (igual que Mapa)
                var projectedQuery = basePerfil.Select(p => new
                {
                    idUser = p.idUser,
                    avatar = p.Avatar,
                    country = p.NombrePais,
                    lat = p.Latitud,
                    lng = p.Longitud,

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

                // Calcular flags para scoring
                var scoredQuery = projectedQuery.Select(x => new
                {
                    x.idUser,
                    hasCond = x.condId.HasValue ? 1 : 0,
                    avatarReal = ((x.avatar != null) && !x.avatar.ToLower().Contains("default") && !x.avatar.ToLower().Contains("ui-avatars.com")) ? 1 : 0,
                    hasLastMood = x.lastMoodDate.HasValue ? 1 : 0,
                    hasCountry = (x.country != null && x.country != "") ? 1 : 0,
                    hasLocation = (x.lat != null && x.lat != "" && x.lng != null && x.lng != "") ? 1 : 0,
                    hasDiagnosis = x.condFechaInicio.HasValue ? 1 : 0
                })
                .Select(x => new
                {
                    x.idUser,
                    // SCORING EXACTO DEL MAPA
                    score = (x.hasDiagnosis * 80)
                            + (x.avatarReal * 40)
                            + (x.hasLastMood * 30)
                            + (x.hasCond * 30)
                            + (x.hasCountry * 20)
                            + (x.hasLocation * 10)
                });

                var scores = await scoredQuery.ToListAsync();

                var total = scores.Count;
                var perfilesCompletos = scores.Count(x => x.score >= 180);  // 🏆 Completos
                var perfilesBasicos = scores.Count(x => x.score >= 80 && x.score < 180);  // 📊 Básicos
                var perfilesMínimos = scores.Count(x => x.score < 80);  // ⚠️ Mínimos

                return new JsonResult(new
                {
                    total,
                    perfilesCompletos,
                    perfilesBasicos,
                    perfilesMinimos = perfilesMínimos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de usuarios con scoring");
                return new JsonResult(new { total = 0, perfilesCompletos = 0, perfilesBasicos = 0, perfilesMinimos = 0 });
            }
        }

        public record EnviarCorreoInput(string UserId);

        /// <summary>
        /// Sends a password reset email to the user using the SendGrid Dynamic Template.
        /// </summary>
        public async Task<IActionResult> OnPostEnviarCorreoAsync([FromBody] EnviarCorreoInput input)
        {
            if (input is null || string.IsNullOrWhiteSpace(input.UserId))
                return new JsonResult(new { success = false, error = "userId requerido." }) { StatusCode = 400 };

            if (!Guid.TryParse(input.UserId, out var userId))
                return new JsonResult(new { success = false, error = "userId inválido." }) { StatusCode = 400 };

            var user = await _userManager.FindByIdAsync(input.UserId);
            if (user is null)
                return new JsonResult(new { success = false, error = "Usuario no encontrado." }) { StatusCode = 404 };

            // Obtener nombre del perfil y condición desde la DB
            var perfil = await _db.Perfil.AsNoTracking()
                .FirstOrDefaultAsync(p => p.idUser == userId);

            var condUsuario = await _db.condicionUsuario
                .Where(cu => !cu.Eliminado && cu.idUsuario == userId)
                .OrderByDescending(cu => cu.fechaInicio ?? cu.fechaCreado)
                .FirstOrDefaultAsync();

            string nombreCondicion = null;
            if (condUsuario?.idCondicion.HasValue == true)
            {
                var condicion = await _db.condiciones
                    .AsNoTracking()
                    .Where(c => !c.Eliminado && c.id == condUsuario.idCondicion.Value)
                    .Select(c => new { c.nombre, c.idPadre })
                    .FirstOrDefaultAsync();

                if (condicion is not null)
                {
                    if (condicion.idPadre.HasValue)
                    {
                        var padre = await _db.condiciones
                            .AsNoTracking()
                            .Where(c => c.id == condicion.idPadre.Value)
                            .Select(c => c.nombre)
                            .FirstOrDefaultAsync();
                        nombreCondicion = padre ?? condicion.nombre;
                    }
                    else
                    {
                        nombreCondicion = condicion.nombre;
                    }
                }
            }

            // Generar token de reseteo con Identity
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Construir enlace de reseteo
            var resetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = encodedToken, email = user.Email },
                protocol: Request.Scheme);

            var templateData = new
            {
                nombre = perfil?.Nombre ?? user.UserName,
                correo = user.Email,
                condicion = nombreCondicion ?? "Sin condición registrada",
                reset_link = resetLink
            };

            await _emailSender.SendDynamicTemplateAsync(
                user.Email,
                templateId: "d-3304c970e0164cbdaa14bfeff2369920",
                templateData: templateData,
                categories: new[] { "EIIBD" });

            _logger.LogInformation("Correo de reseteo enviado al usuario {Email} por administrador.", user.Email);

            return new JsonResult(new { success = true });
        }
    }
}