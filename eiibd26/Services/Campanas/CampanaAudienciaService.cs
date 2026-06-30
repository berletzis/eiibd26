using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Campanas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Services.Campanas
{
    /// <summary>
    /// Implementación de la resolución de audiencias. Extraída del PageModel de Campañas
    /// para que el conteo en vivo y el job de envío compartan exactamente la misma lógica
    /// de exclusión (una sola fuente de verdad).
    /// </summary>
    public class CampanaAudienciaService : ICampanaAudienciaService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ICampanaTargetingService _targeting;
        private readonly eiibd26.Services.Email.BounceClasificador _bounceClasificador;

        public CampanaAudienciaService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IConfiguration configuration,
            ICampanaTargetingService targeting,
            eiibd26.Services.Email.BounceClasificador bounceClasificador)
        {
            _userManager = userManager;
            _db = db;
            _configuration = configuration;
            _targeting = targeting;
            _bounceClasificador = bounceClasificador;
        }

        // FaseLog para TodosConfirmados. No colisiona con 1/2/3 (toques de secuencia) ni 0 (reset pw).
        private const int FaseLogGeneral = 10;
        // FaseLog para audiencias de tarea pendiente (20=SinCondicion, 21=SinMood, 22=ConRespuestas, 23=Diagnostico, 24=SinAvatar, 25=CompletarFechaDiag, 26=SinMoodReciente).
        private const int FaseLogSinCondicion  = 20;
        private const int FaseLogSinMood       = 21;
        private const int FaseLogConRespuestas = 22;
        private const int FaseLogDiagnostico   = 23;
        private const int FaseLogSinAvatar     = 24;
        private const int FaseLogCompletarFechaDiag = 25;
        private const int FaseLogSinMoodReciente = 26;

        /// <inheritdoc/>
        public int FaseLogPara(AudienciaCampana audiencia) => audiencia switch
        {
            AudienciaCampana.ViejosSinToque1 => 1,
            AudienciaCampana.Toque2          => 2,
            AudienciaCampana.Toque3          => 3,
            AudienciaCampana.SinCondicion       => FaseLogSinCondicion,
            AudienciaCampana.SinMood            => FaseLogSinMood,
            AudienciaCampana.ConRespuestasSemana  => FaseLogConRespuestas,
            AudienciaCampana.DiagnosticoPendiente => FaseLogDiagnostico,
            AudienciaCampana.SinAvatar            => FaseLogSinAvatar,
            AudienciaCampana.CompletarFechaDiagnostico => FaseLogCompletarFechaDiag,
            AudienciaCampana.SinMoodReciente      => FaseLogSinMoodReciente,
            _                                     => FaseLogGeneral
        };

        /// <inheritdoc/>
        public async Task<(IQueryable<ApplicationUser> query, int faseLog)> BuildAudienciaQueryAsync(
            AudienciaCampana audiencia, string? templateId = null)
        {
            // Criterio único de validez (excluye suspendidos) aplicado al universo base:
            // así TODAS las audiencias quedan filtradas de raíz, sin tocar cada case.
            var users = _userManager.Users.SoloValidos();

            // Excluir direcciones rebotadas (hard / soft reincidente) de TODOS los envíos,
            // igual que el filtro de validez. Se calcula al vuelo desde SendGridEventLog.
            // Cruce por Email normalizado (lower) — los eventos bounce pueden tener UserId null.
            // SOLO afecta envíos: no toca UsuarioValidez, dashboard ni stats de campaña.
            // Al estar en el universo base, el conteo "elegibles" de la UI ya refleja la exclusión.
            var excluidosPorRebote = await _bounceClasificador.ObtenerEmailsExcluidosAsync();
            if (excluidosPorRebote.Count > 0)
            {
                users = users.Where(u => u.Email == null
                                         || !excluidosPorRebote.Contains(u.Email.ToLower()));
            }

            // Excluir cuentas de SISTEMA (NINA, Comunidad) de TODOS los envíos.
            // No son pacientes; nunca deben recibir campañas. GUIDs vienen de config.
            var sistemaIds = new List<Guid>();
            if (Guid.TryParse(_configuration["AiAnswer:SystemUserId"], out var ninaId))
                sistemaIds.Add(ninaId);
            if (Guid.TryParse(_configuration["Comunidad:UserId"], out var comunidadId))
                sistemaIds.Add(comunidadId);
            if (sistemaIds.Count > 0)
                users = users.Where(u => !sistemaIds.Contains(u.Id));

            switch (audiencia)
            {
                case AudienciaCampana.ViejosSinToque1:
                {
                    var yaF1 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 1 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.UsuariosViejos)
                            .Where(u => !yaF1.Contains(u.Id)),
                        1);
                }

                case AudienciaCampana.Toque2:
                {
                    var conF1 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 1 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    var yaF2 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 2 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.UsuariosViejos)
                            .Where(u => conF1.Contains(u.Id) && !yaF2.Contains(u.Id)),
                        2);
                }

                case AudienciaCampana.Toque3:
                {
                    var conF2 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 2 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    var yaF3 = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == 3 && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.UsuariosViejos)
                            .Where(u => conF2.Contains(u.Id) && !yaF3.Contains(u.Id)),
                        3);
                }

                case AudienciaCampana.SinCondicion:
                {
                    // Usuarios que YA tienen condición (para excluirlos del universo)
                    var conCondicion = new HashSet<Guid>(await _db.condicionUsuario
                        .Where(c => !c.Eliminado)
                        .Select(c => c.idUsuario).Distinct().ToListAsync());
                    // Excluir a quienes ya recibieron esta campaña (mismo patrón que ViejosSinToque1).
                    var yaEnviados = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == FaseLogSinCondicion && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados)
                            .Where(u => !conCondicion.Contains(u.Id) && !yaEnviados.Contains(u.Id)),
                        FaseLogSinCondicion);
                }

                case AudienciaCampana.SinMood:
                {
                    // Usuarios que YA registraron mood (para excluirlos del universo)
                    var conMood = new HashSet<Guid>(await _db.EstadoAnimoUsuario
                        .Where(e => !e.Eliminado)
                        .Select(e => e.IdUsuario).Distinct().ToListAsync());
                    // Excluir a quienes ya recibieron esta campaña (mismo patrón que ViejosSinToque1).
                    var yaEnviados = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == FaseLogSinMood && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados)
                            .Where(u => !conMood.Contains(u.Id) && !yaEnviados.Contains(u.Id)),
                        FaseLogSinMood);
                }

                case AudienciaCampana.ConRespuestasSemana:
                {
                    // Usuarios cuyas preguntas recibieron respuesta de OTRO en los últimos 7 días.
                    // Mismo criterio que NewAnswersCount del dashboard.
                    var weekAgo = DateTimeOffset.UtcNow.AddDays(-7);
                    var conRespuestas = new HashSet<Guid>(await _db.Respuestas
                        .Where(r => r.FechaCreacion >= weekAgo && !r.Eliminado)
                        .Join(_db.Preguntas, r => r.PreguntaId, p => p.Id, (r, p) => new { r, p })
                        .Where(x => x.p.UsuarioId != x.r.UsuarioId)
                        .Select(x => x.p.UsuarioId)
                        .Distinct().ToListAsync());
                    // Excluir a quienes ya recibieron esta campaña (mismo patrón que ViejosSinToque1).
                    var yaEnviados = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == FaseLogConRespuestas && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados)
                            .Where(u => conRespuestas.Contains(u.Id) && !yaEnviados.Contains(u.Id)),
                        FaseLogConRespuestas);
                }

                case AudienciaCampana.DiagnosticoPendiente:
                {
                    // Replicar exacto el criterio de NeedsDiagnosisDateUpdate del dashboard:
                    // condicionUsuario.fechaInicio.Date == (Perfil.FechaCreado ?? Perfil.FechaCreacion).Date
                    var condiciones = await _db.condicionUsuario
                        .Where(cu => !cu.Eliminado && cu.fechaInicio != null)
                        .Select(cu => new { cu.idUsuario, cu.fechaInicio })
                        .ToListAsync();

                    var perfiles = await _db.Perfil
                        .AsNoTracking()
                        .Select(p => new { p.idUser, p.FechaCreado, p.FechaCreacion })
                        .ToListAsync();

                    var perfilFechas = perfiles.ToDictionary(
                        p => p.idUser,
                        p => (p.FechaCreado ?? p.FechaCreacion).Date);

                    var diagPendiente = new HashSet<Guid>(
                        condiciones
                            .Where(cu => perfilFechas.TryGetValue(cu.idUsuario, out var perfilDate)
                                         && cu.fechaInicio!.Value.Date == perfilDate)
                            .Select(cu => cu.idUsuario)
                            .Distinct());

                    // Excluir a quienes ya recibieron esta campaña (mismo patrón que ViejosSinToque1).
                    var yaEnviados = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == FaseLogDiagnostico && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados)
                            .Where(u => diagPendiente.Contains(u.Id) && !yaEnviados.Contains(u.Id)),
                        FaseLogDiagnostico);
                }

                case AudienciaCampana.SinAvatar:
                {
                    // Criterio idéntico al scoring de Admin/Usuarios/Index:
                    // sin avatar propio = Avatar null/vacío, o contiene "ui-avatars.com", o contiene "default"
                    var sinAvatar = new HashSet<Guid>(await _db.Perfil
                        .Where(p => string.IsNullOrEmpty(p.Avatar)
                                 || p.Avatar.ToLower().Contains("ui-avatars.com")
                                 || p.Avatar.ToLower().Contains("default"))
                        .Select(p => p.idUser).ToListAsync());
                    // Excluir a quienes ya recibieron esta campaña (mismo patrón que ViejosSinToque1).
                    var yaEnviados = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == FaseLogSinAvatar && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados)
                            .Where(u => sinAvatar.Contains(u.Id) && !yaEnviados.Contains(u.Id)),
                        FaseLogSinAvatar);
                }

                case AudienciaCampana.CompletarFechaDiagnostico:
                {
                    // Usuarios con condición SIN fecha de diagnóstico real:
                    // B = fechaInicio NULL; D = fechaInicio placeholder (1 de enero de cualquier año).
                    var sinFechaReal = new HashSet<Guid>(await _db.condicionUsuario
                        .Where(cu => !cu.Eliminado
                            && (cu.fechaInicio == null
                                || (cu.fechaInicio.Value.Month == 1 && cu.fechaInicio.Value.Day == 1)))
                        .Select(cu => cu.idUsuario).Distinct().ToListAsync());
                    // Excluir a quienes ya recibieron esta campaña (mismo patrón que ViejosSinToque1).
                    var yaEnviados = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == FaseLogCompletarFechaDiag && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados)
                            .Where(u => sinFechaReal.Contains(u.Id) && !yaEnviados.Contains(u.Id)),
                        FaseLogCompletarFechaDiag);
                }

                case AudienciaCampana.SinMoodReciente:
                {
                    var hace14dias = DateTime.UtcNow.AddDays(-14);
                    // Usuarios cuyo ÚLTIMO registro de mood fue hace más de 14 días.
                    // Agrupa por usuario, toma el máximo FechaRegistro, y filtra los que están por debajo del umbral.
                    // Esto EXCLUYE a quienes nunca registraron (esos están en SinMood=6).
                    var ultimoMoodPorUsuario = await _db.EstadoAnimoUsuario
                        .Where(e => !e.Eliminado)
                        .GroupBy(e => e.IdUsuario)
                        .Select(g => new { Usuario = g.Key, Ultimo = g.Max(e => e.FechaRegistro) })
                        .ToListAsync();
                    var dejaronDeRegistrar = new HashSet<Guid>(
                        ultimoMoodPorUsuario
                            .Where(x => x.Ultimo < hace14dias)
                            .Select(x => x.Usuario));
                    // Excluir a quienes ya recibieron esta campaña (mismo patrón que ViejosSinToque1).
                    var yaEnviados = new HashSet<Guid>(await _db.EmailCampanaLogs
                        .Where(l => l.Fase == FaseLogSinMoodReciente && l.Exito)
                        .Select(l => l.UserId).Distinct().ToListAsync());
                    return (
                        _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados)
                            .Where(u => dejaronDeRegistrar.Contains(u.Id) && !yaEnviados.Contains(u.Id)),
                        FaseLogSinMoodReciente);
                }

                case AudienciaCampana.TodosConfirmados:
                default:
                {
                    var q = _targeting.AplicarCriterio(users, PublicoCampana.TodosConfirmados);
                    if (!string.IsNullOrWhiteSpace(templateId))
                    {
                        var yaRecibieron = new HashSet<Guid>(await _db.EmailCampanaLogs
                            .Where(l => l.Fase == FaseLogGeneral && l.Exito && l.TemplateId == templateId)
                            .Select(l => l.UserId).Distinct().ToListAsync());
                        q = q.Where(u => !yaRecibieron.Contains(u.Id));
                    }
                    return (q, FaseLogGeneral);
                }
            }
        }
    }
}
