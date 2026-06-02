using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Models.Medico;
using eiibd26.Models.Validacion;

namespace eiibd26.Services.Medico;

public class MedicoBadgeService : IMedicoBadgeService
{
    private readonly ApplicationDbContext _db;

    public MedicoBadgeService(ApplicationDbContext db) => _db = db;

    public async Task<List<MedicoBadgeDto>> GetTodosLosBadgesAsync(int medicoId)
    {
        var catalogo = await _db.MedicosBadge
            .AsNoTracking()
            .Where(b => b.Activo)
            .OrderBy(b => b.Orden)
            .ToListAsync();

        var ganados = await _db.MedicosPerfilBadge
            .AsNoTracking()
            .Where(pb => pb.MedicoId == medicoId)
            .ToListAsync();

        return catalogo.Select(b =>
        {
            var ganado = ganados.FirstOrDefault(g => g.BadgeId == b.Id);
            return new MedicoBadgeDto
            {
                Id             = b.Id,
                Codigo         = b.Codigo,
                Nombre         = b.Nombre,
                Descripcion    = b.Descripcion,
                ComoObtenerlo  = b.ComoObtenerlo,
                Icono          = b.Icono,
                Nivel          = b.Nivel,
                Obtenido       = ganado != null,
                FechaObtenido  = ganado?.FechaObtenido,
                EnRevision     = ganado?.EnRevision ?? false,
                RevisionMotivo = ganado?.RevisionMotivo
            };
        }).ToList();
    }

    public async Task<List<MedicoBadgeDto>> GetBadgesGanadosAsync(int medicoId)
    {
        // Two-step: fetch with Orden (for stable ThenBy) then project to DTO in memory
        var rows = await _db.MedicosPerfilBadge
            .AsNoTracking()
            .Where(pb => pb.MedicoId == medicoId)
            .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id,
                (pb, b) => new
                {
                    b.Id, b.Codigo, b.Nombre, b.Descripcion, b.ComoObtenerlo,
                    b.Icono, b.Nivel, b.Orden,
                    pb.FechaObtenido, pb.EnRevision, pb.RevisionMotivo
                })
            .OrderBy(x => x.Nivel).ThenBy(x => x.Orden)
            .ToListAsync();

        return rows.Select(x => new MedicoBadgeDto
        {
            Id             = x.Id,
            Codigo         = x.Codigo,
            Nombre         = x.Nombre,
            Descripcion    = x.Descripcion,
            ComoObtenerlo  = x.ComoObtenerlo,
            Icono          = x.Icono,
            Nivel          = x.Nivel,
            Obtenido       = true,
            FechaObtenido  = x.FechaObtenido,
            EnRevision     = x.EnRevision,
            RevisionMotivo = x.RevisionMotivo
        }).ToList();
    }

    public async Task<int> GetNivelActualAsync(int medicoId)
    {
        var badges = await GetBadgesGanadosAsync(medicoId);
        return badges.Count > 0 ? badges.Max(b => b.Nivel) : 0;
    }

    public async Task<bool> OtorgarBadgeAsync(int medicoId, string codigo, string otorgadoPor)
    {
        var badge = await _db.MedicosBadge.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Codigo == codigo && b.Activo);
        if (badge is null) return false;

        var yaExiste = await _db.MedicosPerfilBadge
            .AnyAsync(pb => pb.MedicoId == medicoId && pb.BadgeId == badge.Id);
        if (yaExiste) return false;

        _db.MedicosPerfilBadge.Add(new MedicoPerfilBadge
        {
            MedicoId      = medicoId,
            BadgeId       = badge.Id,
            FechaObtenido = DateTime.UtcNow,
            OtorgadoPor   = otorgadoPor
        });
        _db.MedicosBadgeHistorial.Add(new MedicoBadgeHistorial
        {
            MedicoId    = medicoId,
            BadgeId     = badge.Id,
            Evento      = "otorgado",
            Actor       = otorgadoPor,
            FechaEvento = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevocarBadgeAsync(int medicoId, string codigo, string revocadoPor = "admin", string? motivo = null)
    {
        var badge = await _db.MedicosBadge.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Codigo == codigo);
        if (badge is null) return false;

        var entry = await _db.MedicosPerfilBadge
            .FirstOrDefaultAsync(pb => pb.MedicoId == medicoId && pb.BadgeId == badge.Id);
        if (entry is null) return false;

        _db.MedicosPerfilBadge.Remove(entry);
        _db.MedicosBadgeHistorial.Add(new MedicoBadgeHistorial
        {
            MedicoId    = medicoId,
            BadgeId     = badge.Id,
            Evento      = "revocado",
            Actor       = revocadoPor,
            Motivo      = motivo,
            FechaEvento = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarcarEnRevisionAsync(int medicoId, string codigo, bool enRevision, string actor, string? motivo = null)
    {
        var badge = await _db.MedicosBadge.AsNoTracking().FirstOrDefaultAsync(b => b.Codigo == codigo);
        if (badge is null) return false;

        var entry = await _db.MedicosPerfilBadge
            .FirstOrDefaultAsync(pb => pb.MedicoId == medicoId && pb.BadgeId == badge.Id);
        if (entry is null) return false;

        entry.EnRevision     = enRevision;
        entry.RevisionMotivo = enRevision ? motivo : null;
        entry.RevisionPor    = enRevision ? actor  : null;
        entry.FechaRevision  = enRevision ? DateTime.UtcNow : null;

        _db.MedicosBadgeHistorial.Add(new MedicoBadgeHistorial
        {
            MedicoId    = medicoId,
            BadgeId     = badge.Id,
            Evento      = enRevision ? "en_revision" : "revision_quitada",
            Actor       = actor,
            Motivo      = motivo,
            FechaEvento = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task EvaluarBadgesAutomaticosAsync(int medicoId)
    {
        // perfil_reclamado: MedicoPerfilExtendido con UserId vinculado
        // y EstatusReclamacion == Reclamado (D-05: evitar badge fantasma en claims rechazados/pendientes)
        var tienePerfilVinculado = await _db.MedicosPerfilExtendido
            .AnyAsync(p => p.MedicoId == medicoId && p.UserId != null);
        var claimAprobado = await _db.MedicosDirectorio
            .AnyAsync(m => m.Id == medicoId && m.EstatusReclamacion == eiibd26.Models.Directorio.Enums.EstatusReclamacion.Reclamado);
        if (tienePerfilVinculado && claimAprobado)
            await OtorgarBadgeAsync(medicoId, "perfil_reclamado", "sistema");

        // activo_comunidad: >= 5 confirmaciones de pacientes
        var totalConfirmaciones = await _db.ConfirmacionesComunitarias
            .CountAsync(c => c.MedicoDirectorioId == medicoId && !c.Eliminado);
        if (totalConfirmaciones >= 5)
            await OtorgarBadgeAsync(medicoId, "activo_comunidad", "sistema");

        // participante_qa y validador_contenido: requieren UserId vinculado
        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MedicoId == medicoId && p.UserId != null);

        if (perfil?.UserId != null)
        {
            // participante_qa: >= 3 respuestas del usuario vinculado (excluye IA y eliminadas)
            var respuestas = await _db.Respuestas
                .CountAsync(r => r.UsuarioId == perfil.UserId.Value && !r.Eliminado && !r.EsIA);
            if (respuestas >= 3)
                await OtorgarBadgeAsync(medicoId, "participante_qa", "sistema");

            // validador_terminos: >= 3 términos del glosario validados (GlossaryValidations)
            var userIdStr = perfil.UserId.Value.ToString();
            var validacionesTerminos = await _db.GlossaryValidations
                .CountAsync(v => v.UserId == userIdStr && v.Approved);
            if (validacionesTerminos >= 3)
                await OtorgarBadgeAsync(medicoId, "validador_terminos", "sistema");

            // validador_contenido: >= 3 contenidos validados (ValidacionesContenidoProfesional)
            var validacionesContenido = await _db.ValidacionesContenidoProfesional
                .CountAsync(v => v.UsuarioMedicoId == userIdStr && v.Estado == EstadoValidacion.Validado);
            if (validacionesContenido >= 3)
                await OtorgarBadgeAsync(medicoId, "validador_contenido", "sistema");

            // validador_respuestas: >= 3 respuestas validadas (ValidacionRespuestaProfesional)
            var validacionesRespuestas = await _db.ValidacionesRespuestaProfesional
                .CountAsync(v => v.UsuarioMedicoId == userIdStr && v.Estado == EstadoValidacion.Validado);
            if (validacionesRespuestas >= 3)
                await OtorgarBadgeAsync(medicoId, "validador_respuestas", "sistema");
        }
    }

    public async Task<bool> TienePermisoAsync(int medicoId, string permiso)
    {
        var nivel = await GetNivelActualAsync(medicoId);
        return permiso switch
        {
            "editar_perfil"            => nivel >= 1,
            "ver_comentarios_anonimos" => nivel >= 2,
            "reportar_comentarios"     => nivel >= 2,
            "ver_nombre_paciente"      => nivel >= 3,
            "responder_comentarios"    => nivel >= 3,
            "participar_qa"            => nivel >= 4,
            "validar_contenido"        => nivel >= 5,
            "validar_respuestas"       => nivel >= 4,
            "crear_contenido"          => nivel >= 6,
            _                          => false
        };
    }

    public async Task<List<MedicoBadgeHistorialDto>> GetHistorialAsync(int medicoId)
    {
        return await _db.MedicosBadgeHistorial
            .AsNoTracking()
            .Where(h => h.MedicoId == medicoId)
            .Join(_db.MedicosBadge,
                  h => h.BadgeId,
                  b => b.Id,
                  (h, b) => new MedicoBadgeHistorialDto
                  {
                      BadgeCodigo = b.Codigo,
                      BadgeNombre = b.Nombre,
                      Evento      = h.Evento,
                      Actor       = h.Actor,
                      Motivo      = h.Motivo,
                      FechaEvento = h.FechaEvento
                  })
            .OrderByDescending(h => h.FechaEvento)
            .ToListAsync();
    }
}
