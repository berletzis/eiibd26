namespace eiibd26.Services.AI
{
    /// <summary>Qué tipo de catálogo se está evaluando.</summary>
    public enum TipoEntidadClinica
    {
        Tratamiento = 1,
        Sintoma = 2
    }

    /// <summary>
    /// Veredicto del gate de reconocimiento. Las cuatro salidas NO son intercambiables:
    /// solo <see cref="Reconocido"/> autoriza generar contenido, y solo
    /// <see cref="NoReconocido"/> puede llegar a despublicar. El resto es "no sé": no
    /// publica, pero tampoco destruye.
    /// </summary>
    public enum ResultadoReconocimiento
    {
        /// <summary>El término existe y es lo que dice ser → se puede generar la ficha.</summary>
        Reconocido = 1,

        /// <summary>El término NO nombra una entidad clínica real → no se genera y se saca de circulación.</summary>
        NoReconocido = 2,

        /// <summary>Ambiguo o poca confianza → no se genera, va a la cola humana, NO se despublica.</summary>
        RevisionHumana = 3,

        /// <summary>
        /// No hubo forma de verificar (API caída, timeout, sin crédito). Fail-safe ASIMÉTRICO:
        /// no se genera nada nuevo y NO se modifica NADA en la BD. Un outage jamás despublica.
        /// </summary>
        GroundingNoDisponible = 4
    }

    /// <summary>
    /// Decisión del gate para un término concreto.
    /// </summary>
    /// <param name="Resultado">Veredicto.</param>
    /// <param name="Fuente">Quién decidió: <c>allowlist</c> (Tier 0), <c>triage-nombre</c> (Tier 1),
    /// <c>gate-deshabilitado</c> (feature flag apagado) o <c>error</c>.</param>
    /// <param name="Confianza">0–1. En Tier 0 siempre 1.</param>
    /// <param name="Motivo">Frase corta para el log, el sello de triage y la sonda de aceptación.</param>
    public record DecisionReconocimiento(
        ResultadoReconocimiento Resultado,
        string Fuente,
        double Confianza,
        string Motivo)
    {
        /// <summary>Única condición que autoriza llamar al generador de descripciones.</summary>
        public bool PermiteGenerar => Resultado == ResultadoReconocimiento.Reconocido;

        /// <summary>
        /// Un outage no es un veredicto: con esto en <c>true</c> el caller NO debe escribir
        /// absolutamente nada en la BD (ni sello de triage, ni Activo).
        /// </summary>
        public bool EsSinVeredicto => Resultado == ResultadoReconocimiento.GroundingNoDisponible;
    }

    /// <summary>
    /// Datos que el caller ya tiene en mano del registro a evaluar. Se pasan en vez de re-leer
    /// la fila: el servicio solo consulta lo que le falta (validaciones del glosario).
    /// </summary>
    /// <param name="Id">Id en <c>tratamientos</c> / <c>sintomas</c>.</param>
    /// <param name="Nombre">Nombre del registro.</param>
    /// <param name="ValidadoHumano">Flag de la propia tabla.</param>
    /// <param name="ValidadoIA">Si la descripción actual la escribió la IA.</param>
    /// <param name="DescripcionExistente">Descripción actual, si la hay.</param>
    public record EntradaReconocimiento(
        int Id,
        string? Nombre,
        bool ValidadoHumano,
        bool ValidadoIA,
        string? DescripcionExistente);

    /// <summary>
    /// Gate que decide si NINA puede escribir una ficha sobre un término.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Motivo: en <c>/Termino/aangamik</c> NINA describió una marca comercial de suplemento de
    /// DMG como "medicamento inyectable indicado para colitis ulcerosa moderada a grave", con
    /// RELACIÓN EII = SÍ. El generador tenía prohibido inventar FUENTES, pero nada le impedía
    /// inventar la vía, la indicación y el fármaco entero.
    /// </para>
    /// <para>
    /// No es un clasificador nuevo: envuelve el triage por NOMBRE que ya está calibrado
    /// (Haiku, temp 0.0, rúbrica con sesgo obligatorio a conservar) y le pone contrato
    /// fail-safe. Los embeddings de <c>ReferenciaRecuperacionService</c> quedan FUERA del
    /// camino de decisión: miden parecido temático, así que dejarían pasar "Aangamik" y
    /// suprimirían fármacos reales poco citados.
    /// </para>
    /// </remarks>
    public interface IReconocimientoEntidadService
    {
        /// <summary>Feature flag activo. En false el gate deja pasar todo.</summary>
        bool Habilitado { get; }

        /// <summary>Umbral de confianza vigente (para mostrarlo en la sonda de aceptación).</summary>
        double ConfianzaMinima { get; }

        /// <summary>
        /// Si un <see cref="ResultadoReconocimiento.NoReconocido"/> debe además sacar el término
        /// del glosario. En <c>false</c> el gate solo bloquea la generación y sella Dudoso.
        /// </summary>
        bool DesactivaNoReconocidos { get; }

        /// <summary>
        /// Evalúa un registro del catálogo: Tier 0 (allowlist en BD, sin red) y, si no aplica,
        /// Tier 1 (triage por NOMBRE). Nunca escribe en la BD.
        /// </summary>
        Task<DecisionReconocimiento> EvaluarAsync(
            TipoEntidadClinica tipo,
            EntradaReconocimiento entrada,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Solo Tier 1, a partir de un nombre suelto. Sin BD y sin escritura: es lo que usa la
        /// sonda de aceptación para calibrar el umbral antes de activar el gate.
        /// </summary>
        Task<DecisionReconocimiento> SondaPorNombreAsync(
            TipoEntidadClinica tipo,
            string nombre,
            CancellationToken cancellationToken = default);
    }
}
