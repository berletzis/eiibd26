using eiibd26.Configuration;
using eiibd26.Models.Glossary;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Twilio.TwiML.Voice;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Implementación del servicio de generación de descripciones para síntomas y tratamientos
    /// </summary>
    public class SintomasTratamientosAiService : ISintomasTratamientosAiService
    {
        private readonly HttpClient _httpClient;
        private readonly AiAnswerConfiguration _config;
        private readonly ILogger<SintomasTratamientosAiService> _logger;

        public SintomasTratamientosAiService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiAnswerConfiguration> config,
            ILogger<SintomasTratamientosAiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("AnthropicClient");
            _config = config.Value;
            _logger = logger;
        }

        public async Task<(string Descripcion, bool RelacionEII, bool Reconocido)> GenerarDescripcionSintomaAsync(
            string nombreSintoma,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreSintoma))
                throw new ArgumentException("El nombre del síntoma no puede estar vacío", nameof(nombreSintoma));

            var systemPrompt = BuildSintomaSystemPrompt();
            var userPrompt = $@"Ahora genera el contenido para el siguiente síntoma: {nombreSintoma}

ANTES QUE NADA, decide si RECONOCES este nombre como un síntoma real y específico.
Si NO lo reconoces, o si el nombre parece una marca comercial, un código, un producto,
un tratamiento, un alimento o texto sin sentido: NO inventes nada. Responde ÚNICAMENTE
con el bloque de metadatos, empezando por 'RECONOCIDO: NO', y sin descripción.

ADEMÁS, al final del texto:

1. Determina si este síntoma tiene relación documentada con la Enfermedad Inflamatoria Intestinal (EII - Crohn o Colitis Ulcerosa).

2. Si tiene relación, explica brevemente POR QUÉ o CÓMO se relaciona con la EII (en 1-2 frases cortas).

3. SOLO si tienes certeza de fuentes específicas donde esta relación está documentada, menciónalas. Si NO estás seguro, NO inventes fuentes.

IMPORTANTE: Responde en texto plano, SIN usar markdown (sin **, sin *, sin #).

Formato de respuesta al final:
RECONOCIDO: [SÍ o NO — SÍ solo si reconoces el término como un síntoma real y específico]
RELACIÓN EII: [SÍ o NO]
NIVEL RELACIÓN EII: [SOLO si respondiste SÍ → escribe exactamente una de estas palabras: Directa, Indirecta o Secundaria. Si respondiste NO, escribe 'No aplica']
EXPLICACIÓN EII: [Si respondiste SÍ, explica la relación en 1-2 frases. Si respondiste NO, escribe 'No aplica']
RAZONAMIENTO GLOSARIO: [Frase clínica breve (máx 200 caracteres) para mostrar en el glosario. Si NO hay relación, escribe 'No aplica']
FUENTES: [SOLO si tienes certeza de fuentes específicas, menciónalas. Si no estás seguro, escribe 'No especificadas']

Si RECONOCIDO es NO, todos los demás campos van en 'No aplica' y RELACIÓN EII en NO.

Guía de niveles (solo para NIVEL RELACIÓN EII):
- Directa: síntoma que aparece como manifestación directa o extraintestinal documentada de la EII.
- Indirecta: síntoma frecuente en EII pero no exclusivo ni patognomónico.
- Secundaria: síntoma que aparece como consecuencia de tratamientos o complicaciones de la EII.
Si hay duda → elige Indirecta.

Cuando describas la relación con EII:
-NO expliques mecanismos biológicos.
- NO uses frases como causa, provoca, debido a.
- Usa lenguaje observacional: algunas personas reportan.
- Siempre aclara que puede ocurrir por otras razones. "
;

            return await CallClaudeApiAsync(systemPrompt, userPrompt, cancellationToken);
        }

        public async Task<(string Descripcion, bool RelacionEII, string? NombreTraducido, bool Reconocido)> GenerarDescripcionTratamientoAsync(
            string nombreTratamiento,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreTratamiento))
                throw new ArgumentException("El nombre del tratamiento no puede estar vacío", nameof(nombreTratamiento));

            var systemPrompt = BuildTratamientoSystemPrompt();
            var userPrompt = $@"Ahora genera el contenido para el siguiente tratamiento: {nombreTratamiento}

IMPORTANTE PRIMERO:
Si el nombre son números, un código, una palabra sin significado clínico, una marca
comercial que no reconoces, o cualquier cosa que NO reconozcas como un tratamiento real
y específico: NO inventes nada. Responde ÚNICAMENTE con el bloque de metadatos,
empezando por 'RECONOCIDO: NO', y sin descripción.

Si el nombre del tratamiento está en inglés u otro idioma, tradúcelo al español. Mantén los nombres comerciales entre paréntesis tal como están.

Ejemplo: 
- Entrada: 'Prednisone (Deltasone, Prednisone Intensol)'
- Traducción: 'Prednisona (Deltasone, Prednisone Intensol)'

ADEMÁS, al final del texto:

1. Determina si este tratamiento tiene relación documentada con la Enfermedad Inflamatoria Intestinal (EII - Crohn o Colitis Ulcerosa).

2. Si tiene relación, explica brevemente POR QUÉ o CÓMO se usa en EII (en 1-2 frases cortas).

3. SOLO si tienes certeza de fuentes específicas donde esta relación está documentada, menciónalas. Si NO estás seguro, NO inventes fuentes.

IMPORTANTE: Responde en texto plano, SIN usar markdown (sin **, sin *, sin #).

Formato de respuesta al final:
RECONOCIDO: [SÍ o NO — SÍ solo si reconoces el término como un tratamiento real y específico]
NOMBRE ESPAÑOL: [Nombre del tratamiento en español. Si ya está en español, escribe el mismo nombre]
RELACIÓN EII: [SÍ o NO]
NIVEL RELACIÓN EII: [SOLO si respondiste SÍ → escribe exactamente una de estas palabras: Directa, Indirecta o Secundaria. Si respondiste NO, escribe 'No aplica']
EXPLICACIÓN EII: [Si respondiste SÍ, explica la relación en 1-2 frases. Si respondiste NO, escribe 'No aplica']
RAZONAMIENTO GLOSARIO: [Frase clínica breve (máx 200 caracteres) para mostrar en el glosario. Si NO hay relación, escribe 'No aplica']
FUENTES: [SOLO si tienes certeza de fuentes específicas, menciónalas. Si no estás seguro, escribe 'No especificadas']

Si RECONOCIDO es NO, todos los demás campos van en 'No aplica' y RELACIÓN EII en NO.

Guía de niveles (solo para NIVEL RELACIÓN EII):
- Directa: tratamiento aprobado o indicado específicamente para la EII.
- Indirecta: tratamiento frecuentemente usado en EII pero no exclusivo (ej: analgésicos, antidiarreicos).
- Secundaria: tratamiento que se usa para manejar efectos secundarios o complicaciones del tratamiento de EII.
Si hay duda → elige Indirecta.

Cuando describas la relación con EII:
-NO expliques mecanismos biológicos.
- NO uses frases como causa, provoca, debido a.
- Usa lenguaje observacional: algunas personas reportan.
- Siempre aclara que puede ocurrir por otras razones. "
;

            return await CallClaudeApiTratamientoAsync(systemPrompt, userPrompt, cancellationToken);
        }

        /// <summary>Modelo económico fijo para el triage. Clasificar es barato y de alto volumen
        /// (~10k registros): no se deja al azar de la configuración general.</summary>
        private const string ModelHaikuClasificacion = "claude-haiku-4-5-20251001";

        /// <summary>
        /// Motivo centinela cuando la respuesta del triage no traía JSON. El gate de
        /// reconocimiento lo distingue de un Dudoso real: "no hubo respuesta legible" NO es un
        /// veredicto y no debe producir ninguna escritura.
        /// </summary>
        public const string MotivoTriageSinJson =
            "La IA no devolvió una clasificación legible — requiere revisión humana.";

        /// <summary>Motivo centinela cuando el JSON del triage venía malformado. Ver <see cref="MotivoTriageSinJson"/>.</summary>
        public const string MotivoTriageJsonInvalido =
            "La IA devolvió una clasificación malformada — requiere revisión humana.";

        public async Task<(byte Estado, double Confianza, string Motivo, MedicalRelationType? Nivel, string? Razonamiento)> ClasificarTratamientoAsync(
            string nombre,
            string? descripcionExistente,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del tratamiento no puede estar vacío", nameof(nombre));

            var systemPrompt = BuildTriageSystemPrompt();

            var contexto = string.IsNullOrWhiteSpace(descripcionExistente)
                ? "(sin descripción previa)"
                : descripcionExistente.Length > 600 ? descripcionExistente[..600] : descripcionExistente;

            var userPrompt = $@"Clasifica este registro del catálogo de tratamientos.

NOMBRE: {nombre}
DESCRIPCIÓN EXISTENTE: {contexto}

OJO con la descripción existente: fue generada por IA sin filtrar basura, así que un
registro-ruido puede traer una descripción que lo hace ver legítimo. Clasifica por el
NOMBRE; la descripción es solo contexto.

Responde ÚNICAMENTE con este JSON, sin texto alrededor ni markdown:
{{""estado"":""valido|basura|dudoso"",""confianza"":0.0,""motivo"":""máx 200 caracteres"",""nivel_eii"":""directa|indirecta|secundaria|ninguna"",""razonamiento"":""máx 200 caracteres o vacío""}}";

            var json = await CallClaudeJsonAsync(systemPrompt, userPrompt, cancellationToken);
            return ParseTriageResponse(json, nombre);
        }

        public async Task<(byte Estado, double Confianza, string Motivo, MedicalRelationType? Nivel, string? Razonamiento)> ClasificarSintomaAsync(
            string nombre,
            string? descripcionExistente,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del síntoma no puede estar vacío", nameof(nombre));

            var systemPrompt = BuildTriageSystemPromptSintomas();

            var contexto = string.IsNullOrWhiteSpace(descripcionExistente)
                ? "(sin descripción previa)"
                : descripcionExistente.Length > 600 ? descripcionExistente[..600] : descripcionExistente;

            var userPrompt = $@"Clasifica este registro del catálogo de síntomas.

NOMBRE: {nombre}
DESCRIPCIÓN EXISTENTE: {contexto}

OJO con la descripción existente: fue generada por IA sin filtrar basura, así que un
registro-ruido puede traer una descripción que lo hace ver legítimo. Clasifica por el
NOMBRE; la descripción es solo contexto.

Responde ÚNICAMENTE con este JSON, sin texto alrededor ni markdown:
{{""estado"":""valido|basura|dudoso"",""confianza"":0.0,""motivo"":""máx 200 caracteres"",""nivel_eii"":""directa|indirecta|secundaria|ninguna"",""razonamiento"":""máx 200 caracteres o vacío""}}";

            var json = await CallClaudeJsonAsync(systemPrompt, userPrompt, cancellationToken);
            return ParseTriageResponse(json, nombre);
        }

        /// <summary>
        /// Rúbrica del triage de SÍNTOMAS. No es la de tratamientos: aquí la pregunta es si el
        /// registro nombra algo que una persona SIENTE, no si es una intervención.
        /// </summary>
        private string BuildTriageSystemPromptSintomas()
        {
            return @"Eres un clasificador de catálogos médicos. Depuras un catálogo de síntomas
que durante años recibió altas libres de pacientes, así que arrastra ruido.

Debes decidir DOS cosas independientes. No las mezcles:

EJE 1 — ¿Es un síntoma de verdad? (esto es lo que decide el campo 'estado')
EJE 2 — ¿Qué relación tiene con la EII? (esto va en 'nivel_eii' y NO influye en el eje 1)

REGLA CENTRAL: un síntoma real que NO tiene nada que ver con la EII sigue siendo VÁLIDO.
La falta de relación con EII jamás lo convierte en basura. Un síntoma de migraña, de
alergia o de un embarazo es un síntoma real.

LA PREGUNTA QUE DECIDE EL EJE 1: ¿esto es algo que una persona SIENTE, NOTA o REPORTA en
su cuerpo o en su ánimo? Si la respuesta es sí, es un síntoma. Si lo que nombra es algo
que se HACE, se TOMA o se USA, no lo es.

RÚBRICA DEL EJE 1 — tres salidas posibles:

VALIDO — manifestación clínica que un paciente siente o reporta: molestia, sensación,
dolor, cambio corporal observable o signo que la propia persona nota.
Ejemplos: 'Dolor abdominal', 'Diarrea', 'Fatiga', 'Fiebre', 'Náusea', 'Artralgia',
'Aftas bucales', 'Urgencia defecatoria', 'Sangrado rectal', 'Pérdida de peso',
'Distensión abdominal', 'Caída del cabello', 'Ansiedad', 'Insomnio', 'Visión borrosa',
'Dolor en las articulaciones', 'Eritema nodoso'.
Cuenta aunque no sea exclusivo de la EII, aunque sea inespecífico y aunque sea propio de
otra especialidad o de otra condición. También cuenta el síntoma emocional o del ánimo
(tristeza, irritabilidad, angustia) y el signo que el paciente observa (hinchazón,
manchas en la piel, moco en las heces).

BASURA — NO es algo que una persona sienta:
· ruido, texto corrupto, nombres sin sentido o palabras sueltas sin significado clínico
· cadenas de prueba, códigos y capturas accidentales ('asdf', 'xxx', '123', 'prueba')
· NO-SÍNTOMAS mal capturados en el catálogo de síntomas:
  - tratamientos, cirugías, terapias y procedimientos ('Prednisona', 'Colonoscopía',
    'Resección intestinal', 'Fisioterapia')
  - medicamentos y suplementos ('Ibuprofeno', 'Vitamina D')
  - alimentos y bebidas ('Lactosa', 'Café')
  - dispositivos y productos ('Bolsa de ostomía', 'Almohadilla térmica')
  - actividades y eventos de vida ('Ir al gimnasio', 'Viaje', 'Cita con el médico')
  - nombres de personas, hospitales, marcas o lugares
· títulos, frases largas o notas del paciente que no nombran un síntoma

DUDOSO — ambiguo; lo revisa un humano:
· términos vagos o demasiado amplios ('Malestar', 'Raro', 'Problemas', 'Cambios')
· síntoma que parece real pero cuya pertenencia al catálogo prefieres que confirme
  un humano (redacción confusa, abreviatura no clara, posible duplicado)
· DIAGNÓSTICOS y ENFERMEDADES que no son un síntoma en sí ('Colitis ulcerosa',
  'Anemia', 'Osteoporosis', 'Depresión mayor'). Están cerca del dominio clínico y muchos
  se capturaron por lo que la persona siente: SIEMPRE 'dudoso', NUNCA 'basura'. Que un
  humano decida si se queda como síntoma o se mueve a condiciones.
· HALLAZGOS DE LABORATORIO o de estudio ('Hemoglobina baja', 'PCR elevada',
  'Calprotectina alta'): no se sienten, pero describen el estado del paciente. 'dudoso'.
· CATEGORÍAS o agrupadores del catálogo ('Síntomas digestivos', 'Síntomas generales',
  'Manifestaciones extraintestinales'): no son un síntoma concreto, pero organizan el
  catálogo y otros registros cuelgan de ellos. Nunca son 'basura'.

REGLA FIRME sobre qué puede ser 'basura': a 'basura' solo van dos cosas — el ruido sin
significado clínico y el no-síntoma claro y evidente (un fármaco, un alimento, una
cirugía, un dispositivo, una actividad, un nombre propio). Todo lo demás que dudes es
'dudoso'.

LOS SÍNTOMAS REALES NUNCA SON 'BASURA'. Esta regla manda sobre todo lo anterior. Si el
registro nombra algo que una persona puede sentir, nunca lo clasifiques como 'basura' —
aunque sea de otra condición, aunque sea inespecífico, aunque no tenga ninguna relación
con la EII, y aunque te parezca menor o poco importante.
COHERENCIA: dos registros de la misma familia se clasifican igual. Si 'Dolor de cabeza'
es 'valido', 'Cefalea tensional' también lo es; si 'Anemia' es 'dudoso', 'Anemia
ferropénica' no puede ser 'basura'.

SESGO OBLIGATORIO A CONSERVAR: ante cualquier duda responde 'dudoso', NUNCA 'basura'.
'basura' se reserva para casos donde estarías dispuesto a defender la decisión.
Marcar de más como basura desactiva síntomas reales que los pacientes usan para
registrar cómo se sienten: es el error caro. Marcar de más como dudoso solo cuesta
trabajo humano: es el error barato.

CONFIANZA: número entre 0 y 1 que refleja qué tan seguro estás del 'estado'. Si dudas,
baja la confianza — no cambies 'basura' por una confianza alta inventada.

EJE 2 — 'nivel_eii' (independiente, no afecta 'estado'):
· directa: manifestación directa o extraintestinal documentada de la EII
· indirecta: frecuente en EII pero no exclusivo ni patognomónico
· secundaria: aparece como consecuencia de los tratamientos o complicaciones de la EII
· ninguna: sin relación documentada con EII, o el registro es basura

Respondes SIEMPRE con un único objeto JSON válido y nada más.";
        }

        private string BuildTriageSystemPrompt()
        {
            return @"Eres un clasificador de catálogos médicos. Depuras un catálogo de tratamientos
que durante años recibió altas libres de pacientes, así que arrastra ruido.

Debes decidir DOS cosas independientes. No las mezcles:

EJE 1 — ¿Es un tratamiento de verdad? (esto es lo que decide el campo 'estado')
EJE 2 — ¿Qué relación tiene con la EII? (esto va en 'nivel_eii' y NO influye en el eje 1)

REGLA CENTRAL: un tratamiento real que NO tiene nada que ver con la EII sigue siendo
VÁLIDO. La falta de relación con EII jamás lo convierte en basura.

RÚBRICA DEL EJE 1 — tres salidas posibles:

VALIDO — es una intervención con intención terapéutica o de manejo de una condición de
salud: sustancia, medicamento, suplemento, cirugía, procedimiento, terapia, técnica,
actividad física, cambio de hábito o de dieta, terapia complementaria.
Ejemplos: 'Proctectomía', 'Aceite de árnica', 'Caminar 2.5-3 millas', 'Stent ureteral',
'Fusión lumbar', 'Prednisona', 'Dieta baja en FODMAP'.
Incluye el AUTOCUIDADO plausible y el manejo cotidiano de la salud: descanso, dormir,
siestas, duchas o baños calientes, manejo del estrés, evitar el alcohol, ajustar la
ingesta de líquidos, actividad física según tolerancia, rutina o higiene intestinal,
evitar el calor. Son manejo real de una condición aunque su relación con la EII sea baja.

BASURA — NO es una intervención terapéutica:
· recordatorios y alarmas de notificación ('Alarmas')
· nombres o códigos de ensayo clínico ('SF-1019', 'Estudio Serono', 'IBS-D IRIS-3')
· productos de consumo, alimentos y cosméticos sin uso terapéutico
  ('Melocotón Vla', 'Gel Limpiador de Kombucha', 'Ropa interior sin costuras')
· texto sin sentido o palabras sueltas sin significado clínico ('ESTRELLA')
· títulos de libro
· actividades genéricas que no son tratamiento ('Visita al doctor')
· condiciones o diagnósticos médicos mal capturados como si fueran tratamiento
· PSEUDOTERAPIAS: prácticas sin base científica ni mecanismo documentado — ACMOS,
  'Códigos curativos', Método Sedona, Técnica Perrin, filtros de armónicos o de campos
  electromagnéticos (STETZERiZER), sanación energética, biodescodificación.
  Señales: lenguaje pseudocientífico vago — armonía, energía, sinergia, frecuencias,
  vibración, 'códigos', bioresonancia — sin evidencia ni mecanismo plausible.
  DESLINDE, no confundir: una terapia complementaria con uso reconocido para confort o
  alivio sintomático NO es pseudoterapia. Baño de Epsom, calor o frío local y ropa de
  compresión siguen siendo 'valido' o 'dudoso'. La línea es esta: pseudociencia = sin
  mecanismo plausible NI uso reconocido. Si dudas de si algo cae aquí, responde 'dudoso'.

DUDOSO — ambiguo; lo revisa un humano:
· servicios o roles profesionales ('Consultor ortopedista', 'Servicios de transporte')
· pruebas diagnósticas ('GeneSight') — diagnostican, no tratan, pero están cerca
· OCIO y EVENTOS DE VIDA sin nexo claro con la salud: viajes ('Viaje a Nueva Zelanda'),
  espectáculos o figuras públicas ('Ver a Joel Osteen'), peregrinaje, hobbies sin
  relación, aprendizaje reportado ('Estudiar idiomas', 'Tejido de punto', 'Dar regalos',
  'Cantar'). El paciente los reportó porque le hicieron bien. SIEMPRE 'dudoso', NUNCA
  'basura', por muy seguro que estés de que no es un tratamiento.
  El aprendizaje cuenta SIEMPRE, sea del tipo que sea: estudiar un idioma, un oficio o
  una certificación profesional ('Estudiar para la Licencia de Técnico en Emergencias
  Médicas'). Que sea formación de carrera y no un pasatiempo NO lo vuelve 'basura':
  si el verbo es estudiar, aprender o practicar algo, la respuesta es 'dudoso'.
  NO los confundas con el AUTOCUIDADO de la lista de VALIDO (descanso, sueño, manejo del
  estrés, evitar alcohol): eso sí es manejo de salud y va a 'valido'. La pregunta que
  los separa: ¿es una forma reconocida de cuidar el cuerpo, o es una actividad de vida
  que de paso sentó bien?
· DISPOSITIVOS y herramientas de monitoreo, protección o apoyo
  ('Monitor de presión arterial', 'Guantes', órtesis, férulas): acompañan el manejo de
  una condición aunque no traten por sí mismos. SIEMPRE 'dudoso', NUNCA 'basura'.
  Distínguelos de los productos de consumo y cosméticos, que sí son basura.
· CATEGORÍAS o agrupadores del catálogo ('Medicamentos con Receta', 'Cirugía',
  'Terapias Alternativas'): no son un tratamiento concreto, pero tampoco son ruido —
  organizan el catálogo y otros registros cuelgan de ellos. Nunca son 'basura'.

REGLA FIRME sobre qué puede ser 'basura': nada de ocio, aprendizaje ni bienestar
reportado por pacientes va a 'basura' jamás. A 'basura' solo van cuatro cosas:
pseudoterapias, no-tratamientos claros (recordatorios y alarmas, productos, alimentos y
cosméticos, códigos y nombres de ensayo clínico), condiciones médicas mal capturadas, y
ruido sin sentido.

LAS INTERVENCIONES TERAPÉUTICAS RECONOCIDAS REALES NUNCA SON 'BASURA'. Esta regla manda
sobre todo lo anterior. Si el registro es (o su nombre principal es) una intervención
reconocida, nunca la clasifiques como 'basura' — aunque su uso sea cosmético, sea de otra
condición, o no tenga ninguna relación con la EII. Cuenta como intervención reconocida:
· un MEDICAMENTO reconocido (nombre genérico o DCI, de prescripción o de venta libre)
· un AGENTE DIAGNÓSTICO (medios de contraste, midriáticos como el ciclopentolato,
  reactivos clínicos como el hipurato de sodio)
· un DISPOSITIVO o producto médico (DIU, órtesis, toxina botulínica…)
· un PROCEDIMIENTO quirúrgico, endoscópico o de rehabilitación — de cualquier
  especialidad, no solo digestiva
· una PSICOTERAPIA o intervención de SALUD MENTAL reconocida: TCC, EMDR, DBT,
  psicoanálisis, Brainspotting ('Puntuación Cerebral'), terapia de trauma o de agresión
  sexual, integración de personalidades, hospitalización psiquiátrica (incluida la
  motivada por sobredosis o ideación suicida), grupos de apoyo estructurados tipo 12
  pasos y de doble diagnóstico (AA, NA, DTR)
· una TERAPIA física, ocupacional o del habla reconocida
→ 'valido' si es claramente una intervención terapéutica.
→ 'dudoso' si su rol es diagnóstico, estético o no-EII, o si es un rol o servicio
  profesional (el 'quién', no el 'qué'), y prefieres que lo confirme un humano.
COHERENCIA: dos registros de la misma familia se clasifican igual. Si 'Hospitalización por
ideación suicida' es 'valido', 'Hospitalización por intento de sobredosis' también lo es;
si EMDR es 'valido', Brainspotting no puede ser 'basura'.

DESLINDE de la regla anterior, para no sobre-corregir: aplica a SUSTANCIAS, FÁRMACOS,
DISPOSITIVOS, PROCEDIMIENTOS y TERAPIAS reconocidos, NO a productos comerciales de
consumo, cosméticos o alimentos que solo CONTIENEN ingredientes, ni a las prácticas sin
mecanismo plausible ni uso reconocido. Esto sigue siendo 'basura':
· pseudoterapias sin mecanismo plausible NI uso reconocido: homeopatía, sanación
  energética, pránica o chamánica, Reiki, biorresonancia, radiestesia, flores de Bach,
  osteopatía craneal, NAET, Hoxsey, GcMAF, quelación con EDTA-DMPS-DMSA
· 'protocolos' y métodos de persona sin base científica ('Protocolo Marshall', 'Protocolo
  CAP de Wheldon', 'Método de baja recuperación de Abraham', 'Receta de…')
· productos comerciales de consumo, cosméticos de marca sin fármaco activo dermatológico
  reconocido (enjuagues bucales comerciales, mascarillas o hidratantes faciales, queratina
  para cabello o uñas, aceites esenciales de marca, lociones, tés, suplementos de marca
  sin DCI)
· alimentos y bebidas
· ruido, nombres sin sentido o no verificables, y códigos o nombres de ensayo clínico que
  NO nombran una intervención concreta ('CALGB 9251')

DESEMPATE REFORZADO: ante la duda entre 'intervención real que no es de EII' y 'basura',
responde SIEMPRE 'dudoso'. Que algo no tenga relación con la EII jamás es motivo
suficiente para mandarlo a 'basura'. A 'basura' solo va la pseudociencia, el producto de
consumo y el ruido — nunca una intervención clínica reconocida.

SESGO OBLIGATORIO A CONSERVAR: ante cualquier duda responde 'dudoso', NUNCA 'basura'.
'basura' se reserva para casos donde estarías dispuesto a defender la decisión.
Marcar de más como basura desactiva tratamientos reales de pacientes: es el error caro.
Marcar de más como dudoso solo cuesta trabajo humano: es el error barato.

CONFIANZA: número entre 0 y 1 que refleja qué tan seguro estás del 'estado'. Si dudas,
baja la confianza — no cambies 'basura' por una confianza alta inventada.

EJE 2 — 'nivel_eii' (independiente, no afecta 'estado'):
· directa: aprobado o indicado específicamente para la EII
· indirecta: usado con frecuencia en EII pero no exclusivo
· secundaria: maneja efectos secundarios o complicaciones del tratamiento de la EII
· ninguna: sin relación documentada con EII, o el registro es basura

Respondes SIEMPRE con un único objeto JSON válido y nada más.";
        }

        /// <summary>
        /// Llamada a Claude que espera JSON de vuelta. Separada de <see cref="CallClaudeApiAsync"/>
        /// porque el triage no debe pasar por el parser de descripciones (RELACIÓN EII / FUENTES /…)
        /// ni escribir el estado <c>_last*</c>.
        /// </summary>
        private async Task<string> CallClaudeJsonAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken)
        {
            if (!_config.Enabled)
                throw new InvalidOperationException("El servicio de IA está deshabilitado");

            if (string.IsNullOrWhiteSpace(_config.AnthropicApiKey))
                throw new InvalidOperationException("La clave API de Anthropic no está configurada");

            var requestBody = new
            {
                model = ModelHaikuClasificacion,
                max_tokens = 400,
                temperature = 0.0,   // clasificar es determinista, no creativo
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userPrompt } }
            };
            var payload = JsonSerializer.Serialize(requestBody);

            // Reintentos cortos: en corridas largas aparecen fallos de red sueltos
            // ("An error occurred while sending the request") y errores 5xx/429 transitorios.
            // Sin esto el registro queda sin sellar y hay que esperar a la siguiente pasada.
            // Un 4xx que no sea 429 es culpa nuestra (payload/credenciales): no se reintenta.
            const int intentosMax = 3;
            var esperaMs = 500;

            for (var intento = 1; ; intento++)
            {
                int status;
                string body;

                // Solo el envío va dentro del try. Si el manejo de status estuviera aquí, el
                // propio throw de un 4xx caería en este catch y se reintentaría igual.
                try
                {
                    using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    using var response = await _httpClient.PostAsync("messages", content, cancellationToken);
                    status = (int)response.StatusCode;
                    body   = await response.Content.ReadAsStringAsync(cancellationToken);
                }
                catch (Exception ex) when (
                    (ex is HttpRequestException or IOException ||
                     (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
                    && intento < intentosMax)
                {
                    // Red caída o timeout del cliente. La cancelación real del usuario NO se reintenta.
                    _logger.LogWarning(ex,
                        "Fallo de red al clasificar, reintento {Intento}/{Max} en {Espera}ms",
                        intento, intentosMax, esperaMs);
                    await System.Threading.Tasks.Task.Delay(esperaMs, cancellationToken);
                    esperaMs *= 2;
                    continue;
                }

                if (status is < 200 or >= 300)
                {
                    var reintentable = status >= 500 || status == 429;
                    if (reintentable && intento < intentosMax)
                    {
                        _logger.LogWarning(
                            "Claude API (triage) devolvió {Status}, reintento {Intento}/{Max} en {Espera}ms",
                            status, intento, intentosMax, esperaMs);
                        await System.Threading.Tasks.Task.Delay(esperaMs, cancellationToken);
                        esperaMs *= 2;
                        continue;
                    }

                    _logger.LogError("Error en Claude API (triage): {Status} - {Error}", status, body);
                    throw new HttpRequestException($"Error en API de Claude: {status}");
                }

                var responseData = JsonSerializer.Deserialize<ClaudeApiResponse>(body);
                if (responseData?.Content == null || responseData.Content.Length == 0)
                    throw new InvalidOperationException("La respuesta de la IA está vacía");

                return responseData.Content[0].Text ?? string.Empty;
            }
        }

        /// <summary>
        /// Parsea el JSON del triage. Cualquier respuesta que no se entienda cae en Dudoso:
        /// un fallo de parseo nunca puede terminar desactivando un tratamiento.
        /// </summary>
        private (byte Estado, double Confianza, string Motivo, MedicalRelationType? Nivel, string? Razonamiento) ParseTriageResponse(
            string fullText,
            string nombre)
        {
            var match = Regex.Match(fullText ?? "", @"\{.*\}", RegexOptions.Singleline);
            if (!match.Success)
            {
                _logger.LogWarning("Triage sin JSON parseable para '{Nombre}'. Respuesta: {Texto}", nombre, fullText);
                return ((byte)eiibd26.Models.TriageLimpieza.Dudoso, 0d,
                        MotivoTriageSinJson, null, null);
            }

            try
            {
                using var doc = JsonDocument.Parse(match.Value);
                var root = doc.RootElement;

                var estadoRaw = root.TryGetProperty("estado", out var e) ? (e.GetString() ?? "") : "";
                var estado = estadoRaw.Trim().ToLowerInvariant() switch
                {
                    "valido" or "válido" => eiibd26.Models.TriageLimpieza.Valido,
                    "basura"             => eiibd26.Models.TriageLimpieza.Basura,
                    _                    => eiibd26.Models.TriageLimpieza.Dudoso
                };

                double confianza = 0d;
                if (root.TryGetProperty("confianza", out var c))
                {
                    if (c.ValueKind == JsonValueKind.Number) confianza = c.GetDouble();
                    else if (c.ValueKind == JsonValueKind.String && double.TryParse(c.GetString(), out var cd)) confianza = cd;
                }
                confianza = Math.Clamp(confianza, 0d, 1d);

                var motivo = root.TryGetProperty("motivo", out var m) ? (m.GetString() ?? "") : "";
                motivo = motivo.Replace("**", "").Trim();
                if (motivo.Length > 400) motivo = motivo[..400];

                MedicalRelationType? nivel = root.TryGetProperty("nivel_eii", out var n)
                    ? (n.GetString() ?? "").Trim().ToLowerInvariant() switch
                    {
                        "directa"    => MedicalRelationType.Directa,
                        "indirecta"  => MedicalRelationType.Indirecta,
                        "secundaria" => MedicalRelationType.Secundaria,
                        _            => null
                    }
                    : null;

                string? razonamiento = root.TryGetProperty("razonamiento", out var r) ? r.GetString() : null;
                if (string.IsNullOrWhiteSpace(razonamiento)) razonamiento = null;
                else if (razonamiento.Length > 500) razonamiento = razonamiento[..500];

                _logger.LogInformation(
                    "Triage '{Nombre}' → {Estado} (confianza {Confianza:0.00}) · nivel {Nivel}",
                    nombre, estado, confianza, nivel?.ToString() ?? "ninguna");

                return ((byte)estado, confianza, motivo, nivel, razonamiento);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Triage con JSON inválido para '{Nombre}'. Respuesta: {Texto}", nombre, fullText);
                return ((byte)eiibd26.Models.TriageLimpieza.Dudoso, 0d,
                        MotivoTriageJsonInvalido, null, null);
            }
        }

        private async Task<(string Descripcion, bool RelacionEII, string? NombreTraducido, bool Reconocido)> CallClaudeApiTratamientoAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken)
        {
            var (descripcion, relacionEII, reconocido) = await CallClaudeApiAsync(systemPrompt, userPrompt, cancellationToken);
            return (descripcion, relacionEII, _lastNombreTraducido, reconocido);
        }

        private async Task<(string Descripcion, bool RelacionEII, bool Reconocido)> CallClaudeApiAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken)
        {
            if (!_config.Enabled)
                throw new InvalidOperationException("El servicio de IA está deshabilitado");

            if (string.IsNullOrWhiteSpace(_config.AnthropicApiKey))
                throw new InvalidOperationException("La clave API de Anthropic no está configurada");

            try
            {
                // Modelo dedicado a la ficha publicada. `_config.Model` lo comparten el Q&A de
                // NINA, Platillos y el evaluador de calidad: subirlo allí encarecería todo. Aquí
                // se paga más porque es el único que produce contenido médico publicado y es
                // donde alucinó (caso Aangamik).
                var modeloDescripcion = string.IsNullOrWhiteSpace(_config.ModelDescripcionFicha)
                    ? _config.Model
                    : _config.ModelDescripcionFicha;

                var requestBody = new
                {
                    model = modeloDescripcion,
                    max_tokens = _config.MaxTokens,
                    temperature = _config.Temperature,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Generando descripción IA. Model: {Model}", modeloDescripcion);

                var response = await _httpClient.PostAsync("messages", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Error en Claude API: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Error en API de Claude: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent);

                if (responseData?.Content == null || responseData.Content.Length == 0)
                    throw new InvalidOperationException("La respuesta de la IA está vacía");

                var fullText = responseData.Content[0].Text ?? string.Empty;

                // Extraer descripción, relación EII y explicación
                var (descripcion, relacionEII, reconocido) = ParseResponse(fullText);

                if (!reconocido)
                {
                    // El modelo declaró que no reconoce el término. Se devuelve el estado, NO el
                    // texto: el caller tiene prohibido persistirlo. Si se guardara, el paciente
                    // vería "RECONOCIDO: NO" en la ficha — peor que la alucinación original.
                    _logger.LogWarning("El generador declaró NO RECONOCIDO — no se devuelve descripción.");
                    return (string.Empty, false, false);
                }

                _logger.LogInformation("Descripción generada exitosamente. RelacionEII: {RelacionEII}, Explicación: {Explicacion}",
                    relacionEII, _lastExplicacionEII);

                return (descripcion, relacionEII, true);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout al generar descripción IA");
                throw new TimeoutException("La generación de la descripción excedió el tiempo límite", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al generar descripción IA");
                throw;
            }
        }

        // Propiedades públicas para que el controller pueda acceder
        public string UltimaExplicacionEII => _lastExplicacionEII;
        public string UltimasFuentes => _lastFuentes;
        public string? UltimoNombreTraducido => _lastNombreTraducido;

        private (string Descripcion, bool RelacionEII, bool Reconocido) ParseResponse(string fullText)
        {
            // Guardrail (2ª red, después del gate): el modelo declara si reconoce el término.
            // Ausencia del marcador ⇒ true. Es deliberado: el gate ya validó la entidad antes de
            // llegar aquí, y tratar un formato descuidado como "no reconocido" tiraría fichas
            // legítimas en masa. El marcador sirve para cazar al modelo admitiendo que no sabe.
            var matchReconocido = Regex.Match(fullText, @"\*?\*?RECONOCIDO:\*?\*?\s*(SÍ|SI|NO)", RegexOptions.IgnoreCase);
            var reconocido = true;
            if (matchReconocido.Success)
            {
                reconocido = matchReconocido.Groups[1].Value.ToUpperInvariant() is "SÍ" or "SI";
                if (!reconocido)
                    _logger.LogWarning("El generador respondió RECONOCIDO: NO — se descarta la salida.");
            }
            else
            {
                _logger.LogWarning("El generador no emitió la línea RECONOCIDO — se asume reconocido (el gate ya validó el término).");
            }

            // Buscar línea "NOMBRE ESPAÑOL: [texto]" (para tratamientos)
            var matchNombre = Regex.Match(fullText, @"\*?\*?NOMBRE ESPAÑOL:\*?\*?\s*(.+?)(?=\r?\n|$)", RegexOptions.IgnoreCase);

            // Buscar línea "RELACIÓN EII: SÍ/NO" (con o sin markdown)
            var matchRelacion = Regex.Match(fullText, @"\*?\*?RELACIÓN EII:\*?\*?\s*(SÍ|SI|NO)", RegexOptions.IgnoreCase);

            // Buscar línea "EXPLICACIÓN EII: [texto]" (con o sin markdown)
            var matchExplicacion = Regex.Match(fullText, @"\*?\*?EXPLICACIÓN EII:\*?\*?\s*(.+?)(?=\r?\n\r?\n|\r?\n\*?\*?FUENTES:|\r?\n\*?\*?RELACIÓN EII:|\r?\n$|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // ⭐ NUEVO: Buscar línea "FUENTES: [texto]" (con o sin markdown)
            var matchFuentes = Regex.Match(fullText, @"\*?\*?FUENTES:\*?\*?\s*(.+?)(?=\r?\n\r?\n|\r?\n$|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            bool relacionEII = false;
            string explicacionEII = "";
            string fuentes = "";
            string? nombreTraducido = null;
            string descripcion = fullText;

            // Log para debugging
            _logger.LogDebug("Texto completo recibido de Claude: {FullText}", fullText);

            // ⭐ PROCESAR NOMBRE TRADUCIDO (para tratamientos)
            if (matchNombre.Success)
            {
                var nombreRaw = matchNombre.Groups[1].Value.Trim();
                _logger.LogDebug("Nombre traducido encontrado: {Nombre}", nombreRaw);

                if (!string.IsNullOrWhiteSpace(nombreRaw))
                {
                    nombreTraducido = nombreRaw
                        .Replace("**", "")
                        .Replace("*", "")
                        .Trim();
                }
            }

            // Procesar RELACIÓN EII
            if (matchRelacion.Success)
            {
                relacionEII = matchRelacion.Groups[1].Value.ToUpperInvariant() is "SÍ" or "SI";

                _logger.LogDebug("Relación EII encontrada: {RelacionEII}", relacionEII ? "SÍ" : "NO");

                // Encontrar el inicio de la sección de metadatos (puede ser NOMBRE ESPAÑOL o RELACIÓN EII)
                int metadataStart = matchNombre.Success && matchNombre.Index < matchRelacion.Index
                    ? matchNombre.Index
                    : matchRelacion.Index;

                // RECONOCIDO va PRIMERO en el bloque de metadatos: si no se incluye aquí, la
                // línea se quedaría dentro de la descripción y el paciente la vería en la ficha.
                if (matchReconocido.Success && matchReconocido.Index < metadataStart)
                    metadataStart = matchReconocido.Index;

                // Si hay explicación, usarla; si no, usar texto por defecto
                if (matchExplicacion.Success)
                {
                    var explicacionRaw = matchExplicacion.Groups[1].Value.Trim();
                    _logger.LogDebug("Explicación raw encontrada: {Explicacion}", explicacionRaw);

                    if (!string.IsNullOrWhiteSpace(explicacionRaw) && 
                        explicacionRaw.ToLower() != "no aplica")
                    {
                        // Limpiar markdown de la explicación
                        explicacionEII = explicacionRaw
                            .Replace("**", "")
                            .Replace("*", "")
                            .Trim();
                    }
                    else if (relacionEII)
                    {
                        explicacionEII = "Síntoma/tratamiento documentado en pacientes con EII";
                    }
                    else
                    {
                        explicacionEII = "No se encontró relación documentada con EII";
                    }
                }
                else if (relacionEII)
                {
                    explicacionEII = "Síntoma/tratamiento documentado en pacientes con EII";
                    _logger.LogWarning("No se encontró explicación en el texto, usando valor por defecto");
                }
                else
                {
                    explicacionEII = "No se encontró relación documentada con EII";
                }

                // ⭐ NUEVO: Procesar FUENTES
                if (matchFuentes.Success)
                {
                    var fuentesRaw = matchFuentes.Groups[1].Value.Trim();
                    _logger.LogDebug("Fuentes raw encontradas: {Fuentes}", fuentesRaw);

                    // Solo guardar si son fuentes reales, no valores por defecto
                    if (!string.IsNullOrWhiteSpace(fuentesRaw) && 
                        fuentesRaw.ToLower() != "no aplica" &&
                        fuentesRaw.ToLower() != "no especificadas" &&
                        fuentesRaw.ToLower() != "no disponible" &&
                        !fuentesRaw.Contains("No hay fuentes", StringComparison.OrdinalIgnoreCase))
                    {
                        // Limpiar markdown de las fuentes
                        fuentes = fuentesRaw
                            .Replace("**", "")
                            .Replace("*", "")
                            .Trim();
                    }
                }

                // ⭐ NO AGREGAR VALORES POR DEFECTO - Si no hay fuentes verificadas, dejar vacío
                if (string.IsNullOrWhiteSpace(fuentes))
                {
                    _logger.LogInformation("No se proporcionaron fuentes específicas verificadas");
                }

                // Remover toda la sección de metadatos de la descripción
                descripcion = fullText.Substring(0, metadataStart).Trim();
            }
            else
            {
                // Si no se encontró el patrón, usar valor por defecto
                explicacionEII = "No se pudo determinar la relación con EII";
                fuentes = ""; // ⭐ NO AGREGAR FUENTES FALSAS
                _logger.LogWarning("No se encontró el patrón 'RELACIÓN EII:' en la respuesta");
            }

            // Red final del marcador: si faltó 'RELACIÓN EII' no se recorta el bloque de
            // metadatos y la descripción se queda con el texto completo. Sin esto, un término
            // legítimo podría publicar una ficha que termina en "RECONOCIDO: SÍ".
            descripcion = Regex.Replace(
                descripcion,
                @"(?im)^[ \t]*\*?\*?RECONOCIDO:\*?\*?[ \t]*(SÍ|SI|NO)[ \t]*\r?$",
                string.Empty).Trim();

            _logger.LogInformation("Resultado del parsing - RelacionEII: {RelacionEII}, Explicación: {Explicacion}, Fuentes: {Fuentes}, NombreTraducido: {Nombre}", 
                relacionEII, explicacionEII, fuentes, nombreTraducido ?? "No proporcionado");

            // ⭐ Extraer NIVEL RELACIÓN EII
            var matchNivel = Regex.Match(fullText, @"\*?\*?NIVEL RELACIÓN EII:\*?\*?\s*(Directa|Indirecta|Secundaria)", RegexOptions.IgnoreCase);
            MedicalRelationType? nivelRelacion = null;
            if (matchNivel.Success && relacionEII)
            {
                nivelRelacion = matchNivel.Groups[1].Value.Trim().ToLowerInvariant() switch
                {
                    "directa"   => MedicalRelationType.Directa,
                    "indirecta" => MedicalRelationType.Indirecta,
                    "secundaria" => MedicalRelationType.Secundaria,
                    _ => null
                };
                _logger.LogInformation("Nivel relación EII encontrado: {Nivel}", nivelRelacion);
            }
            // Si RelacionEII=true pero nivel no encontrado → default Indirecta
            else if (relacionEII)
            {
                nivelRelacion = MedicalRelationType.Indirecta;
                _logger.LogWarning("No se encontró NIVEL RELACIÓN EII, usando Indirecta por defecto");
            }

            // ⭐ Extraer RAZONAMIENTO GLOSARIO
            var matchRazonamiento = Regex.Match(fullText, @"\*?\*?RAZONAMIENTO GLOSARIO:\*?\*?\s*(.+?)(?=\r?\n|$)", RegexOptions.IgnoreCase);
            string? razonamiento = null;
            if (matchRazonamiento.Success)
            {
                var raw = matchRazonamiento.Groups[1].Value.Trim()
                    .Replace("**", "").Replace("*", "").Trim();
                if (!string.IsNullOrWhiteSpace(raw) &&
                    !raw.Equals("No aplica", StringComparison.OrdinalIgnoreCase))
                {
                    razonamiento = raw.Length > 500 ? raw[..500] : raw;
                    _logger.LogInformation("Razonamiento glosario encontrado: {Razonamiento}", razonamiento);
                }
            }

            // Guardar la explicación, fuentes, nombre traducido, nivel y razonamiento
            _lastExplicacionEII = explicacionEII;
            _lastFuentes        = fuentes;
            _lastNombreTraducido = nombreTraducido;
            _lastNivelRelacion  = nivelRelacion;
            _lastRazonamiento   = razonamiento;

            return (descripcion, relacionEII, reconocido);
        }

        // Variables temporales para pasar datos al controller
        private string _lastExplicacionEII = "";
        private string _lastFuentes = "";
        private string? _lastNombreTraducido = null;
        private MedicalRelationType? _lastNivelRelacion = null;
        private string? _lastRazonamiento = null;

        public MedicalRelationType? UltimoNivelRelacion => _lastNivelRelacion;
        public string? UltimoRazonamiento => _lastRazonamiento;

        private string BuildSintomaSystemPrompt()
        {
            return @"REGLA CERO, POR ENCIMA DE TODAS LAS DEMÁS — NO INVENTES:
Si NO reconoces el término como un síntoma real y específico, NO escribas una descripción.
No es aceptable producir un texto plausible sobre algo que no conoces: esto se publica a
pacientes. En particular, tienes PROHIBIDO afirmar cómo se siente, dónde se localiza, con
qué frecuencia aparece o con qué enfermedad se asocia (colitis ulcerosa, Crohn, EII…) si
no puedes verificarlo. Un nombre que parezca una marca comercial, un código, un producto,
un tratamiento, un alimento o texto sin sentido NO es un síntoma.
En ese caso responde 'RECONOCIDO: NO' y nada más. Decir 'no lo reconozco' es la respuesta
correcta y esperada; inventar es el único error grave.
No estás obligado a que el término tenga relación con la EII: un síntoma real de otra
condición se describe con normalidad. Lo que no se hace es describir lo que no existe.

Actúa como redactor de contenido médico orientado a pacientes no como médico ni como enciclopedia clínica.

Tu objetivo es describir síntomas en lenguaje sencillo para ayudar a las personas a reconocer y expresar lo que sienten, SIN diagnosticar ni explicar enfermedades en profundidad.

Reglas obligatorias:
• Usa lenguaje claro y cotidiano (nivel lectura 6–8 grado).
• NO expliques mecanismos biológicos complejos.
• NO menciones tratamientos.
• NO sugieras diagnósticos.
• NO listes causas específicas graves.
• Describe solo la EXPERIENCIA del síntoma.
• Mantén tono neutral y tranquilizador.
• Máximo 120 palabras totales.

Estructura EXACTA:

Nombre del síntoma (traducción simple)

¿Qué es?
Explicación breve en 1–2 frases centradas en cómo se siente.

¿Cómo puede sentirse?
• 4 ejemplos cotidianos reales del paciente.

Importante
Aclara que es un síntoma y no un diagnóstico y que requiere evaluación médica si persiste.";
        }

        private string BuildTratamientoSystemPrompt()
        {
            return @"REGLA CERO, POR ENCIMA DE TODAS LAS DEMÁS — NO INVENTES:
Si NO reconoces el término como un medicamento o tratamiento real y específico, NO escribas
una descripción. No es aceptable producir un texto plausible sobre algo que no conoces:
esto se publica a pacientes. En particular, tienes PROHIBIDO afirmar:
· la VÍA o la forma (inyectable, intravenoso, oral, tópico, supositorio…)
· si es de PRESCRIPCIÓN o de venta libre
· el MECANISMO o la familia farmacológica
· la INDICACIÓN (colitis ulcerosa, Crohn, EII, cualquier enfermedad)
…si no puedes verificarlo para ESE término concreto.
Ojo con las MARCAS COMERCIALES: una marca que no reconoces con certeza NO se describe
como fármaco. Muchas son suplementos de venta libre, no medicamentos.
En ese caso responde 'RECONOCIDO: NO' y nada más. Decir 'no lo reconozco' es la respuesta
correcta y esperada; inventar es el único error grave.
No estás obligado a que el término tenga relación con la EII: un tratamiento real de otra
condición se describe con normalidad. Lo que no se hace es describir lo que no existe.

Actúa como redactor de contenido de salud orientado a pacientes, no como médico ni como enciclopedia clínica.

Tu objetivo es ayudar a una persona a ENTENDER qué representa un tratamiento dentro de su proceso de salud, usando lenguaje sencillo y educativo.

NO expliques farmacología ni mecanismos biológicos.

Reglas obligatorias:
• Usa lenguaje claro y cotidiano (nivel lectura 6–8 grado).
• Explica el tratamiento desde la experiencia del paciente.
• Describe qué es y qué suele implicar usarlo.
• NO recomiendes usarlo ni evitarlo.
• NO prometas resultados.
• NO compares tratamientos.
• NO menciones porcentajes de eficacia.
• NO uses lenguaje técnico complejo.
• Mantén tono neutral, informativo y tranquilizador.
• Máximo 120 palabras totales.

Estructura EXACTA:

Nombre del tratamiento (traducción simple si aplica)

¿Qué es?
Explicación breve del tipo de tratamiento en términos generales.

¿Cómo suele formar parte del tratamiento?
1–2 frases sobre cómo las personas suelen integrarlo en su rutina o seguimiento médico.

¿Qué puede notar una persona?
• 4 experiencias comunes relacionadas con su uso (rutina, controles, sensaciones generales).

Importante
Aclara que debe seguir recomendaciones médicas y que todo tratamiento requiere supervisión profesional.";
        }

        #region DTOs for Claude API

        private class ClaudeApiResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public ContentItem[]? Content { get; set; }
        }

        private class ContentItem
        {
            [System.Text.Json.Serialization.JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        #endregion
    }
}
