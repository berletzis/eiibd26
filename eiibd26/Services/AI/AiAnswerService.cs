using eiibd26.Configuration;
using eiibd26.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Implementación del servicio de respuestas de IA usando Claude de Anthropic
    /// </summary>
    public class AiAnswerService : IAiAnswerService
    {
        private readonly HttpClient _httpClient;
        private readonly AiAnswerConfiguration _config;
        private readonly IAiPromptBuilder _promptBuilder;
        private readonly ILogger<AiAnswerService> _logger;

        public AiAnswerService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiAnswerConfiguration> config,
            IAiPromptBuilder promptBuilder,
            ILogger<AiAnswerService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("AnthropicClient");
            _config = config.Value;
            _promptBuilder = promptBuilder;
            _logger = logger;

            // Log configuration on initialization
            _logger.LogInformation("AiAnswerService initialized:");
            _logger.LogInformation("  BaseAddress: {BaseAddress}", _httpClient.BaseAddress);
            _logger.LogInformation("  API Version: {ApiVersion}", _config.ApiVersion);
            _logger.LogInformation("  Model: {Model}", _config.Model);
            _logger.LogInformation("  Has API Key: {HasApiKey}", !string.IsNullOrWhiteSpace(_config.AnthropicApiKey));

            // Verify headers
            var hasApiKeyHeader = _httpClient.DefaultRequestHeaders.Contains("x-api-key");
            var hasVersionHeader = _httpClient.DefaultRequestHeaders.Contains("anthropic-version");
            _logger.LogInformation("  Headers configured: x-api-key={HasApiKey}, anthropic-version={HasVersion}", 
                hasApiKeyHeader, hasVersionHeader);
        }

        public async Task<string> GenerarRespuestaAsync(Pregunta pregunta, CancellationToken cancellationToken = default, string? contextoDinamico = null)
        {
            if (!_config.Enabled)
            {
                _logger.LogWarning("AI Answer service is disabled in configuration");
                throw new InvalidOperationException("El servicio de IA está deshabilitado");
            }

            if (string.IsNullOrWhiteSpace(_config.AnthropicApiKey))
            {
                _logger.LogError("Anthropic API key is not configured");
                throw new InvalidOperationException("La clave API de Anthropic no está configurada");
            }

            // Check cancellation before expensive operation
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var systemPrompt = _promptBuilder.BuildSystemPrompt();
                var userPrompt = _promptBuilder.BuildUserPrompt(pregunta, contextoDinamico);

                var requestBody = new
                {
                    model = _config.Model,
                    max_tokens = _config.MaxTokens,
                    temperature = _config.Temperature,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = userPrompt
                        }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation(
                    "Enviando solicitud a Claude API para pregunta {PreguntaId}. Tokens max: {MaxTokens}, Temperatura: {Temperature}, Model: {Model}",
                    pregunta.Id, _config.MaxTokens, _config.Temperature, _config.Model);

                _logger.LogDebug("URL completa: {BaseUrl}messages", _httpClient.BaseAddress);
                _logger.LogDebug("Request body: {JsonContent}", jsonContent);

                var response = await _httpClient.PostAsync("messages", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Error en llamada a Claude API. Status: {StatusCode}, URL: {RequestUri}, Response: {Response}",
                        response.StatusCode, response.RequestMessage?.RequestUri, errorContent);
                    throw new HttpRequestException($"Error en API de Claude: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                // Log the raw response for debugging
                _logger.LogInformation("Raw Claude API response: {ResponseContent}", responseContent);

                var responseData = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent);

                if (responseData?.Content == null || responseData.Content.Length == 0)
                {
                    _logger.LogError("Respuesta vacía de Claude API");
                    throw new InvalidOperationException("La respuesta de la IA está vacía");
                }

                var generatedText = responseData.Content[0].Text;

                _logger.LogInformation(
                    "Respuesta generada exitosamente para pregunta {PreguntaId}. Caracteres: {Length}",
                    pregunta.Id, generatedText?.Length ?? 0);

                return generatedText ?? string.Empty;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timeout al generar respuesta de IA para pregunta {PreguntaId}", pregunta.Id);
                throw new TimeoutException("La generación de la respuesta excedió el tiempo límite", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al generar respuesta de IA para pregunta {PreguntaId}", pregunta.Id);
                throw;
            }
        }

        public async Task<bool> IsAvailableAsync()
        {
            if (!_config.Enabled)
                return false;

            if (string.IsNullOrWhiteSpace(_config.AnthropicApiKey))
                return false;

            // Podríamos hacer un health check a la API aquí si fuera necesario
            return await Task.FromResult(true);
        }

        #region DTOs for Claude API

        private class ClaudeApiResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string? Id { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string? Type { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("role")]
            public string? Role { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public ContentItem[]? Content { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("model")]
            public string? Model { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("stop_reason")]
            public string? StopReason { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("usage")]
            public Usage? Usage { get; set; }
        }

        private class ContentItem
        {
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string? Type { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class Usage
        {
            [System.Text.Json.Serialization.JsonPropertyName("input_tokens")]
            public int InputTokens { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("output_tokens")]
            public int OutputTokens { get; set; }
        }

        #endregion
    }
}
