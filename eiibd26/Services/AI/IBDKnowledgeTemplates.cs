namespace eiibd26.Services.AI;

/// <summary>
/// Repositorio de conocimiento EII (Enfermedad Inflamatoria Intestinal) para respuestas
/// locales sin costo. Solo cubre el dominio clínico de la plataforma: CUCI, Crohn,
/// biológicos, brotes, colonoscopias, medicamentos, nutrición y seguimiento.
/// </summary>
internal static class IBDKnowledgeTemplates
{
    /// <summary>
    /// Intenta resolver una pregunta simple con conocimiento local EII.
    /// Devuelve null si el tema no está cubierto (debe escalarse a IA).
    /// </summary>
    public static string? TryResolve(string tituloNormalizado)
    {
        // CUCI / Colitis Ulcerosa
        if (ContainsAny(tituloNormalizado, "qué es", "que es", "definición", "definicion") &&
            ContainsAny(tituloNormalizado, "cuci", "colitis ulcerosa", "colitis ulcerativa"))
            return CuciDefinicion;

        // Crohn
        if (ContainsAny(tituloNormalizado, "qué es", "que es", "definición", "definicion") &&
            ContainsAny(tituloNormalizado, "crohn", "enfermedad de crohn"))
            return CrohnDefinicion;

        // EII general
        if (ContainsAny(tituloNormalizado, "qué es", "que es", "definición", "definicion") &&
            ContainsAny(tituloNormalizado, "eii", "enfermedad inflamatoria intestinal"))
            return EiiDefinicion;

        // Diferencia entre CUCI y Crohn
        if (ContainsAny(tituloNormalizado, "diferencia", "cuci", "colitis") &&
            ContainsAny(tituloNormalizado, "crohn"))
            return DiferenciaCuciCrohn;

        // Brotes
        if (ContainsAny(tituloNormalizado, "brote", "recaída", "recaida", "exacerbación", "exacerbacion"))
            return InformacionBrotes;

        // Biológicos
        if (ContainsAny(tituloNormalizado, "biológico", "biologico", "anti-tnf", "anti tnf",
            "infliximab", "adalimumab", "vedolizumab", "ustekinumab", "ozanimod"))
            return InformacionBiologicos;

        // Colonoscopia
        if (ContainsAny(tituloNormalizado, "colonoscopia", "endoscopia", "preparación colonoscopia",
            "preparacion colonoscopia"))
            return InformacionColonoscopia;

        // Medicamentos base
        if (ContainsAny(tituloNormalizado, "mesalazina", "mesalamine", "5-asa", "5asa"))
            return InformacionMesalazina;

        if (ContainsAny(tituloNormalizado, "azatioprina", "azathioprine", "inmunosupresor",
            "immunosuppressant"))
            return InformacionAzatioprina;

        if (ContainsAny(tituloNormalizado, "corticoide", "cortisona", "prednisona", "budesonida",
            "esteroide"))
            return InformacionCorticoides;

        // Nutrición
        if (ContainsAny(tituloNormalizado, "dieta", "nutrición", "nutricion", "alimentos",
            "qué comer", "que comer", "alimentación", "alimentacion"))
            return InformacionNutricion;

        // Seguimiento médico
        if (ContainsAny(tituloNormalizado, "seguimiento", "control", "revisión medica",
            "revision medica", "cita", "consulta"))
            return InformacionSeguimiento;

        // Síntomas comunes EII
        if (ContainsAny(tituloNormalizado, "síntoma", "sintoma", "síntomas frecuentes",
            "sintomas frecuentes", "manifestación", "manifestacion"))
            return InformacionSintomas;

        return null;
    }

    /// <summary>Respuesta de dominio incorrecto para bloquear preguntas fuera de EII.</summary>
    public static string RespuestaDominioIncorrecto =>
        """
        Esta pregunta no corresponde al dominio de la plataforma EIIBD, que está especializada
        en Enfermedad Inflamatoria Intestinal (EII): Colitis Ulcerosa Crónica Idiopática (CUCI)
        y Enfermedad de Crohn.

        Para preguntas sobre otros temas de salud, consulta a un profesional médico especializado
        o visita fuentes de información de tu país (servicios de salud, IMSS, ISSSTE, etc.).
        """;

    // =========================================================================
    // CUCI
    // =========================================================================
    private const string CuciDefinicion =
        """
        La **Colitis Ulcerosa Crónica Idiopática (CUCI)** es una enfermedad inflamatoria
        intestinal (EII) que afecta el colon (intestino grueso) y el recto. Produce inflamación
        continua y úlceras en la mucosa del intestino grueso.

        **Características principales:**
        • Afecta exclusivamente el colon y el recto
        • La inflamación es continua (no hay "zonas sanas" entre las áreas inflamadas)
        • Síntomas frecuentes: diarrea con sangre, dolor abdominal tipo cólico, urgencia para ir al baño
        • Es crónica: alterna períodos de actividad (brotes) y remisión

        **Diagnóstico:**
        Se confirma mediante colonoscopia con biopsias.

        Consulta siempre con tu gastroenterólogo sobre tu situación individual.
        """;

    // =========================================================================
    // Crohn
    // =========================================================================
    private const string CrohnDefinicion =
        """
        La **Enfermedad de Crohn** es una enfermedad inflamatoria intestinal (EII) que puede
        afectar cualquier segmento del tracto digestivo, desde la boca hasta el ano, aunque es
        más frecuente en el íleon terminal (parte final del intestino delgado) y el colon.

        **Características principales:**
        • Puede afectar cualquier parte del tracto gastrointestinal
        • La inflamación puede ser transmural (todas las capas de la pared intestinal)
        • Puede presentar "zonas sanas" entre áreas inflamadas (distribución en parches)
        • Síntomas frecuentes: dolor abdominal, diarrea, pérdida de peso, fatiga
        • Puede haber complicaciones: fístulas, estenosis, abscesos

        **Diagnóstico:**
        Requiere combinación de colonoscopia, estudios de imagen y análisis de laboratorio.

        Consulta siempre con tu gastroenterólogo sobre tu situación individual.
        """;

    // =========================================================================
    // EII General
    // =========================================================================
    private const string EiiDefinicion =
        """
        La **Enfermedad Inflamatoria Intestinal (EII)** es un grupo de enfermedades crónicas
        del sistema digestivo caracterizadas por inflamación persistente del tracto gastrointestinal.

        **Principales tipos:**
        • **CUCI (Colitis Ulcerosa Crónica Idiopática):** afecta solo el colon y el recto
        • **Enfermedad de Crohn:** puede afectar cualquier parte del tracto digestivo

        **Causas:**
        No existe una causa única conocida. Se cree que resulta de la interacción entre factores
        genéticos, el sistema inmunológico y el entorno/microbioma intestinal.

        **Impacto:**
        La EII es crónica pero tratable. Con el manejo adecuado, la mayoría de los pacientes
        pueden mantener períodos prolongados de remisión y buena calidad de vida.

        Habla con tu gastroenterólogo para orientación personalizada.
        """;

    // =========================================================================
    // Diferencia CUCI vs Crohn
    // =========================================================================
    private const string DiferenciaCuciCrohn =
        """
        **Diferencias principales entre CUCI y Crohn:**

        | Característica | CUCI | Crohn |
        |---|---|---|
        | Localización | Solo colon y recto | Todo el tracto GI |
        | Patrón inflamación | Continuo | En parches (skip lesions) |
        | Profundidad | Mucosa superficial | Transmural (todas las capas) |
        | Sangrado rectal | Frecuente | Menos frecuente |
        | Fístulas y abscesos | Raros | Frecuentes |

        Ambas son crónicas, con períodos de brote y remisión. El tratamiento difiere
        según el tipo y la extensión de la enfermedad.

        Consulta siempre con tu gastroenterólogo para saber cuál te afecta y cuál es el
        mejor plan de tratamiento para tu caso.
        """;

    // =========================================================================
    // Brotes
    // =========================================================================
    private const string InformacionBrotes =
        """
        Un **brote** (también llamado exacerbación o recaída) es un período en que la
        enfermedad inflamatoria intestinal está activa y los síntomas reaparecen o empeoran.

        **Señales de un posible brote:**
        • Aumento en la frecuencia de evacuaciones
        • Reaparición o incremento de sangre en heces
        • Dolor abdominal más intenso
        • Fatiga marcada
        • Fiebre (señal de alarma — consulta de inmediato)

        **Qué hacer:**
        • Contacta a tu gastroenterólogo antes de modificar medicamentos
        • No suspendas ni aumentes tratamientos sin indicación médica
        • Lleva registro de síntomas (duración, intensidad, evacuaciones al día)

        **Señales de urgencia** que requieren atención inmediata:
        • Sangrado abundante
        • Fiebre alta (>38.5 °C)
        • Dolor abdominal severo
        • Señales de deshidratación

        Tu equipo médico es la mejor guía para el manejo de tu brote.
        """;

    // =========================================================================
    // Biológicos
    // =========================================================================
    private const string InformacionBiologicos =
        """
        Los **medicamentos biológicos** son tratamientos avanzados para la EII, indicados
        generalmente cuando los tratamientos convencionales no son suficientes o en
        enfermedad moderada a grave.

        **Tipos principales usados en EII:**
        • **Anti-TNF:** infliximab (Remicade), adalimumab (Humira)
        • **Anti-integrinas:** vedolizumab (Entyvio)
        • **Anti-IL-12/23:** ustekinumab (Stelara)
        • **Moduladores S1P:** ozanimod (Zeposia)
        • **JAK inhibidores:** tofacitinib (Xeljanz), upadacitinib (Rinvoq)

        **Consideraciones importantes:**
        • Requieren prescripción y seguimiento por gastroenterólogo especializado
        • Pueden requerir estudios previos (radiografía de tórax, PPD, hepatitis)
        • Es necesario monitorear con análisis regulares
        • NO suspendas ni inicies un biológico sin indicación de tu médico

        Consulta con tu gastroenterólogo si un biológico es adecuado para tu caso.
        """;

    // =========================================================================
    // Colonoscopia
    // =========================================================================
    private const string InformacionColonoscopia =
        """
        La **colonoscopia** es el estudio de referencia (gold standard) para el diagnóstico
        y seguimiento de la EII. Permite visualizar directamente la mucosa del colon
        y tomar biopsias.

        **¿Cuándo se indica?**
        • Diagnóstico inicial de EII
        • Evaluación de actividad de la enfermedad (brotes)
        • Vigilancia de displasia en pacientes con EII de larga evolución
        • Control después de cambio de tratamiento

        **Preparación general (tu médico dará instrucciones específicas):**
        • Dieta sin fibra 1-3 días antes
        • Ayuno completo horas antes del estudio
        • Uso de solución laxante (preparación intestinal)
        • Informe sobre todos tus medicamentos, especialmente anticoagulantes

        Sigue siempre las instrucciones específicas que te proporcione tu unidad de endoscopía.
        """;

    // =========================================================================
    // Mesalazina
    // =========================================================================
    private const string InformacionMesalazina =
        """
        La **mesalazina** (también llamada mesalamina o 5-ASA) es uno de los medicamentos
        base para el tratamiento de la Colitis Ulcerosa (CUCI), especialmente en enfermedad
        leve a moderada.

        **Formas de administración:**
        • Tabletas u oral
        • Supositorios (para enfermedad rectal)
        • Enemas (para enfermedad del colon izquierdo)

        **Uso:** Mantenimiento de remisión y tratamiento de brotes leves.

        **Efectos secundarios posibles:** náuseas, dolor de cabeza, diarrea leve.
        En raros casos puede afectar el riñón — se monitorea con análisis periódicos.

        No ajustes la dosis sin indicación médica. Habla con tu gastroenterólogo
        si tienes dudas sobre tu pauta actual.
        """;

    // =========================================================================
    // Azatioprina
    // =========================================================================
    private const string InformacionAzatioprina =
        """
        La **azatioprina** es un inmunosupresor utilizado en EII para mantener la remisión
        cuando la mesalazina no es suficiente o como complemento a biológicos.

        **Uso:** Mantenimiento de remisión en CUCI y Crohn moderados.

        **Aspectos clave:**
        • Tarda 3-6 meses en alcanzar su efecto completo
        • Requiere monitoreo regular de hemograma (puede afectar glóbulos blancos)
        • No se recomienda en embarazo sin evaluación cuidadosa
        • Riesgo de infecciones — informa a tu médico ante cualquier signo de infección

        Nunca modifiques la dosis ni suspendas la azatioprina sin consultar con tu
        gastroenterólogo. El seguimiento con análisis es obligatorio.
        """;

    // =========================================================================
    // Corticoides
    // =========================================================================
    private const string InformacionCorticoides =
        """
        Los **corticosteroides** (prednisona, budesonida, metilprednisolona) se usan en EII
        para controlar brotes moderados a graves. Son medicamentos de corto plazo.

        **Uso habitual:**
        • Inducción de remisión durante un brote
        • NO se recomiendan para mantenimiento a largo plazo

        **Efectos secundarios con uso prolongado:**
        • Aumento de peso y retención de líquidos
        • Alteraciones del sueño y ánimo
        • Hiperglucemia
        • Riesgo de osteoporosis
        • Mayor susceptibilidad a infecciones

        Los corticoides deben usarse solo bajo indicación médica y con plan de reducción
        gradual (no suspender abruptamente). Habla con tu gastroenterólogo sobre la
        duración y reducción de dosis.
        """;

    // =========================================================================
    // Nutrición
    // =========================================================================
    private const string InformacionNutricion =
        """
        La nutrición en EII es un componente importante del manejo, aunque no existe una
        dieta universal para todos los pacientes.

        **Principios generales:**
        • No existe una "dieta EII" única — cada paciente responde diferente
        • Durante un brote: dieta baja en residuos puede ayudar a reducir síntomas
        • En remisión: dieta equilibrada variada es el objetivo
        • Identificar y evitar alimentos personales desencadenantes

        **Nutrientes a vigilar:**
        • Hierro (riesgo de anemia por sangrado)
        • Vitamina D y calcio (especialmente con uso de corticoides)
        • Vitamina B12 (especialmente en Crohn ileocecal)
        • Ácido fólico (especialmente con metrotexate o azatioprina)

        **Cuándo buscar nutriólogo:**
        • Pérdida de peso involuntaria
        • Desnutrición
        • Necesidad de nutrición enteral o parenteral

        Solicita una valoración nutricional a tu equipo médico para orientación personalizada.
        """;

    // =========================================================================
    // Seguimiento
    // =========================================================================
    private const string InformacionSeguimiento =
        """
        El **seguimiento médico regular** es fundamental en EII para prevenir brotes,
        detectar complicaciones y ajustar el tratamiento oportunamente.

        **Frecuencia general (orientativa — tu médico definirá la tuya):**
        • En remisión estable: cada 6-12 meses
        • Durante ajuste de tratamiento o post-brote: cada 1-3 meses
        • Colonoscopia de vigilancia: según protocolo por años de evolución

        **Análisis de rutina habituales:**
        • Hemograma completo
        • Función hepática y renal
        • Proteína C reactiva y calprotectina fecal
        • Niveles de medicamento (según el fármaco)
        • Vitaminas y minerales según deficiencias previas

        Mantén actualizado a tu gastroenterólogo sobre cualquier cambio en tus síntomas,
        incluso entre citas programadas.
        """;

    // =========================================================================
    // Síntomas
    // =========================================================================
    private const string InformacionSintomas =
        """
        Los **síntomas de la EII** varían según el tipo (CUCI o Crohn) y la actividad
        de la enfermedad.

        **Síntomas intestinales comunes:**
        • Diarrea (frecuentemente con sangre en CUCI)
        • Dolor abdominal y cólicos
        • Urgencia para defecar
        • Tenesmo (sensación de evacuación incompleta)
        • Pérdida de peso involuntaria

        **Síntomas sistémicos:**
        • Fatiga y cansancio marcado
        • Fiebre (en brotes)
        • Anemia (por sangrado o deficiencia de hierro)

        **Manifestaciones extraintestinales (pueden acompañar la EII):**
        • Articulaciones: artritis, dolor articular
        • Piel: eritema nodoso, pioderma gangrenoso
        • Ojos: epiescleritis, uveítis
        • Hígado: colangitis esclerosante primaria (rara pero importante)

        Si experimentas síntomas nuevos o empeoramiento, comunícate con tu
        gastroenterólogo antes de hacer cambios en tu tratamiento.
        """;

    // =========================================================================
    // Helpers
    // =========================================================================
    private static bool ContainsAny(string text, params string[] terms)
        => terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}
