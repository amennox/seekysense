using Nest;
using McpServer.Models;
using Microsoft.Extensions.Options;
using McpServer.Configuration;

public class ElasticsearchService
{
    private readonly IElasticClient _client;
    private const string IndexName = "elements";
    private readonly ElasticSettings _elastic;

    public ElasticsearchService(IOptions<ElasticSettings> elasticOptions)
    {
        var settings = new ConnectionSettings(new Uri(elasticOptions.Value.BaseUrl))
            .DefaultIndex(IndexName);
        _client = new ElasticClient(settings);
        _elastic = elasticOptions.Value;
    }

    public async Task<List<(Element Element, double? Score)>> SearchByVectorAndTextAsync(double[] vector, string query, string? scope, string businessId)
    {
        var response = await _client.SearchAsync<Element>(s => s
            .Query(q => q
                .FunctionScore(fs => fs
                    .Query(fq => fq.Bool(b => b
                        .Must(
                            bq => bq.MatchAll()
                        )
                        .Should(
                            bq => bq.Match(m => m
                                .Field(f => f.Title)
                                .Query(query)
                                .Boost(2.0f)
                            ),
                            bq => bq.Match(m => m
                                .Field(f => f.Fulltext)
                                .Query(query)
                                .Boost(1.2f)
                            )
                        )
                        .MinimumShouldMatch(1)
                        .Filter(f => f.Bool(bb => bb
                            .Must(
                                scope != null
                                    ? f.Term(t => t.Field(e => e.Scope).Value(scope))
                                    : f.MatchAll(),
                                f.Bool(bbb => bbb
                                    .Should(
                                        f.Term(t => t.Field(e => e.BusinessId).Value(businessId)),
                                        f.Term(t => t.Field(e => e.BusinessId).Value((string?)null)),
                                        f.Term(t => t.Field(e => e.BusinessId).Value("0"))
                                    )
                                )
                            )
                        ))
                    ))
                    .Functions(f => f
                        .ScriptScore(ss => ss
                            .Script(s => s
                                .Source("cosineSimilarity(params.query_vector, 'fulltextVect') + 1.0")
                                .Params(p => p.Add("query_vector", vector))
                            )
                        )
                    )
                    .BoostMode(FunctionBoostMode.Multiply)
                )
            )
            .Size(100)
        );

        return response.Hits.Select(hit => (hit.Source, hit.Score)).ToList();
    }




    public async Task<IndexResponse> InsertElementAsync(Element element)
    {
        return await _client.IndexDocumentAsync(element);
    }

    public async Task<IndexResponse> InsertScopeDocumentAsync(ScopeDocument scope)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex("scopes"); // Nuovo indice scopes
        var client = new ElasticClient(settings);

        return await client.IndexDocumentAsync(scope);
    }



    // Cancella documento Scope da indice scopes
    public async Task<DeleteResponse> DeleteScopeDocumentAsync(string scopeId)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex("scopes");
        var client = new ElasticClient(settings);

        return await client.DeleteAsync<ScopeDocument>(scopeId);
    }

    public async Task<IndexResponse> InsertFineTuningElementAsync(FineTuningElement element)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex("ftelement");
        var client = new ElasticClient(settings);

        return await client.IndexDocumentAsync(element);
    }

    public async Task<FineTuningElement?> GetFineTuningElementByIdAsync(string id)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex("ftelement");
        var client = new ElasticClient(settings);

        var response = await client.GetAsync<FineTuningElement>(id);
        return response.Source;
    }

    public async Task<List<FineTuningElement>> SearchFineTuningElementsAsync(string? scope, string? businessId)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex("ftelement");
        var client = new ElasticClient(settings);

        var response = await client.SearchAsync<FineTuningElement>(s => s
            .Query(q => q.Bool(b => b
                .Must(
                    scope != null ? q.Term(t => t.Scope, scope) : null,
                    businessId != null ? q.Term(t => t.BusinessId, businessId) : null
                )))
            .Size(100));

        return response.Documents.ToList();
    }

    public async Task<UpdateResponse<FineTuningElement>> UpdateFineTuningElementAsync(string id, FineTuningElement updatedElement)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex("ftelement");
        var client = new ElasticClient(settings);

        return await client.UpdateAsync<FineTuningElement>(id, u => u.Doc(updatedElement));
    }

    public async Task<DeleteResponse> DeleteFineTuningElementAsync(string id)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex("ftelement");
        var client = new ElasticClient(settings);

        return await client.DeleteAsync<FineTuningElement>(id);
    }

    public async Task<BulkResponse> InsertFineTuningElementsAsync(IEnumerable<FineTuningElement> elements)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex("ftelement");
        var client = new ElasticClient(settings);

        var bulkDescriptor = new BulkDescriptor();

        foreach (var element in elements)
        {
            bulkDescriptor.Index<FineTuningElement>(op => op
                .Document(element)
            );
        }

        var response = await client.BulkAsync(bulkDescriptor);
        return response;
    }

    public async Task<List<Element>> GetAllElementsAsync()
    {
        var response = await _client.SearchAsync<Element>(s => s
            .Query(q => q.MatchAll())
            .Size(10000) // oppure imposta un limite secondo le tue esigenze
        );

        return response.Documents.ToList();
    }

    public async Task<List<(Element Element, double? Score)>> SearchByVectorAndTextFTAsync(
    double[] vector, string query, string? scope, string businessId)
    {
        var response = await _client.SearchAsync<Element>(s => s
            .Query(q =>
                q.Bool(b => b
                    .Must(
                        q.ScriptScore(ss => ss
                            .Query(inner => inner.Match(m => m
                                .Field(f => f.Fulltext)
                                .Query(query)))
                            .Script(scs => scs
                                .Source("cosineSimilarity(params.query_vector, 'fulltextVectFT') + 1.0")
                                .Params(p => p.Add("query_vector", vector))
                            )
                        )
                    )
                    .Filter(f => f.Bool(bb => bb
                        .Must(
                            scope != null
                                ? f.Term(t => t.Field(e => e.Scope).Value(scope))
                                : f.MatchAll(),
                            f.Bool(bbb => bbb
                                .Should(
                                    f.Term(t => t.Field(e => e.BusinessId).Value(businessId)),
                                    f.Term(t => t.Field(e => e.BusinessId).Value((string?)null)),
                                    f.Term(t => t.Field(e => e.BusinessId).Value("0"))
                                )
                            )
                        )
                    ))
                )
            )
            .Size(1000)
        );

        return response.Hits.Select(hit => (hit.Source, hit.Score)).ToList();
    }


    public async Task<List<(Image Image, double? Score)>> SearchByImageVectorAsync(
        double[] imageVector,
        string? text,
        string? scope,
        string businessId
    )
    {
        var response = await _client.SearchAsync<Image>(s => s
            .Index("images")
            .Query(q =>
                q.ScriptScore(ss => ss
                    .Query(inner => inner
                        .Bool(b => b
                            .Filter(
                                !string.IsNullOrEmpty(scope) ? q => q.Term(t => t.Field(f => f.Scope).Value(scope)) : q => q.MatchAll(),
                                !string.IsNullOrEmpty(businessId) ? q => q.Term(t => t.Field(f => f.BusinessId).Value(businessId)) : q => q.MatchAll()
                            )
                        )
                    )
                    .Script(scs => scs
                        .Source("cosineSimilarity(params.query_vector, 'imageVect') + 1.0")
                        .Params(p => p.Add("query_vector", imageVector))
                    )
                )
            )
            .Size(10)
        );

        return response.Hits.Select(hit => (hit.Source, hit.Score)).ToList();
    }


    public async Task<List<(Element Element, double? Score)>> SearchByVectorOnlyAsync(double[] vector, string? scope, string businessId)
    {
        var response = await _client.SearchAsync<Element>(s => s
            .Query(q =>
                q.Bool(b => b
                    .Must(
                        q.ScriptScore(ss => ss
                            .Query(inner => inner.MatchAll()) // Nessuna query testuale
                            .Script(scs => scs
                                .Source("cosineSimilarity(params.query_vector, 'fulltextVect') + 1.0")
                                .Params(p => p.Add("query_vector", vector))
                            )
                        )
                    )
                    .Filter(f => f.Bool(bb => bb
                        .Must(
                            scope != null
                                ? f.Term(t => t.Field(e => e.Scope).Value(scope))
                                : f.MatchAll(),
                            f.Bool(bbb => bbb
                                .Should(
                                    f.Term(t => t.Field(e => e.BusinessId).Value(businessId)),
                                    f.Term(t => t.Field(e => e.BusinessId).Value((string?)null)),
                                    f.Term(t => t.Field(e => e.BusinessId).Value("0"))
                                )
                            )
                        )
                    ))
                )
            )
            .Size(1000)
        );

        return response.Hits.Select(hit => (hit.Source, hit.Score)).ToList();
    }

}