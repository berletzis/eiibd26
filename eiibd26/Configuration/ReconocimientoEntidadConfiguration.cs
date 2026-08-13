namespace eiibd26.Configuration
{
    /// <summary>
    /// Gate de reconocimiento de entidad: impide que NINA genere una ficha para un término
    /// que no reconoce (caso Aangamik — se describió un suplemento de venta libre como
    /// "inyectable indicado para colitis ulcerosa").
    /// </summary>
    /// <remarks>
    /// Sección de appsettings: <c>ReconocimientoEntidad</c>. Todo es calibrable sin redeploy.
    /// </remarks>
    public class ReconocimientoEntidadConfiguration
    {
        /// <summary>
        /// Feature flag del gate. En <c>false</c> el gate deja pasar todo (comportamiento
        /// anterior al REQ) y solo queda el guardrail del generador como red.
        /// Default <c>true</c>: ante un despliegue sin configuración explícita, la opción
        /// segura es la que NO publica fichas de términos desconocidos.
        /// </summary>
        public bool Habilitado { get; set; } = true;

        /// <summary>
        /// Confianza mínima del triage por nombre para tomar una decisión firme
        /// (Reconocido / NoReconocido). Por debajo del umbral todo cae en RevisionHumana.
        /// Subirlo = más cola humana y menos automatismo; bajarlo = más decisiones solas.
        /// </summary>
        public double ConfianzaMinima { get; set; } = 0.80;

        /// <summary>
        /// Si un término resulta NoReconocido, desactivar su término del glosario
        /// (<c>GlossaryTerm.Activo = false</c>). Solo aplica a términos SIN respaldo humano:
        /// el Tier 0 (allowlist) nunca produce NoReconocido, así que un registro validado por
        /// un médico jamás puede llegar aquí. En <c>false</c> el gate solo bloquea la
        /// generación y sella Dudoso, sin tocar la visibilidad.
        /// </summary>
        public bool DesactivarNoReconocidos { get; set; } = true;
    }
}
