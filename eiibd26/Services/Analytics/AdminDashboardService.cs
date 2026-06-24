using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Directorio;
using eiibd26.Models.Directorio.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Services.Analytics
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminDashboardService> _logger;

        public AdminDashboardService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminDashboardService> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<AdminDashboardDto> GetDashboardAsync()
        {
            var dto = new AdminDashboardDto();
            var now = DateTime.UtcNow;
            var hace7d = now.AddDays(-7);
            var hace30d = now.AddDays(-30);
            var hace90d = now.AddDays(-90);

            // ── Usuarios ─────────────────────────────────────────────
            // Criterio único de validez (excluye suspendidos) — mismo helper que en los mapas.
            var idsValidos = UsuarioValidez.IdsValidosQuery(_db);
            try
            {
                dto.TotalUsuarios = await _userManager.Users.SoloValidos().CountAsync();
                dto.UsuariosConfirmados = await _userManager.Users.SoloValidos().CountAsync(u => u.EmailConfirmed);
                dto.NuevosEstaSemana = await _db.Perfil.AsNoTracking()
                    .CountAsync(p => p.FechaCreacion >= hace7d && idsValidos.Contains(p.idUser));
                dto.Activos30d = await _db.Perfil.AsNoTracking()
                    .CountAsync(p => p.UltimaActividad != null && p.UltimaActividad >= hace30d && idsValidos.Contains(p.idUser));
                dto.Activos90d = await _db.Perfil.AsNoTracking()
                    .CountAsync(p => p.UltimaActividad != null && p.UltimaActividad >= hace90d && idsValidos.Contains(p.idUser));
            }
            catch (Exception ex) { _logger.LogWarning(ex, "[Dashboard] Error en métricas usuarios"); }

            // Scoring de perfiles (requiere cálculo en memoria — replicado del handler de Usuarios/Index)
            try
            {
                var minValidDate = new DateTime(1900, 1, 1);
                var pacienteRoleId = await _db.Roles
                    .Where(r => r.Name == "Paciente")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (pacienteRoleId != Guid.Empty)
                {
                    var basePerfil = _db.Perfil.AsNoTracking()
                        .Where(p =>
                            !string.IsNullOrWhiteSpace(p.Latitud) &&
                            !string.IsNullOrWhiteSpace(p.Longitud) &&
                            idsValidos.Contains(p.idUser) &&
                            _db.UserRoles.Any(ur => ur.UserId == p.idUser && ur.RoleId == pacienteRoleId));

                    var scores = await basePerfil.Select(p => new
                    {
                        hasDiagnosis = _db.condicionUsuario
                            .Any(cu => !cu.Eliminado && cu.idUsuario == p.idUser &&
                                       cu.fechaInicio != null && cu.fechaInicio > minValidDate && cu.fechaInicio <= now) ? 1 : 0,
                        avatarReal = (p.Avatar != null &&
                                      !p.Avatar.ToLower().Contains("default") &&
                                      !p.Avatar.ToLower().Contains("ui-avatars.com")) ? 1 : 0,
                        hasLastMood = _db.EstadoAnimoUsuario
                            .Any(m => !m.Eliminado && m.IdUsuario == p.idUser) ? 1 : 0,
                        hasCond = _db.condicionUsuario
                            .Any(cu => !cu.Eliminado && cu.idUsuario == p.idUser) ? 1 : 0,
                        hasCountry = (p.NombrePais != null && p.NombrePais != "") ? 1 : 0,
                        hasLocation = (p.Latitud != null && p.Latitud != "" &&
                                       p.Longitud != null && p.Longitud != "") ? 1 : 0
                    })
                    .Select(x => (x.hasDiagnosis * 80) + (x.avatarReal * 40) + (x.hasLastMood * 30) +
                                 (x.hasCond * 30) + (x.hasCountry * 20) + (x.hasLocation * 10))
                    .ToListAsync();

                    dto.PerfilesCompletos = scores.Count(s => s >= 180);
                    dto.PerfilesBasicos = scores.Count(s => s >= 80 && s < 180);
                    dto.PerfilesMinimos = scores.Count(s => s < 80);
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "[Dashboard] Error en scoring perfiles"); }

            // ── Comunidad ─────────────────────────────────────────────
            try
            {
                dto.TotalPreguntas = await _db.Preguntas.AsNoTracking()
                    .CountAsync(p => !p.Eliminado);
                dto.PreguntasEstaSemana = await _db.Preguntas.AsNoTracking()
                    .CountAsync(p => !p.Eliminado && p.FechaCreacion >= hace7d);
                dto.PreguntasSinResponder = await _db.Preguntas.AsNoTracking()
                    .CountAsync(p => !p.Eliminado &&
                        !_db.Respuestas.Any(r => r.PreguntaId == p.Id && !r.EsIA && !r.Eliminado));
                dto.TotalRespuestasHumanas = await _db.Respuestas.AsNoTracking()
                    .CountAsync(r => !r.EsIA && !r.Eliminado);
                dto.TotalRespuestasNINA = await _db.Respuestas.AsNoTracking()
                    .CountAsync(r => r.EsIA && !r.Eliminado);
                dto.TotalVotos = await _db.Votos.AsNoTracking()
                    .CountAsync(v => !v.Eliminado);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "[Dashboard] Error en métricas comunidad"); }

            // ── Salud ─────────────────────────────────────────────────
            try
            {
                dto.RegistrosMoodEstaSemana = await _db.EstadoAnimoUsuario.AsNoTracking()
                    .CountAsync(m => !m.Eliminado && m.FechaRegistro >= hace7d);
                dto.TotalUsuariosConMood = await _db.EstadoAnimoUsuario.AsNoTracking()
                    .Where(m => !m.Eliminado)
                    .Select(m => m.IdUsuario)
                    .Distinct()
                    .CountAsync();
                dto.TotalUsuariosConCondicion = await _db.condicionUsuario.AsNoTracking()
                    .Where(cu => !cu.Eliminado)
                    .Select(cu => cu.idUsuario)
                    .Distinct()
                    .CountAsync();
                dto.TotalTrackingSintomas = await _db.TrackingSintomaUsuario.AsNoTracking()
                    .CountAsync();
            }
            catch (Exception ex) { _logger.LogWarning(ex, "[Dashboard] Error en métricas salud"); }

            // ── Contenidos ────────────────────────────────────────────
            try
            {
                dto.TotalPublicados = await _db.Contenidos.AsNoTracking()
                    .CountAsync(c => !c.Eliminado && c.EstadoPublicacion >= 1);
                dto.TotalBorradores = await _db.Contenidos.AsNoTracking()
                    .CountAsync(c => !c.Eliminado && (c.EstadoPublicacion == null || c.EstadoPublicacion == 0));
                var avgGris = await _db.ContenidoCalidad.AsNoTracking()
                    .Where(q => q.GrisEvaluado && q.GrisPuntajeGlobal != null)
                    .AverageAsync(q => (double?)q.GrisPuntajeGlobal);
                dto.PromedioCalidadGris = avgGris.HasValue ? Math.Round(avgGris.Value, 1) : 0;
            }
            catch (Exception ex) { _logger.LogWarning(ex, "[Dashboard] Error en métricas contenidos"); }

            // ── Médicos ────────────────────────────────────────────────
            try
            {
                dto.TotalDirectorio = await _db.MedicosDirectorio.AsNoTracking()
                    .CountAsync(m => !m.Eliminado);
                dto.MedicosVerificados = await _db.MedicosDirectorio.AsNoTracking()
                    .CountAsync(m => !m.Eliminado &&
                        m.EstatusValidacion == EstatusValidacionCedula.Validado);
                dto.MedicosReclamados = await _db.MedicosDirectorio.AsNoTracking()
                    .CountAsync(m => !m.Eliminado &&
                        m.EstatusReclamacion == EstatusReclamacion.Reclamado);
                dto.TotalConfirmacionesComunitarias = await _db.ConfirmacionesComunitarias.AsNoTracking()
                    .CountAsync(c => !c.Eliminado);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "[Dashboard] Error en métricas médicos"); }

            // ── Marketing ─────────────────────────────────────────────
            try
            {
                dto.TotalShortUrls = await _db.ShortUrls.AsNoTracking()
                    .CountAsync(s => !s.Eliminado);
                dto.TotalClicksShortUrls = await _db.ShortUrls.AsNoTracking()
                    .Where(s => !s.Eliminado)
                    .SumAsync(s => (long)s.ClickCount);
                dto.ClicksEstaSemana = await _db.ShortUrlClicks.AsNoTracking()
                    .CountAsync(c => c.FechaClick >= hace7d);
                dto.TotalPushEnviadas = await _db.PushNotifications.AsNoTracking()
                    .CountAsync(p => p.IsSent);
                dto.CampañasEmailEnviadas = await _db.EmailCampanaLogs.AsNoTracking()
                    .CountAsync(e => e.Exito);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "[Dashboard] Error en métricas marketing"); }

            return dto;
        }
    }
}
