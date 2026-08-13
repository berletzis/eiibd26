using eiibd26.Data;
using eiibd26.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    /// <summary>
    /// Sonda del gate de reconocimiento. TODO es READ-ONLY: corre el Tier 1 y devuelve el
    /// veredicto sin tocar la BD, para poder calibrar el umbral ANTES de dejar que el gate
    /// escriba nada.
    /// </summary>
    /// <remarks>
    /// No hay ni un <c>SaveChanges</c> en este controller a propósito: la garantía de "0 filas
    /// modificadas" de la prueba de aceptación #4 es estructural, no una promesa.
    /// </remarks>
    [Authorize(Roles = "Administrador")]
    [ApiController]
    [Route("api/admin/reconocimiento")]
    public class ReconocimientoAdminController : ControllerBase
    {
        private readonly IReconocimientoEntidadService _gate;
        private readonly ILogger<ReconocimientoAdminController> _logger;

        public ReconocimientoAdminController(
            IReconocimientoEntidadService gate,
            ILogger<ReconocimientoAdminController> logger)
        {
            _gate = gate;
            _logger = logger;
        }

        /// <summary>Qué se espera de un caso de prueba.</summary>
        private enum Expectativa
        {
            /// <summary>Tiene que salir Reconocido.</summary>
            DebeReconocer,

            /// <summary>NO puede salir Reconocido (NoReconocido o RevisionHumana son válidos:
            /// lo que importa es que no se publique solo).</summary>
            NoDebeReconocer,

            /// <summary>REGLA ANTI-SUPRESIÓN: no puede salir NoReconocido. Un fármaco real
            /// suprimido es el error caro — desactivaría contenido legítimo.</summary>
            NoPuedeSerNoReconocido
        }

        private record CasoPrueba(string Nombre, TipoEntidadClinica Tipo, Expectativa Expectativa, string Grupo);

        /// <summary>
        /// Batería fija del REQ §8. Se mantiene en código (no en la request) para que la prueba
        /// sea siempre la misma y comparable entre corridas.
        /// </summary>
        private static readonly CasoPrueba[] Bateria =
        [
            // §8.1 — positivos: tratamientos reales y bien conocidos.
            new("Mesalazina",              TipoEntidadClinica.Tratamiento, Expectativa.DebeReconocer,          "positivos"),
            new("Infliximab",              TipoEntidadClinica.Tratamiento, Expectativa.DebeReconocer,          "positivos"),
            new("Prednisona",              TipoEntidadClinica.Tratamiento, Expectativa.DebeReconocer,          "positivos"),
            new("Dieta baja en FODMAP",    TipoEntidadClinica.Tratamiento, Expectativa.DebeReconocer,          "positivos"),

            // §8.2 — negativos: el caso real que motivó el REQ, ruido y producto de consumo.
            new("Aangamik",                TipoEntidadClinica.Tratamiento, Expectativa.NoDebeReconocer,        "negativos"),
            new("Xyzqwe 500",              TipoEntidadClinica.Tratamiento, Expectativa.NoDebeReconocer,        "negativos"),
            new("Gel Limpiador de Kombucha", TipoEntidadClinica.Tratamiento, Expectativa.NoDebeReconocer,      "negativos"),

            // §8.3 — anti-supresión (CRÍTICA): fármacos reales, oscuros o ajenos a la EII.
            new("Ciclopentolato",          TipoEntidadClinica.Tratamiento, Expectativa.NoPuedeSerNoReconocido, "anti-supresion"),
            new("Hipurato de sodio",       TipoEntidadClinica.Tratamiento, Expectativa.NoPuedeSerNoReconocido, "anti-supresion"),
            new("Vedolizumab",             TipoEntidadClinica.Tratamiento, Expectativa.NoPuedeSerNoReconocido, "anti-supresion"),
            new("Ácido obeticólico",       TipoEntidadClinica.Tratamiento, Expectativa.NoPuedeSerNoReconocido, "anti-supresion"),

            // Espejo para el catálogo de síntomas (misma lógica, otra rúbrica).
            new("Dolor abdominal",         TipoEntidadClinica.Sintoma,     Expectativa.DebeReconocer,          "positivos"),
            new("Fatiga",                  TipoEntidadClinica.Sintoma,     Expectativa.DebeReconocer,          "positivos"),
            new("Aangamik",                TipoEntidadClinica.Sintoma,     Expectativa.NoDebeReconocer,        "negativos"),
            new("asdfgh",                  TipoEntidadClinica.Sintoma,     Expectativa.NoDebeReconocer,        "negativos"),
            new("Eritema nodoso",          TipoEntidadClinica.Sintoma,     Expectativa.NoPuedeSerNoReconocido, "anti-supresion"),
            new("Uveítis",                 TipoEntidadClinica.Sintoma,     Expectativa.NoPuedeSerNoReconocido, "anti-supresion"),
        ];

        /// <summary>
        /// Corre la batería de aceptación del REQ §8 sin escribir nada.
        /// GET /api/admin/reconocimiento/aceptacion?repeticiones=2
        /// </summary>
        /// <param name="repeticiones">
        /// §8.6 (estabilidad): con temperatura 0.0 dos pasadas deben dar el mismo veredicto.
        /// Cualquier caso que cambie entre pasadas se reporta como inestable.
        /// </param>
        [HttpGet("aceptacion")]
        public async Task<IActionResult> Aceptacion(int repeticiones = 1, CancellationToken cancellationToken = default)
        {
            var vueltas = Math.Clamp(repeticiones, 1, 3);
            var resultados = new List<object>();
            var inestables = new List<string>();

            var fallosCriticos = 0;
            var fallos = 0;
            var sinVeredicto = 0;

            foreach (var caso in Bateria)
            {
                ResultadoReconocimiento? primero = null;
                DecisionReconocimiento? ultima = null;
                var estable = true;

                // Secuencial: además de respetar la prohibición de concurrencia del REQ, evita
                // que una ráfaga de llamadas dispare el rate limit y ensucie la medición.
                for (var i = 0; i < vueltas; i++)
                {
                    ultima = await _gate.SondaPorNombreAsync(caso.Tipo, caso.Nombre, cancellationToken);
                    primero ??= ultima.Resultado;
                    if (ultima.Resultado != primero) estable = false;
                }

                var decision = ultima!;
                if (!estable) inestables.Add($"{caso.Nombre} ({caso.Tipo})");

                // Sin veredicto no es aprobar ni reprobar: es que no hubo verificación.
                // Se cuenta aparte para no maquillar una corrida hecha con la API caída.
                var esSinVeredicto = decision.Resultado == ResultadoReconocimiento.GroundingNoDisponible;

                var cumple = caso.Expectativa switch
                {
                    Expectativa.DebeReconocer          => decision.Resultado == ResultadoReconocimiento.Reconocido,
                    Expectativa.NoDebeReconocer        => decision.Resultado != ResultadoReconocimiento.Reconocido,
                    Expectativa.NoPuedeSerNoReconocido => decision.Resultado != ResultadoReconocimiento.NoReconocido,
                    _                                  => false
                };

                if (esSinVeredicto)
                {
                    sinVeredicto++;
                }
                else if (!cumple)
                {
                    fallos++;
                    if (caso.Expectativa == Expectativa.NoPuedeSerNoReconocido)
                        fallosCriticos++;
                }

                resultados.Add(new
                {
                    nombre      = caso.Nombre,
                    tipo        = caso.Tipo.ToString(),
                    grupo       = caso.Grupo,
                    espera      = caso.Expectativa.ToString(),
                    resultado   = decision.Resultado.ToString(),
                    fuente      = decision.Fuente,
                    confianza   = Math.Round(decision.Confianza, 3),
                    motivo      = decision.Motivo,
                    cumple,
                    estable,
                    sinVeredicto = esSinVeredicto
                });
            }

            // Un solo fármaco real en NoReconocido invalida la corrida: hay que recalibrar el
            // umbral o la rúbrica ANTES de encender el gate.
            var veredicto =
                sinVeredicto == Bateria.Length ? "SIN_GROUNDING (la API no respondió: prueba no concluyente)"
                : fallosCriticos > 0           ? "FALLIDA (regla anti-supresión rota: NO activar el gate)"
                : fallos > 0                   ? "FALLIDA"
                : sinVeredicto > 0             ? "PARCIAL (hubo casos sin veredicto)"
                : inestables.Count > 0         ? "INESTABLE"
                                               : "APROBADA";

            _logger.LogInformation(
                "Sonda de reconocimiento: {Veredicto} — {Fallos} fallos ({Criticos} críticos), {SinVeredicto} sin veredicto, {Inestables} inestables.",
                veredicto, fallos, fallosCriticos, sinVeredicto, inestables.Count);

            return Ok(new
            {
                ok = true,
                veredicto,
                gateHabilitado  = _gate.Habilitado,
                confianzaMinima = _gate.ConfianzaMinima,
                desactivaNoReconocidos = _gate.DesactivaNoReconocidos,
                repeticiones = vueltas,
                totales = new
                {
                    casos = Bateria.Length,
                    fallos,
                    fallosCriticos,
                    sinVeredicto,
                    inestables = inestables.Count
                },
                inestables,
                resultados,
                nota = "Sonda read-only: no se escribió ninguna fila."
            });
        }

        /// <summary>
        /// Corre el gate sobre nombres arbitrarios, sin escribir. Para probar un término
        /// concreto antes o después de calibrar.
        /// POST /api/admin/reconocimiento/sonda  { "tipo": 1, "nombres": ["Aangamik"] }
        /// </summary>
        [HttpPost("sonda")]
        public async Task<IActionResult> Sonda([FromBody] SondaRequest request, CancellationToken cancellationToken)
        {
            if (request?.Nombres == null || request.Nombres.Count == 0)
                return BadRequest(new { ok = false, error = "Envía al menos un nombre." });

            var tipo = request.Tipo == (int)TipoEntidadClinica.Sintoma
                ? TipoEntidadClinica.Sintoma
                : TipoEntidadClinica.Tratamiento;

            var resultados = new List<object>();
            foreach (var nombre in request.Nombres.Take(50))
            {
                var decision = await _gate.SondaPorNombreAsync(tipo, nombre ?? "", cancellationToken);
                resultados.Add(new
                {
                    nombre,
                    resultado = decision.Resultado.ToString(),
                    fuente    = decision.Fuente,
                    confianza = Math.Round(decision.Confianza, 3),
                    motivo    = decision.Motivo
                });
            }

            return Ok(new
            {
                ok = true,
                tipo = tipo.ToString(),
                confianzaMinima = _gate.ConfianzaMinima,
                resultados,
                nota = "Sonda read-only: no se escribió ninguna fila."
            });
        }

        /// <summary>
        /// Corre el gate COMPLETO (Tier 0 + Tier 1) sobre un registro real del catálogo, sin
        /// escribir. Es la única forma de comprobar la prueba §8.5: un registro con
        /// <c>ValidadoHumano</c> (o con validación médica aprobada) y nombre deliberadamente
        /// basura debe salir Reconocido vía <c>allowlist</c> — o sea, el gate NO pisa el
        /// trabajo humano.
        /// GET /api/admin/reconocimiento/registro?tipo=1&amp;id=123
        /// </summary>
        [HttpGet("registro")]
        public async Task<IActionResult> Registro(
            [FromServices] ApplicationDbContext db,
            int tipo,
            int id,
            CancellationToken cancellationToken)
        {
            EntradaReconocimiento? entrada;
            TipoEntidadClinica tipoEntidad;

            // Proyección a tipo anónimo y luego construcción: no se le pide al traductor de EF
            // que sepa materializar el record posicional.
            if (tipo == (int)TipoEntidadClinica.Sintoma)
            {
                tipoEntidad = TipoEntidadClinica.Sintoma;
                var fila = await db.sintomas.AsNoTracking()
                    .Where(s => s.id == id)
                    .Select(s => new { s.id, s.nombre, s.ValidadoHumano, s.ValidadoIA, s.DescripcionIA })
                    .FirstOrDefaultAsync(cancellationToken);
                entrada = fila == null
                    ? null
                    : new EntradaReconocimiento(fila.id, fila.nombre, fila.ValidadoHumano, fila.ValidadoIA, fila.DescripcionIA);
            }
            else
            {
                tipoEntidad = TipoEntidadClinica.Tratamiento;
                var fila = await db.tratamientos.AsNoTracking()
                    .Where(t => t.id == id)
                    .Select(t => new { t.id, t.nombre, t.ValidadoHumano, t.ValidadoIA, t.DescripcionIA })
                    .FirstOrDefaultAsync(cancellationToken);
                entrada = fila == null
                    ? null
                    : new EntradaReconocimiento(fila.id, fila.nombre, fila.ValidadoHumano, fila.ValidadoIA, fila.DescripcionIA);
            }

            if (entrada == null)
                return NotFound(new { ok = false, error = "Registro no encontrado." });

            // EvaluarAsync solo lee: la sonda sigue siendo read-only.
            var decision = await _gate.EvaluarAsync(tipoEntidad, entrada, cancellationToken);

            return Ok(new
            {
                ok = true,
                tipo = tipoEntidad.ToString(),
                id,
                nombre = entrada.Nombre,
                validadoHumano = entrada.ValidadoHumano,
                resultado = decision.Resultado.ToString(),
                fuente    = decision.Fuente,
                confianza = Math.Round(decision.Confianza, 3),
                motivo    = decision.Motivo,
                efectoSiSeAplicara = decision.Resultado switch
                {
                    ResultadoReconocimiento.Reconocido            => "Se generaría la ficha.",
                    ResultadoReconocimiento.NoReconocido          => _gate.DesactivaNoReconocidos
                        ? "NO se generaría; se sellaría Dudoso y el término saldría del glosario."
                        : "NO se generaría; se sellaría Dudoso (sin despublicar).",
                    ResultadoReconocimiento.RevisionHumana        => "NO se generaría; se sellaría Dudoso SIN despublicar.",
                    _                                             => "No se escribiría nada (sin veredicto)."
                },
                nota = "Sonda read-only: no se escribió ninguna fila."
            });
        }

        public class SondaRequest
        {
            /// <summary>1 = Tratamiento (default) · 2 = Síntoma.</summary>
            public int Tipo { get; set; } = 1;

            public List<string?> Nombres { get; set; } = new();
        }
    }
}
