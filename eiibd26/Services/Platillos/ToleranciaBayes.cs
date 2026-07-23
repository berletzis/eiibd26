using System;

namespace eiibd26.Services.Platillos
{
    /// <summary>
    /// Resultado del posterior Beta-Binomial para un par (segmento, ingrediente).
    /// Porcentajes en 0..100 (no fracciones) porque así se muestran.
    /// </summary>
    public sealed class ToleranciaEstimacion
    {
        /// <summary>Votos "Sí" (éxitos).</summary>
        public int Si { get; init; }
        /// <summary>Votos "No" (fracasos). "A veces" NO entra aquí — queda fuera del binario.</summary>
        public int No { get; init; }
        /// <summary>Sí + No. OJO: no es la n total de la encuesta (esa incluye "A veces").</summary>
        public int NBinario => Si + No;

        /// <summary>Media posterior E[θ] × 100 — es el "X %" que se muestra.</summary>
        public double MediaPct { get; init; }
        /// <summary>Cuantil 0.025 del posterior × 100.</summary>
        public double CiBajoPct { get; init; }
        /// <summary>Cuantil 0.975 del posterior × 100.</summary>
        public double CiAltoPct { get; init; }
        /// <summary>Ancho del intervalo creíble en puntos porcentuales. Es la medida de incertidumbre del gate.</summary>
        public double AnchoPct => CiAltoPct - CiBajoPct;

        public int MediaRedondeada => (int)Math.Round(MediaPct);
        public int CiBajoRedondeado => (int)Math.Round(CiBajoPct);
        public int CiAltoRedondeado => (int)Math.Round(CiAltoPct);
    }

    /// <summary>
    /// Módulo #16 — modelo bayesiano de tolerancia alimentaria (Beta-Binomial conjugado).
    /// FUENTE ÚNICA de la matemática: la encuesta pública (/tolero/{slug}) y el panel admin
    /// (EstadisticasTolerancia) DEBEN dar el mismo número para el mismo (segmento, ingrediente).
    ///
    /// Formulación:
    ///   prior      θ ~ Beta(α₀, β₀), default Beta(1,1) — neutro y equivalente al Laplace del MVP.
    ///   posterior  θ | datos ~ Beta(α₀ + s, β₀ + f)   con s = "Sí", f = "No".
    ///   puntual    E[θ] = (α₀+s) / (α₀+β₀+s+f)        ← el "X %" (con Beta(1,1) es (s+1)/(s+f+2)).
    ///   incert.    IC 95% = cuantiles 0.025 y 0.975 del posterior.
    ///
    /// "A veces" queda FUERA del binario a propósito (igual que el MVP): se reporta aparte como
    /// contexto. Crédito parcial / modelo ordinal de 3 niveles = fuera de alcance.
    ///
    /// Clase pura: NO toca BD, NO tiene estado, NO se inyecta.
    /// </summary>
    public static class ToleranciaBayes
    {
        /// <summary>Prior por defecto: Beta(1,1), uniforme. Continuidad exacta con el % del MVP.</summary>
        public const double PriorAlfa = 1.0;
        public const double PriorBeta = 1.0;

        /// <summary>Piso de respuestas (incluyendo "A veces") para revelar el porcentaje. Piso histórico del MVP.</summary>
        public const int MinVotos = 10;

        /// <summary>
        /// Ancho máximo tolerado del IC 95%, en puntos. Si el intervalo es más ancho que esto, el dato
        /// no es informativo y NO se muestra (aunque n ≥ MinVotos). Con un consenso partido (≈50/50)
        /// esto exige ~20 votos binarios; con un consenso marcado (≈80/20), ~13.
        /// </summary>
        public const double MaxAnchoIcPct = 40.0;

        /// <summary>
        /// Posterior Beta-Binomial. <paramref name="si"/> y <paramref name="no"/> son conteos de votos;
        /// "A veces" no se pasa aquí.
        /// </summary>
        public static ToleranciaEstimacion Estimar(int si, int no, double a0 = PriorAlfa, double b0 = PriorBeta)
        {
            if (si < 0) si = 0;
            if (no < 0) no = 0;
            if (a0 <= 0) a0 = PriorAlfa;
            if (b0 <= 0) b0 = PriorBeta;

            double a = a0 + si;
            double b = b0 + no;

            double media = a / (a + b);
            double lo = CuantilBeta(0.025, a, b);
            double hi = CuantilBeta(0.975, a, b);

            return new ToleranciaEstimacion
            {
                Si = si,
                No = no,
                MediaPct = media * 100.0,
                CiBajoPct = lo * 100.0,
                CiAltoPct = hi * 100.0
            };
        }

        /// <summary>
        /// Gate de confiabilidad (§5 del REQ): se muestra el porcentaje solo si hay suficientes
        /// respuestas Y el intervalo es suficientemente angosto. Si no pasa: "aún no hay suficientes
        /// respuestas" — NUNCA un porcentaje.
        /// </summary>
        /// <param name="nTotal">Respuestas totales del segmento, INCLUYENDO "A veces".</param>
        public static bool PasaGate(
            ToleranciaEstimacion e,
            int nTotal,
            int minVotos = MinVotos,
            double maxAnchoPct = MaxAnchoIcPct)
        {
            if (e == null) return false;
            if (e.NBinario <= 0) return false;             // solo "A veces" → no hay binario que estimar
            if (nTotal < minVotos) return false;
            return e.AnchoPct <= maxAnchoPct;
        }

        // ===================================================================================
        // Numérico: cuantil de la Beta. Sin dependencias externas (decisión consciente — el
        // BCL no trae Beta.InvCDF y no queremos un paquete nuevo en el proyecto del paciente).
        // ===================================================================================

        /// <summary>
        /// Cuantil (inverso de la CDF) de Beta(a,b) por bisección sobre I_x(a,b), que es continua y
        /// estrictamente creciente en x. 200 iteraciones ⇒ precisión muy por debajo del redondeo a %.
        /// </summary>
        private static double CuantilBeta(double p, double a, double b)
        {
            if (p <= 0) return 0.0;
            if (p >= 1) return 1.0;

            double lo = 0.0, hi = 1.0;
            for (int i = 0; i < 200; i++)
            {
                double mid = (lo + hi) / 2.0;
                if (BetaIncompletaRegularizada(mid, a, b) < p) lo = mid; else hi = mid;
                if (hi - lo < 1e-12) break;
            }
            return (lo + hi) / 2.0;
        }

        /// <summary>
        /// Beta incompleta regularizada I_x(a,b) = CDF de Beta(a,b) en x.
        /// Fracción continua de Lentz (Numerical Recipes, betai/betacf), con el cambio de cola
        /// I_x(a,b) = 1 − I_{1−x}(b,a) donde la fracción converge mal.
        /// </summary>
        private static double BetaIncompletaRegularizada(double x, double a, double b)
        {
            if (x <= 0.0) return 0.0;
            if (x >= 1.0) return 1.0;

            double bt = Math.Exp(
                LogGamma(a + b) - LogGamma(a) - LogGamma(b)
                + a * Math.Log(x) + b * Math.Log(1.0 - x));

            return x < (a + 1.0) / (a + b + 2.0)
                ? bt * FraccionContinuaBeta(x, a, b) / a
                : 1.0 - bt * FraccionContinuaBeta(1.0 - x, b, a) / b;
        }

        /// <summary>Fracción continua de la beta incompleta, evaluada con el método de Lentz modificado.</summary>
        private static double FraccionContinuaBeta(double x, double a, double b)
        {
            const int MaxIter = 300;
            const double Eps = 3.0e-14;
            const double FpMin = 1.0e-300;

            double qab = a + b, qap = a + 1.0, qam = a - 1.0;
            double c = 1.0;
            double d = 1.0 - qab * x / qap;
            if (Math.Abs(d) < FpMin) d = FpMin;
            d = 1.0 / d;
            double h = d;

            for (int m = 1; m <= MaxIter; m++)
            {
                int m2 = 2 * m;

                // Paso par
                double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1.0 + aa * d;
                if (Math.Abs(d) < FpMin) d = FpMin;
                c = 1.0 + aa / c;
                if (Math.Abs(c) < FpMin) c = FpMin;
                d = 1.0 / d;
                h *= d * c;

                // Paso impar
                aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
                d = 1.0 + aa * d;
                if (Math.Abs(d) < FpMin) d = FpMin;
                c = 1.0 + aa / c;
                if (Math.Abs(c) < FpMin) c = FpMin;
                d = 1.0 / d;
                double del = d * c;
                h *= del;

                if (Math.Abs(del - 1.0) < Eps) break;
            }
            return h;
        }

        // Coeficientes de Lanczos (g = 7, n = 9) — precisión ~15 dígitos, de sobra para conteos de votos.
        private static readonly double[] LanczosG7 =
        {
            0.99999999999980993,
            676.5203681218851,
            -1259.1392167224028,
            771.32342877765313,
            -176.61502916214059,
            12.507343278686905,
            -0.13857109526572012,
            9.9843695780195716e-6,
            1.5056327351493116e-7
        };

        /// <summary>ln Γ(z) por aproximación de Lanczos. El BCL no expone LogGamma.</summary>
        private static double LogGamma(double z)
        {
            if (z < 0.5)
            {
                // Reflexión: Γ(z)Γ(1−z) = π / sin(πz)
                return Math.Log(Math.PI / Math.Sin(Math.PI * z)) - LogGamma(1.0 - z);
            }

            z -= 1.0;
            double x = LanczosG7[0];
            for (int i = 1; i < 9; i++) x += LanczosG7[i] / (z + i);
            double t = z + 7.5;
            return 0.5 * Math.Log(2.0 * Math.PI) + (z + 0.5) * Math.Log(t) - t + Math.Log(x);
        }
    }
}
