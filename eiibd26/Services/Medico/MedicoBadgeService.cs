using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Models.Medico;

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
                Id            = b.Id,
                Codigo        = b.Codigo,
                Nombre        = b.Nombre,
                Descripcion   = b.Descripcion,
                ComoObtenerlo = b.ComoObtenerlo,
                Icono         = b.Icono,
                Nivel         = b.Nivel,
                Obtenido      = ganado != null,
                FechaObtenido = ganado?.FechaObtenido
            };
        }).ToList();
    }

    public async Task<List<MedicoBadgeDto>> GetBadgesGanadosAsync(int medicoId)
    {
        return await _db.MedicosPerfilBadge
            .AsNoTracking()
            .Where(pb => pb.MedicoId == medicoId)
            .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id,
                (pb, b) => new MedicoBadgeDto
                {
                    Id            = b.Id,
                    Codigo        = b.Codigo,
                    Nombre        = b.Nombre,
                    Descripcion   = b.Descripcion,
                    ComoObtenerlo = b.ComoObtenerlo,
                    Icono         = b.Icono,
                    Nivel         = b.Nivel,
                    Obtenido      = true,
                    FechaObtenido = pb.FechaObtenido
                })
            .OrderBy(d => d.Nivel)
            .ToListAsync();
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
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task EvaluarBadgesAutomaticosAsync(int medicoId)
    {
        // perfil_reclamado: MedicoPerfilExtendido con UserId != null
        var tienePerfilVinculado = await _db.MedicosPerfilExtendido
            .AnyAsync(p => p.MedicoId == medicoId && p.UserId != null);
        if (tienePerfilVinculado)
            await OtorgarBadgeAsync(medicoId, "perfil_reclamado", "sistema");

        // activo_comunidad: >= 5 confirmaciones de pacientes
        var totalConfirmaciones = await _db.DirectorioMedicoConfirmaciones
            .CountAsync(c => c.MedicoId == medicoId && !c.Eliminado);
        if (totalConfirmaciones >= 5)
            await OtorgarBadgeAsync(medicoId, "activo_comunidad", "sistema");

        // participante_qa y validador_contenido: requieren UserId vinculado
        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MedicoId == medicoId && p.UserId != null);

        if (perfil?.UserId != null)
        {
            // participante_qa: >= 3 respuestas del usuario vinculado
            var respuestas = await _db.Respuestas
                .CountAsync(r => r.UsuarioId == perfil.UserId.Value);
            if (respuestas >= 3)
                await OtorgarBadgeAsync(medicoId, "participante_qa", "sistema");

            // validador_contenido: >= 5 validaciones en GlossaryValidations
            var userIdStr = perfil.UserId.Value.ToString();
            var validaciones = await _db.GlossaryValidations
                .CountAsync(v => v.UserId == userIdStr);
            if (validaciones >= 5)
                await OtorgarBadgeAsync(medicoId, "validador_contenido", "sistema");
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
            "crear_contenido"          => nivel >= 6,
            _                          => false
        };
    }
}
