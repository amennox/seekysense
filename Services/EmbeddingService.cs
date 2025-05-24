using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using McpServer.Models;
using Microsoft.Extensions.Options;
using McpServer.Configuration;

public class EmbeddingService
{
    private readonly HttpClient _http;
    private readonly EmbeddingSettings _embedding;
    private readonly EmbeddingFTSettings _embeddingFT;

    public EmbeddingService(
        IHttpClientFactory httpClientFactory,
        IOptions<EmbeddingSettings> embeddingOptions,
        IOptions<EmbeddingFTSettings> embeddingFTOptions)
    {
        _http = httpClientFactory.CreateClient();
        _embedding = embeddingOptions.Value;
        _embeddingFT = embeddingFTOptions.Value;
    }

    // === Metodo retrocompatibile ===
    public async Task<float[]?> GetEmbeddingFromOllama(string text)
    {
        var payload = new
        {
            model = _embedding.Model,
            input = text
        };

        var response = await _http.PostAsJsonAsync(_embedding.BaseUrl, payload);

        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
        return result?.Embeddings?.FirstOrDefault()?.ToArray();
    }

    public async Task<float[]?> GetEmbedding(string text, string type = "fine-tuned")
    {
        string baseUrl;
        string model;

        if (type == "standard")
        {
            baseUrl = _embedding.BaseUrl;
            model = _embedding.Model;
        }
        else
        {
            if (_embeddingFT == null || string.IsNullOrWhiteSpace(_embeddingFT.BaseUrl) || string.IsNullOrWhiteSpace(_embeddingFT.Model))
            {
                throw new InvalidOperationException("EmbeddingFT settings are not configured properly.");
            }

            baseUrl = _embeddingFT.BaseUrl;
            model = _embeddingFT.Model;
        }

        var payload = new
        {
            model = model,
            input = text
        };

        var response = await _http.PostAsJsonAsync(baseUrl, payload);

        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
        return result?.Embeddings?.FirstOrDefault()?.ToArray();
    }

    private class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embeddings")]
        public List<List<float>>? Embeddings { get; set; }
    }
}
