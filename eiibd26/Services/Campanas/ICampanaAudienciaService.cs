using eiibd26.Models;
using eiibd26.Models.Campanas;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Services.Campanas
{
    /// <summary>
    /// Resuelve el universo de elegibles de cada audiencia de campaña y su FaseLog.
    /// Fuente ÚNICA de verdad de la lógica de exclusión: la usan tanto el conteo en vivo
    /// (OnGetConteoAudiencia) como el envío en segundo plano (CampanaEnvioJob).
    /// </summary>
    public interface ICampanaAudienciaService
    {
        /// <summary>
        /// Construye la query de elegibles para la audiencia indicada y devuelve el FaseLog asignado.
        /// Aplica filtros de validez, rebote y cuentas de sistema al universo base, y la exclusión
        /// por FaseLog (no reenvía a quien ya recibió) en todas las audiencias que la soportan.
        /// </summary>
        Task<(IQueryable<ApplicationUser> query, int faseLog)> BuildAudienciaQueryAsync(
            AudienciaCampana audiencia, string? templateId = null);

        /// <summary>
        /// Devuelve el FaseLog asociado a una audiencia sin construir la query.
        /// Lo usa el reset global para saber qué registros de EmailCampanaLog borrar.
        /// </summary>
        int FaseLogPara(AudienciaCampana audiencia);
    }
}
