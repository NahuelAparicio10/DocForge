using DocForge.Application.Abstractions;
using System.Net.Http.Json;
using System.Text.Json;
namespace DocForge.Infrastructure.Services;

public sealed class OllamaSummaryService : ISummaryService
{
    private readonly HttpClient _httpClient;

    public OllamaSummaryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:11434");
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var trimmedText = text.Length > 16000 ? text[..16000] : text;

        var prompt = $"""
                      Eres un asistente especializado en resumir documentos en español.

                      Quiero que hagas un resumen ÚTIL, no demasiado corto, bien estructurado y fácil de leer.
                      No hagas un resumen telegráfico ni excesivamente escueto.

                      Devuelve el resultado en este formato exacto:

                      RESUMEN GENERAL
                      [un resumen relativamente extenso, claro y conectado, de varios párrafos si hace falta]

                      PUNTOS CLAVE
                      - [punto clave 1]
                      - [punto clave 2]
                      - [punto clave 3]
                      - [punto clave 4]
                      - [punto clave 5]

                      CONCLUSIÓN
                      [cierre con la idea principal del documento]

                      Reglas:
                      - Escribe en español.
                      - Sé claro, natural y bien organizado.
                      - Prioriza ideas importantes frente a detalles secundarios.
                      - Si el texto parece académico o técnico, mantén el contenido importante pero explícalo de forma comprensible.
                      - No inventes información que no aparezca en el texto.
                      - Si el contenido es largo, sintetiza sin quedarte demasiado corto.

                      Texto a resumir:

                      {trimmedText}
                      """;

        var request = new
        {
            model = "llama3.2:3b",
            prompt,
            stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/generate", request, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (json.RootElement.TryGetProperty("response", out var responseElement))
            return responseElement.GetString() ?? string.Empty;

        return string.Empty;
    }
}
