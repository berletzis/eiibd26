using eiibd26.Data;
using eiibd26.Models.Validacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Services.Validacion
{
    public class ValidacionContenidoService : IValidacionContenidoService
    {
        private readonly ApplicationDbContext _db;
        private readonly eiibd26.Services.Medico.IMedicoBadgeService _badgeService;
        private readonly ILogger<ValidacionContenidoService> _logger;

        public ValidacionContenidoService(
            ApplicationDbContext db,
            eiibd26.Services.Medico.IMedicoBadgeService badgeService,
            ILogger<ValidacionContenidoService> logger)
        {
            _db = db;
            _badgeService = badgeService;
            _logger = logger;
        }

        public async Task<UpsertResult> GuardarValidacionAsync(
            TipoContenidoValidado tipo,
            int contenidoId,
            string usuarioMedicoId,
            string? comentario)
        {
            try
            {
                var existente = await _db.ValidacionesContenidoProfesional
                    .FirstOrDefaultAsync(v =>
                        v.TipoContenido == tipo
                        && v.ContenidoId == contenidoId
                        && v.UsuarioMedicoId == usuarioMedicoId);

                UpsertResult result;

                if (existente == null)
                {
                    _db.ValidacionesContenidoProfesional.Add(new ValidacionContenidoProfesional
                    {
                        TipoContenido   = tipo,
                        ContenidoId     = contenidoId,
                        UsuarioMedicoId = usuarioMedicoId,
                        Comentario      = comentario?.Trim(),
                        Estado          = EstadoValidacion.Validado,
                        CreadoEn        = DateTime.UtcNow
                    });
                    result = UpsertResult.Creada;
                }
                else
                {
                    var nuevoComentario = comentario?.Trim();
                    if (existente.Comentario == nuevoComentario)
                        return UpsertResult.SinCambios;

                    existente.Comentario    = nuevoComentario;
                    existente.ActualizadoEn = DateTime.UtcNow;
                    result = UpsertResult.Actualizada;
                }

                await _db.SaveChangesAsync();

                try
                {
                    if (Guid.TryParse(usuarioMedicoId, out var userGuid))
                    {
                        var perfilMedico = await _db.MedicosPerfilExtendido
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.UserId == userGuid && p.MedicoId != null);
                        if (perfilMedico?.MedicoId != null)
                            await _badgeService.EvaluarBadgesAutomaticosAsync(perfilMedico.MedicoId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudieron evaluar badges para usuario {UserId}", usuarioMedicoId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar validación ({Tipo}, {ContenidoId}) para {UserId}", tipo, contenidoId, usuarioMedicoId);
                return UpsertResult.Error;
            }
        }

        public async Task<ValidacionExistenteDto?> ObtenerMiValidacionAsync(
            TipoContenidoValidado tipo,
            int contenidoId,
            string usuarioMedicoId)
        {
            var v = await _db.ValidacionesContenidoProfesional
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TipoContenido == tipo
                    && x.ContenidoId == contenidoId
                    && x.UsuarioMedicoId == usuarioMedicoId);

            if (v == null) return null;

            return new ValidacionExistenteDto
            {
                Id         = v.Id,
                Comentario = v.Comentario,
                Estado     = v.Estado
            };
        }

        public async Task<List<ValidacionPublicaDto>> ObtenerValidacionesPublicasAsync(
            TipoContenidoValidado tipo,
            int contenidoId)
        {
            var validaciones = await _db.ValidacionesContenidoProfesional
                .AsNoTracking()
                .Where(v =>
                    v.TipoContenido == tipo
                    && v.ContenidoId == contenidoId
                    && v.Estado == EstadoValidacion.Validado)
                .OrderByDescending(v => v.CreadoEn)
                .Select(v => new { v.Id, v.UsuarioMedicoId, v.Comentario, v.CreadoEn, v.ActualizadoEn })
                .ToListAsync();

            if (!validaciones.Any())
                return new List<ValidacionPublicaDto>();

            var userGuidList = validaciones
                .Select(v => Guid.TryParse(v.UsuarioMedicoId, out var g) ? (Guid?)g : null)
                .Where(g => g.HasValue).Select(g => g!.Value).Distinct().ToList();

            var perfiles = await _db.MedicosPerfilExtendido
                .AsNoTracking()
                .Where(p => p.UserId.HasValue && userGuidList.Contains(p.UserId.Value) && p.MedicoId != null)
                .Select(p => new { p.UserId, p.MedicoId })
                .ToListAsync();

            var medicoIds = perfiles.Where(p => p.MedicoId.HasValue).Select(p => p.MedicoId!.Value).ToList();

            var badgesVerificados = await _db.MedicosPerfilBadge
                .AsNoTracking()
                .Where(pb => medicoIds.Contains(pb.MedicoId))
                .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id, (pb, b) => new { pb.MedicoId, b.Codigo })
                .Where(x => x.Codigo == "perfil_reclamado" || x.Codigo == "verificado")
                .Select(x => x.MedicoId)
                .Distinct()
                .ToListAsync();

            var nombresMedico = await _db.MedicosDirectorio
                .AsNoTracking()
                .Where(m => medicoIds.Contains(m.Id))
                .Select(m => new { m.Id, m.NombreCompleto })
                .ToListAsync();

            var nombreDict = nombresMedico.ToDictionary(m => m.Id, m => m.NombreCompleto);

            var medicoIdByUser = perfiles.Where(p => p.MedicoId.HasValue)
                .ToDictionary(p => p.UserId!.Value, p => p.MedicoId!.Value);

            var avatarDict = await _db.Perfil
                .AsNoTracking()
                .Where(p => userGuidList.Contains(p.idUser))
                .Select(p => new { p.idUser, p.Avatar })
                .ToListAsync();
            var avatarByUser = avatarDict.ToDictionary(p => p.idUser, p => p.Avatar);

            var result = new List<ValidacionPublicaDto>();

            foreach (var v in validaciones)
            {
                if (!Guid.TryParse(v.UsuarioMedicoId, out var guid)) continue;

                string display;
                if (medicoIdByUser.TryGetValue(guid, out var medicoId)
                    && badgesVerificados.Contains(medicoId)
                    && nombreDict.TryGetValue(medicoId, out var nombre))
                    display = $"Dr. {nombre}";
                else
                    display = "Médico verificado";

                string? avatarUrl = null;
                if (avatarByUser.TryGetValue(guid, out var avatarVal)
                    && !string.IsNullOrWhiteSpace(avatarVal) && avatarVal != "default.jpg")
                    avatarUrl = avatarVal.StartsWith("/") ? avatarVal : "/" + avatarVal;

                result.Add(new ValidacionPublicaDto
                {
                    Id           = v.Id,
                    UserDisplay  = display,
                    AvatarUrl    = avatarUrl,
                    Comentario   = v.Comentario,
                    CreadoEn     = v.CreadoEn,
                    ActualizadoEn = v.ActualizadoEn
                });
            }

            return result;
        }

        public async Task<bool> CambiarEstadoAsync(
            int validacionId,
            EstadoValidacion nuevoEstado,
            string adminUserId,
            string? nota = null)
        {
            try
            {
                var validacion = await _db.ValidacionesContenidoProfesional
                    .FirstOrDefaultAsync(v => v.Id == validacionId);

                if (validacion == null) return false;

                var medicoUserId = validacion.UsuarioMedicoId;
                validacion.Estado           = nuevoEstado;
                validacion.ModeradoPorId    = adminUserId;
                validacion.FechaModeracion  = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(nota))
                    validacion.NotaModeracion = nota;

                await _db.SaveChangesAsync();

                try
                {
                    if (Guid.TryParse(medicoUserId, out var userGuid))
                    {
                        var perfilMedico = await _db.MedicosPerfilExtendido
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.UserId == userGuid && p.MedicoId != null);
                        if (perfilMedico?.MedicoId != null)
                            await _badgeService.EvaluarBadgesAutomaticosAsync(perfilMedico.MedicoId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudieron re-evaluar badges tras cambio de estado");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de validación {Id}", validacionId);
                return false;
            }
        }

        public async Task<List<ValidacionAdminDto>> ObtenerValidacionesMedicoAsync(string usuarioMedicoId)
        {
            var validaciones = await _db.ValidacionesContenidoProfesional
                .AsNoTracking()
                .Where(v => v.UsuarioMedicoId == usuarioMedicoId)
                .OrderByDescending(v => v.CreadoEn)
                .ToListAsync();

            if (!validaciones.Any())
                return new List<ValidacionAdminDto>();

            var terminoIds = validaciones
                .Where(v => v.TipoContenido == TipoContenidoValidado.Termino)
                .Select(v => v.ContenidoId).Distinct().ToList();

            var articuloIds = validaciones
                .Where(v => v.TipoContenido == TipoContenidoValidado.Articulo)
                .Select(v => v.ContenidoId).Distinct().ToList();

            var perfilMedicoIds = validaciones
                .Where(v => v.TipoContenido == TipoContenidoValidado.PerfilMedico)
                .Select(v => v.ContenidoId).Distinct().ToList();

            var terminoTitulos = terminoIds.Any()
                ? await _db.GlossaryTerms
                    .AsNoTracking()
                    .Where(t => terminoIds.Contains(t.Id))
                    .Select(t => new { t.Id, t.Nombre, t.Slug })
                    .ToListAsync()
                : new();

            var articuloTitulos = articuloIds.Any()
                ? await _db.Contenidos
                    .AsNoTracking()
                    .Where(c => articuloIds.Contains(c.Id) && !c.Eliminado)
                    .Select(c => new { c.Id, Nombre = c.ContenidoTitulo ?? "", c.ContenidoTituloSlug })
                    .ToListAsync()
                : new();

            var medicoNombres = perfilMedicoIds.Any()
                ? await _db.MedicosDirectorio
                    .AsNoTracking()
                    .Where(m => perfilMedicoIds.Contains(m.Id))
                    .Select(m => new { m.Id, Nombre = m.NombreCompleto })
                    .ToListAsync()
                : new();

            var terminoDict      = terminoTitulos.ToDictionary(t => t.Id);
            var articuloDict     = articuloTitulos.ToDictionary(t => t.Id);
            var medicoNombreDict = medicoNombres.ToDictionary(m => m.Id, m => m.Nombre);

            return validaciones.Select(v =>
            {
                string titulo = "";
                string? url   = null;

                if (v.TipoContenido == TipoContenidoValidado.Termino
                    && terminoDict.TryGetValue(v.ContenidoId, out var t))
                {
                    titulo = t.Nombre;
                    url    = $"/Termino/{t.Slug}";
                }
                else if (v.TipoContenido == TipoContenidoValidado.Articulo
                    && articuloDict.TryGetValue(v.ContenidoId, out var a))
                {
                    titulo = a.Nombre;
                    url    = $"/Contenidos/Detalle/{a.ContenidoTituloSlug}";
                }
                else if (v.TipoContenido == TipoContenidoValidado.PerfilMedico
                    && medicoNombreDict.TryGetValue(v.ContenidoId, out var nombre))
                {
                    titulo = nombre;
                    url    = $"/DirectorioMedicos/Detalle/{v.ContenidoId}";
                }

                return new ValidacionAdminDto
                {
                    Id              = v.Id,
                    TipoContenido   = v.TipoContenido,
                    ContenidoId     = v.ContenidoId,
                    ContenidoTitulo = titulo,
                    ContenidoUrl    = url,
                    Comentario      = v.Comentario,
                    Estado          = v.Estado,
                    CreadoEn        = v.CreadoEn,
                    ActualizadoEn   = v.ActualizadoEn,
                    NotaModeracion  = v.NotaModeracion
                };
            }).ToList();
        }
    }
}
