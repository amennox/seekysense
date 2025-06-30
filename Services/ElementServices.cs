using Nest;
using McpServer.Models;
using Microsoft.Extensions.Options;
using McpServer.Configuration;

public class ElementService
{
    private readonly IElasticClient _client;
    private readonly EmbeddingService _embeddingService;

    private readonly ConfigurationService _configurationService;
    private const string IndexName = "elements";

    public ElementService(IOptions<ElasticSettings> elasticOptions, EmbeddingService embeddingService, ConfigurationService configurationService)
    {
        var settings = new ConnectionSettings(new Uri(elasticOptions.Value.BaseUrl))
            .DefaultIndex(IndexName);
        _client = new ElasticClient(settings);
        _embeddingService = embeddingService;
        _configurationService = configurationService;
    }

    // CREATE
    public async Task<IndexResponse> InsertElementAsync(Element element)
    {
        // Recupera scope
        var scope = await _configurationService.GetScopeByIdAsync(element.Scope);
        if (scope == null)
            throw new Exception("Scope not found");

        // Scegli tipo embedding
        string embeddingType = "standard"; // Default
        if (!string.IsNullOrEmpty(scope.Embedding))
            embeddingType = scope.Embedding;

        // Genera embedding
        var vector = await _embeddingService.GetEmbedding(element.Fulltext);
        if (vector == null || vector.Length == 0)
            throw new Exception("Embedding generation failed");

        element.FulltextVect = vector;

        // Genera embeddingFT
        if(embeddingType=="fine-tuned" || embeddingType=="mixed")
        {
            var vectorFT = await _embeddingService.GetEmbedding(element.Fulltext, embeddingType);
            if (vectorFT == null || vectorFT.Length == 0)
                throw new Exception("Embedding FT generation failed");

            element.FulltextVectFT = vectorFT;
        }
        else
        {
            element.FulltextVectFT = null; // Non necessario per standard
        }



        var response = await _client.IndexDocumentAsync(element);
        return response;
    }

    // READ ALL (con filtri opzionali)
    public async Task<List<Element>> SearchElementsAsync(string? scope = null, string? businessId = null)
    {
        var response = await _client.SearchAsync<Element>(s => s
            .Query(q =>
                q.Bool(b =>
                    b.Must(
                        scope != null ? q.Term(t => t.Field(f => f.Scope).Value(scope)) : null,
                        businessId != null ? q.Term(t => t.Field(f => f.BusinessId).Value(businessId)) : null
                    )
                )
            )
            .Size(1000) // Puoi parametrizzare la size
        );

        return response.Documents.ToList();
    }

    // READ ONE
    public async Task<Element?> GetElementByIdAsync(string id)
    {
        var response = await _client.GetAsync<Element>(id);
        return response.Found ? response.Source : null;
    }

    // UPDATE
    public async Task<IndexResponse> UpdateElementAsync(Element updatedElement)
    {
        // Rigenera l'embedding per il nuovo fulltext
        var scope = await _configurationService.GetScopeByIdAsync(updatedElement.Scope);
        if (scope == null)
            throw new Exception("Scope not found");

        // Scegli tipo embedding
        string embeddingType = "standard"; // Default
        if (!string.IsNullOrEmpty(scope.Embedding))
            embeddingType = scope.Embedding;

        var vector = await _embeddingService.GetEmbedding(updatedElement.Fulltext);
        if (vector == null || vector.Length == 0)
            throw new Exception("Embedding generation failed");

        updatedElement.FulltextVect = vector;

        // Genera embeddingFT
        if(embeddingType=="fine-tuned" || embeddingType=="mixed")
        {
            var vectorFT = await _embeddingService.GetEmbedding(updatedElement.Fulltext, embeddingType);
            if (vectorFT == null || vectorFT.Length == 0)
                throw new Exception("Embedding FT generation failed");

            updatedElement.FulltextVectFT = vectorFT;
        }
        else
        {
            updatedElement.FulltextVectFT = null; // Non necessario per standard
        }


        var response = await _client.IndexDocumentAsync(updatedElement);
        return response;
    }

    // DELETE
    public async Task<bool> DeleteElementAsync(string id)
    {
        var response = await _client.DeleteAsync<Element>(id);
        return response.IsValid;
    }
}
