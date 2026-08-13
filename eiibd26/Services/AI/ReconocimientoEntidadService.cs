using eiibd26.Configuration;
using eiibd26.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace eiibd26.Services.AI
{
    /// <inheritdoc cref="IReconocimientoEntidadService"/>
    public class ReconocimientoEntidadService : IReconocimientoEntidadService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISintomasTratamientosAiService _aiService;
        private readonly ReconocimientoEntidadConfiguration _config;
        private readonly ILogger<ReconocimientoEntidadService> _logger;

        public ReconocimientoEntidadService(
            ApplicationDbContext db,
            ISintomasTratamientosAiService aiService,
            IOptions<ReconocimientoEntidadConfiguration> config,
            ILogger<ReconocimientoEntidadService> logger)
        {
            _db        = db;
            _aiService = aiService;
            _config    = config.Value;
            _logger    = logger;
        }

        public bool Habilitado => _config.Habilitado;

        public double ConfianzaMinima => _config.ConfianzaMinima;

        public bool DesactivaNoReconocidos => _config.DesactivarNoReconocidos;

        public async Task<DecisionReconocimiento> EvaluarAsync(
            TipoEntidadClinica tipo,
            EntradaReconocimiento entrada,
            CancellationToken cancellationToken = default)
        {
            if (!_config.Habilitado)
            {
                // Rollback: el gate apagado devuelve el comportamiento anterior al REQ.
                // Queda el guardrail del generador como única red.
                return new DecisionReconocimiento(
                    ResultadoReconocimiento.Reconocido, "gate-deshabilitado", 0d,
                    "El gate de reconocimiento está deshabilitado por configuración.");
            }

            // ── TIER 0 — allowlist en BD. Determinista, sin red, inmune a outages.
            //    SOLO DEJA PASAR: esta capa nunca devuelve NoReconocido. Es la garantía
            //    estructural de que el gate no puede despublicar trabajo médico humano.
            if (entrada.ValidadoHumano)
            {
                return new DecisionReconocimiento(
                    ResultadoReconocimiento.Reconocido, "allowlist", 1d,
                    "Validado por un humano en el catálogo.");
            }

            try
            {
                if (await TieneValidacionMedicaAprobadaAsync(tipo, entrada.Id, cancellationToken))
                {
                    return new DecisionReconocimiento(
                        ResultadoReconocimiento.Reconocido, "allowlist", 1d,
                        "Tiene validación médica aprobada en el glosario.");
                }
            }
            catch (Exception ex)
            {
                // Si no se puede leer la allowlist NO se puede afirmar que el registro carece de
                // respaldo humano. Sin esa certeza, seguir a Tier 1 podría terminar desactivando
                // una ficha validada por un médico: se corta aquí sin veredicto.
                _logger.LogError(ex,
                    "Gate: falló la consulta de allowlist para {Tipo} {Id}. Sin veredicto.", tipo, entrada.Id);
                return new DecisionReconocimiento(
                    ResultadoReconocimiento.GroundingNoDisponible, "error", 0d,
                    "No se pudo comprobar la validación humana — sin veredicto.");
            }

            // ── TIER 1 — triage por NOMBRE.
            if (string.IsNullOrWhiteSpace(entrada.Nombre))
            {
                return new DecisionReconocimiento(
                    ResultadoReconocimiento.RevisionHumana, "triage-nombre", 0d,
                    "El registro no tiene nombre — no hay nada que reconocer.");
            }

            // La descripción autogenerada NO entra al gate: es justo la que puede estar
            // confabulada, y el triage la usaría como contexto para confirmarse a sí mismo.
            // Solo se pasa cuando la escribió una persona.
            var descripcionParaContexto = entrada.ValidadoIA && !entrada.ValidadoHumano
                ? null
                : entrada.DescripcionExistente;

            return await EjecutarTriageAsync(tipo, entrada.Nombre, descripcionParaContexto, cancellationToken);
        }

        public Task<DecisionReconocimiento> SondaPorNombreAsync(
            TipoEntidadClinica tipo,
            string nombre,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return Task.FromResult(new DecisionReconocimiento(
                    ResultadoReconocimiento.RevisionHumana, "triage-nombre", 0d,
                    "Nombre vacío."));
            }

            // La sonda ignora el feature flag a propósito: sirve para calibrar el umbral ANTES
            // de encender el gate. No toca BD ni escribe nada.
            return EjecutarTriageAsync(tipo, nombre, null, cancellationToken);
        }

        /// <summary>
        /// Tier 1: llama al triage ya calibrado y traduce sus 3 vías al contrato del gate.
        /// </summary>
        private async Task<DecisionReconocimiento> EjecutarTriageAsync(
            TipoEntidadClinica tipo,
            string nombre,
            string? descripcionParaContexto,
            CancellationToken cancellationToken)
        {
            byte estado;
            double confianza;
            string motivo;

            try
            {
                (estado, confianza, motivo, _, _) = tipo == TipoEntidadClinica.Tratamiento
                    ? await _aiService.ClasificarTratamientoAsync(nombre, descripcionParaContexto, cancellationToken)
                    : await _aiService.ClasificarSintomaAsync(nombre, descripcionParaContexto, cancellationToken);
            }
            catch (Exception ex)
            {
                // Fail-safe: "no hubo respuesta" NUNCA es "no reconocido".
                _logger.LogWarning(ex, "Gate: sin grounding para '{Nombre}' ({Tipo})", nombre, tipo);
                return new DecisionReconocimiento(
                    ResultadoReconocimiento.GroundingNoDisponible, "triage-nombre", 0d,
                    $"No se pudo verificar el término: {ex.Message}");
            }

            // El triage nunca lanza por respuesta ilegible: la absorbe como Dudoso con un motivo
            // centinela. Ese caso es "no hubo verificación", no "es dudoso" — se distingue aquí
            // para que no genere ni una sola escritura.
            if (motivo == SintomasTratamientosAiService.MotivoTriageSinJson ||
                motivo == SintomasTratamientosAiService.MotivoTriageJsonInvalido)
            {
                _logger.LogWarning("Gate: respuesta ilegible del triage para '{Nombre}' ({Tipo})", nombre, tipo);
                return new DecisionReconocimiento(
                    ResultadoReconocimiento.GroundingNoDisponible, "triage-nombre", 0d,
                    "La verificación devolvió una respuesta ilegible — sin veredicto.");
            }

            if (confianza < _config.ConfianzaMinima)
            {
                return new DecisionReconocimiento(
                    ResultadoReconocimiento.RevisionHumana, "triage-nombre", confianza,
                    $"Confianza {confianza:0.00} < {_config.ConfianzaMinima:0.00} — requiere revisión humana. {motivo}");
            }

            return estado switch
            {
                (byte)TriageLimpieza.Valido => new DecisionReconocimiento(
                    ResultadoReconocimiento.Reconocido, "triage-nombre", confianza, motivo),

                (byte)TriageLimpieza.Basura => new DecisionReconocimiento(
                    ResultadoReconocimiento.NoReconocido, "triage-nombre", confianza, motivo),

                // Dudoso (y cualquier estado inesperado) → cola humana. Nunca despublica.
                _ => new DecisionReconocimiento(
                    ResultadoReconocimiento.RevisionHumana, "triage-nombre", confianza, motivo)
            };
        }

        /// <summary>
        /// ¿El término del glosario vinculado tiene alguna validación médica aprobada?
        /// Las validaciones de los médicos viven en <c>GlossaryValidation</c> (se hacen desde
        /// <c>/Termino/{slug}</c>), NO en <c>tratamientos.ValidadoHumano</c>. Ignorarlas sería
        /// pisar trabajo clínico ya hecho.
        /// </summary>
        private async Task<bool> TieneValidacionMedicaAprobadaAsync(
            TipoEntidadClinica tipo,
            int id,
            CancellationToken cancellationToken)
        {
            var links = tipo == TipoEntidadClinica.Tratamiento
                ? _db.GlossaryTermMedicalLinks.AsNoTracking().Where(l => l.TratamientoId == id)
                : _db.GlossaryTermMedicalLinks.AsNoTracking().Where(l => l.SintomaId == id);

            return await links.AnyAsync(
                l => _db.GlossaryValidations.Any(v => v.GlossaryTermId == l.GlossaryTermId && v.Approved),
                cancellationToken);
        }
    }
}
