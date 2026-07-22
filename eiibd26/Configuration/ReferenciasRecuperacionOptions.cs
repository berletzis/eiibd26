using System.Collections.Generic;

namespace eiibd26.Configuration
{
    /// <summary>
    /// Config del motor de RECUPERACIÓN de referencias reales para las notas de Platillos
    /// (Nivel 1 del REQ: índice del crawler + embeddings). Los links SIEMPRE se recuperan del
    /// índice; el modelo NUNCA escribe una URL. Editable en appsettings sin recompilar.
    /// </summary>
    public class ReferenciasRecuperacionOptions
    {
        public const string SectionName = "ReferenciasRecuperacion";

        /// <summary>Interruptor general. Si está apagado o Voyage no tiene key, no se recupera nada
        /// (degradación limpia → sin candidatos, la nota queda con su leyenda honesta de siempre).</summary>
        public bool Habilitado { get; set; } = true;

        /// <summary>Dominios de confianza de los que SÍ se ofrecen candidatos (subcadena del dominio,
        /// ej. "funeiico.com"). Solo se recupera de sitios crawleados que matcheen esta lista.
        /// Cuando el crawler indexe Mayo/CCF, basta agregarlos aquí — sin tocar código.</summary>
        public List<string> DominiosConfiables { get; set; } = new();

        /// <summary>Piso de similitud coseno para considerar una página como candidata. 0.55 = "mismo
        /// tema" (nivel 'área' del Motor de Cobertura). Recuperar es sugerir, no afirmar: el humano valida.</summary>
        public double UmbralCoseno { get; set; } = 0.55;

        /// <summary>Máximo de candidatos a ofrecer por nota.</summary>
        public int TopK { get; set; } = 5;
    }
}
