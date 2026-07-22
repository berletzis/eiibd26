using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Generación de contenido del módulo Platillos. Hermano de
    /// <see cref="SintomasTratamientosAiService"/> — reusa el mismo cliente "AnthropicClient" y el
    /// mismo estilo de prompt/parseo, pero separa DOS niveles de riesgo en prompts distintos.
    /// </summary>
    public class PlatillosAiService : IPlatillosAiService
    {
        private readonly HttpClient _httpClient;
        private readonly AiAnswerConfiguration _config;
        private readonly ILogger<PlatillosAiService> _logger;

        public PlatillosAiService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiAnswerConfiguration> config,
            ILogger<PlatillosAiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("AnthropicClient");
            _config = config.Value;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  CONTENIDO CLÍNICO — notas de grupo / ingrediente (candados estrictos)
        // ─────────────────────────────────────────────────────────────────────────────

        public async Task<NotaClinicaGeneradaDto> GenerarNotaClinicaGrupoAsync(
            string nombreGrupo, IEnumerable<string> ingredientesDelGrupo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreGrupo))
                throw new ArgumentException("El nombre del grupo no puede estar vacío", nameof(nombreGrupo));

            var lista = string.Join(", ", (ingredientesDelGrupo ?? Enumerable.Empty<string>())
                .Where(i => !string.IsNullOrWhiteSpace(i)));
            if (string.IsNullOrWhiteSpace(lista)) lista = "(sin ingredientes registrados aún)";

            var userPrompt = $@"Genera el contenido para el GRUPO de alimentos: {nombreGrupo}
Ingredientes de este grupo: {lista}

Al final, en líneas separadas, texto plano:
TITULO: [ej. ¿Puedo comer {nombreGrupo}?]
QUE_ES: [1-2 frases]
QUE_SUELE_PASAR: [observacional; brote vs remisión si aplica]
IMPORTANTE: [el matiz honesto: el costo de excluir el grupo entero]
FUENTES: [solo de la lista blanca, separadas por ';', o 'Ninguna']
REVISION_PRIORITARIA: [SÍ si el tema es sensible y no hubo fuente aplicable; si no, NO]";

            var fullText = await CallClaudeApiAsync(BuildClinicoSystemPrompt(), userPrompt, cancellationToken);
            return ParseNotaClinica(fullText);
        }

        public async Task<NotaClinicaGeneradaDto> GenerarNotaClinicaIngredienteAsync(
            string nombreIngrediente, string? nombreGrupo, IEnumerable<string> atributos,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreIngrediente))
                throw new ArgumentException("El nombre del ingrediente no puede estar vacío", nameof(nombreIngrediente));

            var listaAtributos = string.Join(", ", (atributos ?? Enumerable.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a)));
            if (string.IsNullOrWhiteSpace(listaAtributos)) listaAtributos = "(ninguno)";
            var grupo = string.IsNullOrWhiteSpace(nombreGrupo) ? "(sin grupo)" : nombreGrupo!;

            var userPrompt = $@"Genera el contenido para el INGREDIENTE: {nombreIngrediente}
Grupo al que pertenece: {grupo}
Atributos intrínsecos: {listaAtributos}   (ej. gluten, cítrico, picante)

Escribe SOLO lo específico de este ingrediente que NO esté ya cubierto por la nota de su
grupo (para no repetir). Si no hay nada específico que agregar, dilo en QUE_SUELE_PASAR.

Al final, en líneas separadas, texto plano:
TITULO: [ej. ¿Puedo comer {nombreIngrediente}?]
QUE_ES: [1-2 frases]
QUE_SUELE_PASAR: [observacional; brote vs remisión si aplica]
IMPORTANTE: [el matiz honesto]
FUENTES: [solo de la lista blanca, separadas por ';', o 'Ninguna']
REVISION_PRIORITARIA: [SÍ si el tema es sensible y no hubo fuente aplicable; si no, NO]";

            var fullText = await CallClaudeApiAsync(BuildClinicoSystemPrompt(), userPrompt, cancellationToken);
            return ParseNotaClinica(fullText);
        }

        public async Task<NotaClinicaGeneradaDto> GenerarNotaPrecaucionGrupoAsync(
            string nombreGrupo, string? riesgoTipo, IEnumerable<string> ingredientesDelGrupo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreGrupo))
                throw new ArgumentException("El nombre del grupo no puede estar vacío", nameof(nombreGrupo));

            var lista = string.Join(", ", (ingredientesDelGrupo ?? Enumerable.Empty<string>())
                .Where(i => !string.IsNullOrWhiteSpace(i)));
            if (string.IsNullOrWhiteSpace(lista)) lista = "(sin ingredientes registrados aún)";
            var motivo = string.IsNullOrWhiteSpace(riesgoTipo) ? "(no especificado)" : riesgoTipo!.Trim();

            var userPrompt = $@"Genera un AVISO DE PRECAUCIÓN de seguridad alimentaria para el GRUPO de riesgo: {nombreGrupo}
Motivo del riesgo: {motivo}
Ingredientes de este grupo: {lista}

Es un aviso de SEGURIDAD por infección (no de digestión), para pacientes con EII cuyo tratamiento
PUEDE bajar las defensas. Aplica a todos los ingredientes del grupo. Respeta los dos candados.

Al final, en líneas separadas, texto plano:
TITULO: [ej. Precaución si tu tratamiento baja tus defensas]
QUE_ES: [1 frase: qué riesgo de seguridad hay con este grupo, condicional a defensas bajas]
QUE_SUELE_PASAR: [el riesgo atado a la PREPARACIÓN: crudo/poco cocido vs bien cocido]
IMPORTANTE: [condicional a inmunosupresión — no todos los tratamientos bajan defensas, la mesalazina NO — y deferir al médico]
FUENTES: [solo de la lista blanca, separadas por ';', o 'Ninguna']
REVISION_PRIORITARIA: [SÍ si el tema es sensible y no hubo fuente aplicable; si no, NO]";

            var fullText = await CallClaudeApiAsync(BuildPrecaucionSystemPrompt(), userPrompt, cancellationToken);
            return ParseNotaClinica(fullText);
        }

        public async Task<string> GenerarNotasEiiAsync(
            string tipo, string nombre, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede estar vacío", nameof(nombre));

            var tipoTxt = string.IsNullOrWhiteSpace(tipo) ? "alimento" : tipo.Trim();
            var userPrompt = $@"En UNA sola frase de máximo 120 caracteres, observacional y sin prescribir, resume qué
conviene tener en cuenta de {tipoTxt} ""{nombre}"" en EII. Texto plano. Sin fuentes.
Solo la frase, nada más.";

            var fullText = await CallClaudeApiAsync(BuildClinicoSystemPrompt(), userPrompt, cancellationToken);
            var frase = LimpiarTextoPlano(fullText);
            // Una sola frase (primera línea). El prompt pide ≤120, pero NO cortamos por carácter:
            // partir a los 120 exactos mutila la última palabra ("…en el estó"). Recortamos en la
            // frontera de palabra y con margen holgado (el campo NotasEII admite hasta 1000), así una
            // frase completa sobrevive intacta.
            frase = frase.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
            return RecortarEnPalabra(frase, 220);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  CONTENIDO TAXONÓMICO — atributo / categoría (prompt ligero, sin fuentes)
        // ─────────────────────────────────────────────────────────────────────────────

        public async Task<string> GenerarDescripcionAtributoAsync(
            string nombreAtributo, string ambito, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreAtributo))
                throw new ArgumentException("El nombre del atributo no puede estar vacío", nameof(nombreAtributo));

            var ambitoTxt = string.IsNullOrWhiteSpace(ambito) ? "Ingrediente" : ambito.Trim();
            var userPrompt = $@"Define la etiqueta: {nombreAtributo}
Ámbito: {ambitoTxt}   (Ingrediente = propiedad del alimento; Uso = cómo se prepara)
Si el ámbito es 'Uso', descríbelo como una FORMA DE PREPARACIÓN (ej. ""asado: cocido al
calor seco, con poca o nada de grasa añadida""), no como propiedad del alimento.
Solo la definición.";

            var fullText = await CallClaudeApiAsync(BuildTaxonomicoSystemPrompt(), userPrompt, cancellationToken);
            return RecortarDescripcion(LimpiarTextoPlano(fullText));
        }

        public async Task<string> GenerarDescripcionCategoriaAsync(
            string nombreCategoria, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nombreCategoria))
                throw new ArgumentException("El nombre de la categoría no puede estar vacío", nameof(nombreCategoria));

            var userPrompt = $@"Define la categoría de platillo: {nombreCategoria}
Es una categoría de platos en un menú (ej. Entrada, Plato fuerte, Ensalada). Describe en 1-2
frases qué agrupa esta categoría dentro de una comida. NO hagas afirmaciones clínicas ni de
tolerancia; es solo una definición.
Solo la definición.";

            var fullText = await CallClaudeApiAsync(BuildTaxonomicoSystemPrompt(), userPrompt, cancellationToken);
            return RecortarDescripcion(LimpiarTextoPlano(fullText));
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  System prompts
        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>Lista blanca formateada para inyectar en los prompts clínicos (uno por línea).</summary>
        private string ListaBlancaTexto()
        {
            var fuentes = _config.FuentesClinicasPermitidas?
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => "- " + f.Trim())
                .ToList() ?? new List<string>();

            return fuentes.Count > 0
                ? string.Join("\n", fuentes)
                : "(No hay fuentes aprobadas configuradas. NO cites ninguna fuente.)";
        }

        private string BuildClinicoSystemPrompt()
        {
            var listaBlanca = ListaBlancaTexto();

            return $@"Actúa como redactor de contenido de salud para pacientes con Enfermedad Inflamatoria
Intestinal (EII: Crohn y Colitis Ulcerosa). NO como médico ni como enciclopedia clínica.

Objetivo: ayudar a un paciente a entender qué suele pasar con un alimento en EII, en
lenguaje sencillo, SIN recomendar ni prohibir.

Reglas obligatorias:
- Lenguaje claro y cotidiano (nivel lectura 6-8 grado).
- NUNCA prescribas. Prohibido ""deberías evitar"", ""es bueno/malo para ti"", ""esta dieta
  funciona"". Un alimento no es ""bueno"" ni ""malo"".
- Lenguaje observacional: ""suele tolerarse"", ""algunas personas reportan"", ""en brote puede
  sentirse más pesado"". NUNCA ""causa"", ""provoca"", ""debido a"".
- Distingue brote vs remisión cuando aplique.
- Recuerda que esto NO es una dieta: la plataforma no conoce la enfermedad, el tratamiento
  ni el momento del paciente. Su dieta la diseña con su médico o nutriólogo.
- NO expliques mecanismos biológicos. NO menciones tratamientos ni fármacos. Sin porcentajes.
- Cuerpo máximo ~140 palabras. Responde en TEXTO PLANO, sin markdown (sin **, *, #).

DOS EJES DE PREOCUPACIÓN — considéralos ambos, no solo la digestión:
1. TOLERANCIA digestiva: si suele sentar pesado, la fibra, la grasa, brote vs remisión.
2. SEGURIDAD por infección: muchas personas con EII están en tratamiento que baja las
   defensas (inmunosupresores, esteroides, biológicos). Para ellas, algunos alimentos
   —mariscos y pescado crudos o poco cocidos, huevo crudo, lácteos sin pasteurizar,
   embutidos, germinados— pueden traer bacterias (Listeria, Salmonella) que ahí importan
   más que la digestión.
Si el alimento tiene un riesgo de SEGURIDAD relevante, MENCIÓNALO — observacional, ligado a
la PREPARACIÓN (crudo/poco cocido vs bien cocido) y a las defensas bajas. Ejemplo del tono:
""bien cocido suele estar bien; crudo o poco cocido puede traer bacterias, y si estás con
medicamentos que bajan tus defensas conviene comentarlo con tu médico"". NUNCA como orden;
siempre deferir al médico. Si NO hay riesgo de seguridad, no lo inventes.

DA ALGO ÚTIL — no cierres siempre en ""depende de tu tolerancia"".
Es cierto, pero insuficiente: el paciente ya lo sabe. Cada nota DEBE dejar al menos UN dato
concreto y accionable, sin dejar de ser observacional. Informa un PATRÓN que el paciente decide,
NO un veredicto:
- Nombra el FACTOR que más cambia, no ""la tolerancia"" en abstracto:
  ✗ ""depende de tu tolerancia"" → ✓ ""lo que más cambia aquí es la grasa: frito pesa más que asado""
- Da el AJUSTE de la mayoría como PATRÓN, no como orden:
  ✗ ""debes comerlo pelado"" → ✓ ""muchos lo toleran mejor pelado, bien cocido y en porción chica""
- Brote vs remisión con concreto: ✓ ""en brote muchos lo dejan bien cocido; en remisión suele volver sin problema""
- Cuando aplique, di CÓMO PROBARLO (empodera y es 100% seguro):
  ✓ ""si quieres probarlo, empieza con poco y bien cocido, y fíjate cómo te sientes en las horas siguientes""
Carga el FACTOR + el PATRÓN en QUE_SUELE_PASAR, y el cómo-probarlo / qué observar en IMPORTANTE.
Sigue prohibido prescribir, prometer resultados o decir ""bueno/malo para ti"" — pero eso NO te exime de ser útil.

FUENTES — regla crítica:
Solo puedes citar fuentes de esta lista blanca:
{listaBlanca}
PROHIBIDO citar cualquier fuente fuera de esa lista. Si ninguna aplica, NO cites nada.
NUNCA inventes una fuente ni cites de memoria.

COHERENCIA CITA-CUERPO (regla dura): si citas una fuente, el CUERPO de la nota DEBE hablar de
ese punto. Prohibido citar algo que el texto no menciona (ej. citar 'Listeria en
inmunocomprometidos' sin decir nada de infección en el cuerpo). La referencia respalda lo que
escribiste, no un punto que omitiste.";
        }

        private static string BuildTaxonomicoSystemPrompt()
        {
            return @"Explica en 1-2 frases, lenguaje simple, qué significa una etiqueta de alimento. NO es
contenido clínico: no afirmas si algo se tolera o no, no citas fuentes. Solo defines el
término. Texto plano.";
        }

        private string BuildPrecaucionSystemPrompt()
        {
            var listaBlanca = ListaBlancaTexto();

            return $@"Actúa como redactor de contenido de salud para pacientes con Enfermedad Inflamatoria
Intestinal (EII). Escribe un AVISO DE SEGURIDAD ALIMENTARIA (riesgo de infección), NO una nota de
tolerancia digestiva y NO como médico.

Objetivo: advertir, sin alarmar y sin prohibir, sobre el riesgo de infección de ciertos alimentos
cuando el tratamiento del paciente baja las defensas.

DOS CANDADOS DE PRECISIÓN — obligatorios, no los rompas:
1. SIEMPRE CONDICIONAL a inmunosupresión. NUNCA asumas que el paciente está inmunosuprimido. No
   todos los tratamientos bajan las defensas: inmunosupresores, biológicos y esteroides sí; la
   MESALAZINA NO. Redáctalo así: ""si tu tratamiento baja tus defensas (no todos lo hacen)…"".
2. ATADO A LA PREPARACIÓN, no al alimento. El riesgo es crudo o poco cocido; bien cocido lo reduce
   mucho. NUNCA ""evita el marisco""; sí ""el marisco crudo o poco cocido…, bien cocido es más seguro"".

Reglas obligatorias:
- Lenguaje claro y cotidiano (nivel lectura 6-8 grado). Observacional, NUNCA una orden.
- Defiere SIEMPRE al médico (""coméntalo con tu médico"").
- Sin porcentajes. Cuerpo máximo ~120 palabras. TEXTO PLANO, sin markdown (sin **, *, #).

FUENTES — regla crítica:
Solo puedes citar fuentes de esta lista blanca:
{listaBlanca}
PROHIBIDO citar cualquier fuente fuera de esa lista. Si ninguna aplica, NO cites nada.
NUNCA inventes una fuente ni cites de memoria.";
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  Llamada a la API (misma forma que el servicio de síntomas)
        // ─────────────────────────────────────────────────────────────────────────────

        private async Task<string> CallClaudeApiAsync(
            string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            if (!_config.Enabled)
                throw new InvalidOperationException("El servicio de IA está deshabilitado");
            if (string.IsNullOrWhiteSpace(_config.AnthropicApiKey))
                throw new InvalidOperationException("La clave API de Anthropic no está configurada");

            try
            {
                var requestBody = new
                {
                    model = _config.Model,
                    max_tokens = _config.MaxTokens,
                    temperature = _config.Temperature,
                    system = systemPrompt,
                    messages = new[] { new { role = "user", content = userPrompt } }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Generando contenido Platillos IA. Model: {Model}", _config.Model);

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

                return responseData.Content[0].Text ?? string.Empty;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout al generar contenido Platillos IA");
                throw new TimeoutException("La generación del contenido excedió el tiempo límite", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al generar contenido Platillos IA");
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        //  Parseo (reusa el patrón tolerante a markdown del servicio de síntomas)
        // ─────────────────────────────────────────────────────────────────────────────

        private NotaClinicaGeneradaDto ParseNotaClinica(string fullText)
        {
            var dto = new NotaClinicaGeneradaDto
            {
                Titulo = ExtraerCampo(fullText, "TITULO"),
                QueEs = ExtraerCampo(fullText, "QUE_ES"),
                QueSuelePasar = ExtraerCampo(fullText, "QUE_SUELE_PASAR"),
                Importante = ExtraerCampo(fullText, "IMPORTANTE")
            };

            // FUENTES: separadas por ';'. CANDADO BACKEND: solo sobreviven las que están en la
            // lista blanca. Todo lo demás se descarta (aunque el modelo lo haya citado igual).
            var fuentesRaw = ExtraerCampo(fullText, "FUENTES");
            var (fuentesValidas, huboFueraDeLista) = FiltrarPorListaBlanca(fuentesRaw);
            dto.Fuentes = fuentesValidas;

            // REVISION_PRIORITARIA: la del modelo (tema sensible sin fuente aplicable), MÁS nuestras
            // propias señales duras:
            //   · huboFueraDeLista → citó algo fuera de la lista blanca (posible fuente inventada que
            //     descartamos) → revisar aunque el modelo no lo pidiera.
            //   · citaHuerfana → una REFERENCIA habla de seguridad/infección pero el CUERPO no toca
            //     ese eje (el caso del camarón: citó Listeria sin decir nada de infección). Chequeo
            //     DIRIGIDO POR LA CITA, no por "toda nota con referencias" (eso sobre-marcaría todo).
            // No forzamos la marca por "cero fuentes" a secas: que la sensibilidad la juzgue el modelo.
            var revisionModelo = EsAfirmativo(ExtraerCampo(fullText, "REVISION_PRIORITARIA"));
            var cuerpo = string.Join(" ", dto.QueEs, dto.QueSuelePasar, dto.Importante);
            var citaHuerfana = CitaHablaDeSeguridad(dto.Fuentes) && !CuerpoHablaDeSeguridad(cuerpo);
            dto.RevisionPrioritaria = revisionModelo || huboFueraDeLista || citaHuerfana;
            if (citaHuerfana)
                _logger.LogWarning("Cita huérfana de seguridad: la referencia menciona infección/seguridad pero el cuerpo no.");

            _logger.LogInformation(
                "Nota clínica IA parseada. Fuentes válidas: {N}, fueraDeLista: {Fuera}, revisión: {Rev}",
                dto.Fuentes.Count, huboFueraDeLista, dto.RevisionPrioritaria);

            return dto;
        }

        /// <summary>
        /// Filtra fuentes citadas dejando SOLO las que coinciden con la lista blanca (comparación
        /// laxa: contiene, sin acentos ni mayúsculas). Devuelve además si se descartó algo (señal
        /// de posible fuente inventada → revisión prioritaria).
        /// </summary>
        private (List<string> Validas, bool HuboFueraDeLista) FiltrarPorListaBlanca(string fuentesRaw)
        {
            var validas = new List<string>();
            if (string.IsNullOrWhiteSpace(fuentesRaw))
                return (validas, false);

            var limpio = fuentesRaw.Trim();
            if (limpio.Equals("Ninguna", StringComparison.OrdinalIgnoreCase) ||
                limpio.Equals("No aplica", StringComparison.OrdinalIgnoreCase) ||
                limpio.Equals("No especificadas", StringComparison.OrdinalIgnoreCase))
                return (validas, false);

            var permitidas = _config.FuentesClinicasPermitidas?
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(Normalizar)
                .ToList() ?? new List<string>();

            var huboFuera = false;
            var citadas = limpio.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);

            foreach (var cita in citadas)
            {
                var norm = Normalizar(cita);
                // Coincide si la cita contiene una fuente permitida o viceversa (tolerante a "ESPEN
                // 2023 (guía...)" vs "ESPEN 2023").
                var match = permitidas.Any(p => norm.Contains(p) || p.Contains(norm));
                if (match) validas.Add(cita);
                else { huboFuera = true; _logger.LogWarning("Fuente fuera de lista blanca descartada: {Cita}", cita); }
            }

            return (validas, huboFuera);
        }

        // Vocabulario del eje de SEGURIDAD/infección (normalizado, sin acentos). Compartido por el
        // chequeo de cita huérfana: si una referencia lo toca pero el cuerpo no, la cita es huérfana.
        private static readonly string[] _terminosSeguridad =
        {
            "listeria", "salmonella", "infeccion", "bacteria", "parasito", "patogeno", "contamin",
            "crudo", "cruda", "poco cocido", "poco cocida", "sin pasteuriz", "inmuno", "defensas"
        };

        private static bool CitaHablaDeSeguridad(IEnumerable<string> fuentes) =>
            fuentes.Any(f => TocaSeguridad(f));

        private static bool CuerpoHablaDeSeguridad(string cuerpo) => TocaSeguridad(cuerpo);

        private static bool TocaSeguridad(string texto)
        {
            var norm = Normalizar(texto);
            return _terminosSeguridad.Any(t => norm.Contains(t));
        }

        /// <summary>Extrae "ETIQUETA: valor" tolerando markdown; toma hasta el fin de línea.</summary>
        private static string ExtraerCampo(string fullText, string etiqueta)
        {
            // Etiquetas ASCII fijas del prompt (TITULO, QUE_ES…). Toleramos ** de markdown alrededor.
            var patron = $@"\*?\*?{Regex.Escape(etiqueta)}:\*?\*?\s*(.+?)(?=\r?\n|$)";
            var m = Regex.Match(fullText, patron, RegexOptions.IgnoreCase);
            if (!m.Success) return "";
            return LimpiarTextoPlano(m.Groups[1].Value);
        }

        private static bool EsAfirmativo(string valor)
        {
            var v = Normalizar(valor);
            return v is "si" or "sí" || v.StartsWith("si ") || v.StartsWith("si,");
        }

        private static string LimpiarTextoPlano(string s) =>
            (s ?? "").Replace("**", "").Replace("*", "").Replace("#", "").Trim();

        private static string RecortarDescripcion(string s) =>
            s.Length > 500 ? RecortarEnPalabra(s, 500) : s;

        /// <summary>Recorta a <paramref name="max"/> caracteres SIN partir la última palabra; agrega
        /// "…" solo si de verdad hubo que recortar. Bajo el límite, devuelve el texto tal cual.</summary>
        private static string RecortarEnPalabra(string s, int max)
        {
            s = (s ?? "").Trim();
            if (s.Length <= max) return s;
            var corte = s.Substring(0, max);
            var ultimoEspacio = corte.LastIndexOf(' ');
            if (ultimoEspacio > 0) corte = corte.Substring(0, ultimoEspacio);
            return corte.TrimEnd(' ', ',', ';', ':', '.', '…') + "…";
        }

        private static string Normalizar(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            var sb = new StringBuilder(s.Length);
            foreach (var c in s.Normalize(System.Text.NormalizationForm.FormD))
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString();
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
