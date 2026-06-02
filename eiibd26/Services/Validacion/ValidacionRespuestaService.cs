using eiibd26.Data;
using eiibd26.Models.Validacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Services.Validacion
{
    public class ValidacionRespuestaService : IValidacionRespuestaService
    {
        private readonly ApplicationDbContext _db;
        private readonly eiibd26.Services.Medico.IMedicoBadgeService _badgeService;
        private readonly ILogger<ValidacionRespuestaService> _logger;

        public ValidacionRespuestaService(
            ApplicationDbContext db,
            eiibd26.Services.Medico.IMedicoBadgeService badgeService,
            ILogger<ValidacionRespuestaService> logger)
        {
            _db = db;
            _badgeService = badgeService;
            _logger = logger;
        }

        public async Task<UpsertResult> GuardarValidacionAsync(
            Guid respuestaId,
            string usuarioMedicoId,
            string? comentario)
        {
            try
            {
                var existente = await _db.ValidacionesRespuestaProfesional
                    .FirstOrDefaultAsync(v =>
                        v.RespuestaId == respuestaId &&
                        v.UsuarioMedicoId == usuarioMedicoId);

                UpsertResult result;

                if (existente == null)
                {
                    _db.ValidacionesRespuestaProfesional.Add(new ValidacionRespuestaProfesional
                    {
                        RespuestaId     = respuestaId,
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
                _logger.LogError(ex, "Error al guardar validación de respuesta {RespuestaId} para {UserId}", respuestaId, usuarioMedicoId);
                return UpsertResult.Error;
            }
        }

        public async Task<ValidacionExistenteDto?> ObtenerMiValidacionAsync(
            Guid respuestaId,
            string usuarioMedicoId)
        {
            var v = await _db.ValidacionesRespuestaProfesional
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.RespuestaId == respuestaId &&
                    x.UsuarioMedicoId == usuarioMedicoId);

            if (v == null) return null;

            return new ValidacionExistenteDto
            {
                Id         = v.Id,
                Comentario = v.Comentario,
                Estado     = v.Estado
            };
        }

        public async Task<List<ValidacionPublicaDto>> ObtenerValidacionesPublicasAsync(Guid respuestaId)
        {
            var validaciones = await _db.ValidacionesRespuestaProfesional
                .AsNoTracking()
                .Where(v => v.RespuestaId == respuestaId && v.Estado == EstadoValidacion.Validado)
                .OrderByDescending(v => v.CreadoEn)
                .Select(v => new { v.Id, v.UsuarioMedicoId, v.Comentario, v.CreadoEn, v.ActualizadoEn })
                .ToListAsync();

            if (!validaciones.Any()) return new List<ValidacionPublicaDto>();

            return await ResolverPerfilesAsync(validaciones.Select(v =>
                (v.Id, v.UsuarioMedicoId, v.Comentario, v.CreadoEn, v.ActualizadoEn)).ToList());
        }

        public async Task<Dictionary<Guid, List<ValidacionPublicaDto>>> ObtenerValidadoresPorRespuestasAsync(
            List<Guid> respuestaIds)
        {
            if (!respuestaIds.Any()) return new();

            var validaciones = await _db.ValidacionesRespuestaProfesional
                .AsNoTracking()
                .Where(v => respuestaIds.Contains(v.RespuestaId) && v.Estado == EstadoValidacion.Validado)
                .OrderByDescending(v => v.CreadoEn)
                .Select(v => new { v.Id, v.RespuestaId, v.UsuarioMedicoId, v.Comentario, v.CreadoEn, v.ActualizadoEn })
                .ToListAsync();

            if (!validaciones.Any()) return new();

            var userGuidList = validaciones
                .Select(v => Guid.TryParse(v.UsuarioMedicoId, out var g) ? (Guid?)g : null)
                .Where(g => g.HasValue).Select(g => g!.Value).Distinct().ToList();

            var (medicoIdByUser, badgesVerificados, nombreDict, slugByMedico, avatarByUser) =
                await CargarDatosMedicosAsync(userGuidList);

            var result = new Dictionary<Guid, List<ValidacionPublicaDto>>();

            foreach (var v in validaciones)
            {
                if (!Guid.TryParse(v.UsuarioMedicoId, out var guid)) continue;

                var dto = BuildDto(v.Id, guid, v.Comentario, v.CreadoEn, v.ActualizadoEn,
                    medicoIdByUser, badgesVerificados, nombreDict, slugByMedico, avatarByUser);

                if (!result.ContainsKey(v.RespuestaId))
                    result[v.RespuestaId] = new List<ValidacionPublicaDto>();
                result[v.RespuestaId].Add(dto);
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
                var validacion = await _db.ValidacionesRespuestaProfesional
                    .FirstOrDefaultAsync(v => v.Id == validacionId);

                if (validacion == null) return false;

                var medicoUserId = validacion.UsuarioMedicoId;
                validacion.Estado          = nuevoEstado;
                validacion.ModeradoPorId   = adminUserId;
                validacion.FechaModeracion = DateTime.UtcNow;
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
                    _logger.LogWarning(ex, "No se pudieron re-evaluar badges tras cambio de estado de validación respuesta");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de validación respuesta {Id}", validacionId);
                return false;
            }
        }

        // ── Helpers privados ────────────────────────────────────────────────

        private async Task<List<ValidacionPublicaDto>> ResolverPerfilesAsync(
            List<(int Id, string UsuarioMedicoId, string? Comentario, DateTime CreadoEn, DateTime? ActualizadoEn)> rows)
        {
            var userGuidList = rows
                .Select(v => Guid.TryParse(v.UsuarioMedicoId, out var g) ? (Guid?)g : null)
                .Where(g => g.HasValue).Select(g => g!.Value).Distinct().ToList();

            var (medicoIdByUser, badgesVerificados, nombreDict, slugByMedico, avatarByUser) =
                await CargarDatosMedicosAsync(userGuidList);

            var result = new List<ValidacionPublicaDto>();
            foreach (var v in rows)
            {
                if (!Guid.TryParse(v.UsuarioMedicoId, out var guid)) continue;
                result.Add(BuildDto(v.Id, guid, v.Comentario, v.CreadoEn, v.ActualizadoEn,
                    medicoIdByUser, badgesVerificados, nombreDict, slugByMedico, avatarByUser));
            }
            return result;
        }

        private async Task<(Dictionary<Guid, int> medicoIdByUser,
                            List<int> badgesVerificados,
                            Dictionary<int, string> nombreDict,
                            Dictionary<int, string?> slugByMedico,
                            Dictionary<Guid, string?> avatarByUser)>
            CargarDatosMedicosAsync(List<Guid> userGuidList)
        {
            var perfiles = await _db.MedicosPerfilExtendido
                .AsNoTracking()
                .Where(p => p.UserId.HasValue && userGuidList.Contains(p.UserId.Value) && p.MedicoId != null)
                .Select(p => new { p.UserId, p.MedicoId, p.Slug })
                .ToListAsync();

            var medicoIds = perfiles.Where(p => p.MedicoId.HasValue).Select(p => p.MedicoId!.Value).ToList();

            var badgesVerificados = medicoIds.Any()
                ? await _db.MedicosPerfilBadge
                    .AsNoTracking()
                    .Where(pb => medicoIds.Contains(pb.MedicoId))
                    .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id, (pb, b) => new { pb.MedicoId, b.Codigo })
                    .Where(x => x.Codigo == "perfil_reclamado" || x.Codigo == "verificado")
                    .Select(x => x.MedicoId).Distinct().ToListAsync()
                : new List<int>();

            var nombresMedico = medicoIds.Any()
                ? await _db.MedicosDirectorio
                    .AsNoTracking()
                    .Where(m => medicoIds.Contains(m.Id))
                    .Select(m => new { m.Id, m.NombreCompleto })
                    .ToListAsync()
                : new();

            var avatarDict = await _db.Perfil
                .AsNoTracking()
                .Where(p => userGuidList.Contains(p.idUser))
                .Select(p => new { p.idUser, p.Avatar })
                .ToListAsync();

            return (
                perfiles.Where(p => p.MedicoId.HasValue).ToDictionary(p => p.UserId!.Value, p => p.MedicoId!.Value),
                badgesVerificados,
                nombresMedico.ToDictionary(m => m.Id, m => m.NombreCompleto),
                perfiles.Where(p => p.MedicoId.HasValue && p.Slug != null).ToDictionary(p => p.MedicoId!.Value, p => p.Slug),
                avatarDict.ToDictionary(p => p.idUser, p => p.Avatar)
            );
        }

        private static ValidacionPublicaDto BuildDto(
            int id, Guid guid, string? comentario, DateTime creadoEn, DateTime? actualizadoEn,
            Dictionary<Guid, int> medicoIdByUser, List<int> badgesVerificados,
            Dictionary<int, string> nombreDict, Dictionary<int, string?> slugByMedico,
            Dictionary<Guid, string?> avatarByUser)
        {
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

            string? slug = null;
            if (medicoIdByUser.TryGetValue(guid, out var midForSlug))
                slugByMedico.TryGetValue(midForSlug, out slug);

            return new ValidacionPublicaDto
            {
                Id            = id,
                UserDisplay   = display,
                AvatarUrl     = avatarUrl,
                Slug          = slug,
                Comentario    = comentario,
                CreadoEn      = creadoEn,
                ActualizadoEn = actualizadoEn
            };
        }
    }
}
