using eiibd26.Services.Glossary.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Services.Glossary.Adapters
{
    /// <summary>
    /// Implementación del adapter para leer datos médicos.
    /// ⚠️ IMPORTANTE: Solo lectura, mantiene bajo acoplamiento con dominio médico.
    /// </summary>
    public class MedicalDataAdapter : IMedicalDataAdapter
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MedicalDataAdapter> _logger;

        public MedicalDataAdapter(
            ApplicationDbContext db,
            ILogger<MedicalDataAdapter> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Lee definición médica de un síntoma (READ ONLY)
        /// </summary>
        public async Task<MedicalDefinitionDto?> GetSymptomDefinitionAsync(int sintomaId)
        {
            try
            {
                var sintoma = await _db.sintomas
                    .AsNoTracking() // ⚠️ READ ONLY
                    .Where(s => s.id == sintomaId)
                    .Select(s => new MedicalDefinitionDto
                    {
                        Nombre = s.nombre ?? "",
                        DescripcionIA = s.DescripcionIA,
                        RelacionEII = s.RelacionEII,
                        RelacionEIIDescripcion = s.RelacionEIIDescripcion,
                        Fuentes = s.Fuentes
                    })
                    .FirstOrDefaultAsync();

                if (sintoma == null)
                {
                    _logger.LogWarning("Síntoma {SintomaId} no encontrado", sintomaId);
                }

                return sintoma;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener definición del síntoma {SintomaId}", sintomaId);
                return null;
            }
        }

        /// <summary>
        /// Lee definición médica de un tratamiento (READ ONLY)
        /// </summary>
        public async Task<MedicalDefinitionDto?> GetTreatmentDefinitionAsync(int tratamientoId)
        {
            try
            {
                var tratamiento = await _db.tratamientos
                    .AsNoTracking() // ⚠️ READ ONLY
                    .Where(t => t.id == tratamientoId)
                    .Select(t => new MedicalDefinitionDto
                    {
                        Nombre = t.nombre ?? "",
                        DescripcionIA = t.DescripcionIA,
                        RelacionEII = t.RelacionEII,
                        RelacionEIIDescripcion = t.RelacionEIIDescripcion,
                        Fuentes = t.Fuentes
                    })
                    .FirstOrDefaultAsync();

                if (tratamiento == null)
                {
                    _logger.LogWarning("Tratamiento {TratamientoId} no encontrado", tratamientoId);
                }

                return tratamiento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener definición del tratamiento {TratamientoId}", tratamientoId);
                return null;
            }
        }
    }
}
