using System;

namespace eiibd26.Helpers
{
    /// <summary>
    /// Centraliza el cálculo de edad y duración de condición para
    /// garantizar resultados consistentes en todo el sistema.
    /// Regla: siempre usa DateTime.Today como referencia, nunca DateTime.Now.
    /// </summary>
    public static class DateCalculationHelper
    {
        /// <summary>
        /// Calcula la edad en años completos al momento del diagnóstico.
        /// </summary>
        public static int? CalcularEdadAlDiagnostico(DateTime? fechaNacimiento, DateTime? fechaDiagnostico)
        {
            if (!fechaNacimiento.HasValue || !fechaDiagnostico.HasValue)
                return null;

            var nac = fechaNacimiento.Value;
            var dx  = fechaDiagnostico.Value;

            if (nac > dx)
                return null;

            var edad = dx.Year - nac.Year;
            if (dx < nac.AddYears(edad))
                edad--;

            return edad >= 0 && edad < 120 ? edad : null;
        }

        /// <summary>
        /// Calcula los años y meses transcurridos desde la fecha de diagnóstico
        /// hasta la fecha de referencia (usar DateTime.Today).
        /// </summary>
        public static (int Anios, int Meses) CalcularDuracion(DateTime fechaDiagnostico, DateTime fechaReferencia)
        {
            if (fechaDiagnostico > fechaReferencia)
                return (0, 0);

            int anios = fechaReferencia.Year - fechaDiagnostico.Year;
            int meses = fechaReferencia.Month - fechaDiagnostico.Month;

            if (fechaReferencia.Day < fechaDiagnostico.Day)
                meses--;

            if (meses < 0)
            {
                anios--;
                meses += 12;
            }

            return (anios, meses);
        }

        /// <summary>
        /// Devuelve el texto de duración formateado en español.
        /// Ejemplos: "1 año y 3 meses", "5 meses", "Reciente"
        /// </summary>
        public static string FormatearDuracion(DateTime? fechaDiagnostico, DateTime? fechaReferencia = null)
        {
            if (!fechaDiagnostico.HasValue)
                return string.Empty;

            // Fechas inválidas (DateTime.MinValue, año 0001, etc.) no producen duración
            if (fechaDiagnostico.Value.Year < 1900)
                return string.Empty;

            var referencia = fechaReferencia ?? DateTime.Today;
            var (anios, meses) = CalcularDuracion(fechaDiagnostico.Value, referencia);

            if (anios > 0 && meses > 0)
                return $"{anios} {(anios == 1 ? "año" : "años")} y {meses} {(meses == 1 ? "mes" : "meses")}";

            if (anios > 0)
                return $"{anios} {(anios == 1 ? "año" : "años")}";

            if (meses > 0)
                return $"{meses} {(meses == 1 ? "mes" : "meses")}";

            return "Reciente";
        }
    }
}
