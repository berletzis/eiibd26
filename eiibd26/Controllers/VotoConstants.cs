namespace eiibd26.Controllers
{
    /// <summary>
    /// Valores canónicos para el campo EntidadTipo de la tabla Votos.
    /// Usar siempre estas constantes para evitar inconsistencias de case.
    /// IMPORTANTE: El score de una entidad se calcula en runtime desde la tabla Votos
    /// (SUM de Valor WHERE !Eliminado). El campo Respuesta.Puntuacion NO se sincroniza
    /// y no debe usarse como fuente de verdad del score.
    /// </summary>
    internal static class VotoTipo
    {
        internal const string Pregunta = "pregunta";
        internal const string Respuesta = "respuesta";
    }
}
