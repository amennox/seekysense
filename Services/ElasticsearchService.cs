using Nest;
using McpServer.Models;
using Microsoft.Extensions.Options;
using McpServer.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Elasticsearch.Net;
using System.Numerics;

public class ElasticsearchService
{
    private readonly IElasticClient _client;
    private const string IndexName = "elements";
    private const string FtImageIndex = "ftimages";
    private const string FtImageElement = "ftelements";
    private const string ImageIndex = "images";

    private const string ScopeIndex = "scopes";

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
            .DefaultIndex(ScopeIndex); // Nuovo indice scopes
        var client = new ElasticClient(settings);

        return await client.IndexDocumentAsync(scope);
    }



    // Cancella documento Scope da indice scopes
    public async Task<DeleteResponse> DeleteScopeDocumentAsync(string scopeId)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex(ScopeIndex);
        var client = new ElasticClient(settings);

        return await client.DeleteAsync<ScopeDocument>(scopeId);
    }

    public async Task<IndexResponse> InsertFineTuningElementAsync(FineTuningElement element)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex(FtImageElement);
        var client = new ElasticClient(settings);

        return await client.IndexDocumentAsync(element);
    }

    public async Task<FineTuningElement?> GetFineTuningElementByIdAsync(string id)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex(FtImageElement);
        var client = new ElasticClient(settings);

        var response = await client.GetAsync<FineTuningElement>(id);
        return response.Source;
    }

    public async Task<List<FineTuningElement>> SearchFineTuningElementsAsync(string? scope, string? businessId)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex(FtImageElement);
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
            .DefaultIndex(FtImageElement);
        var client = new ElasticClient(settings);

        return await client.UpdateAsync<FineTuningElement>(id, u => u.Doc(updatedElement));
    }

    public async Task<DeleteResponse> DeleteFineTuningElementAsync(string id)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex(FtImageElement);
        var client = new ElasticClient(settings);

        return await client.DeleteAsync<FineTuningElement>(id);
    }

    public async Task<BulkResponse> InsertFineTuningElementsAsync(IEnumerable<FineTuningElement> elements)
    {
        var settings = new ConnectionSettings(new Uri(_elastic.BaseUrl))
            .DefaultIndex(FtImageElement);
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
            .Index(ImageIndex)
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


    public async Task<Element?> GetElementByIdAsync(string id)
    {
        var response = await _client.GetAsync<Element>(id, idx => idx.Index("elements"));
        if (!response.Found)
            return null;
        return response.Source;
    }

    public async Task<IndexResponse> InsertFtImageAsync(FtImage ftImage)
        => await _client.IndexAsync(ftImage, i => i.Index(FtImageIndex));

    public async Task<FtImage?> GetFtImageAsync(string id)
        => (await _client.GetAsync<FtImage>(id, g => g.Index(FtImageIndex))).Source;

    public async Task<bool> DeleteFtImageAsync(string id)
        => (await _client.DeleteAsync<FtImage>(id, d => d.Index(FtImageIndex))).IsValid;

    public async Task<(List<FtImage> items, long total)> GetFtImagesByScopeAsync(string? scope, int page, int pageSize)
    {
        var searchRequest = new SearchRequest("ftimages")
        {
            From = (page - 1) * pageSize,
            Size = pageSize,
            Query = string.IsNullOrEmpty(scope)
                ? new MatchAllQuery()
                : new TermQuery { Field = "scope", Value = scope }
        };

        var response = await _client.SearchAsync<FtImage>(searchRequest);
        return (response.Documents.ToList(), response.Total);
    }

    public async Task<List<AggregatedElementResult>> SearchWithPositiveNegativeAndCollapseAsync(
      double[] includeEmbedding,
      double[]? excludeEmbedding,
      string queryText,
      string? scope,
      string businessId,
      bool groupByExternalId,
      int size = 20,
      double negativeWeight = 0.8,
      double minScore = 1.05)
    {
        return await SearchWithRawJsonApproachAsync(
            includeEmbedding, excludeEmbedding, queryText, scope, businessId,
            groupByExternalId, size, negativeWeight, minScore);
    }

    private async Task<List<AggregatedElementResult>> SearchWithRawJsonApproachAsync(
        double[] includeEmbedding,
        double[]? excludeEmbedding,
        string queryText,
        string? scope,
        string businessId,
        bool groupByExternalId,
        int size,
        double negativeWeight,
        double minScore)
    {
        // Script per il calcolo del punteggio
        var scriptParams = new Dictionary<string, object>
        {
            { "inc", includeEmbedding }
        };

        string scriptSource = "double pos = cosineSimilarity(params.inc, 'fulltextVect');";
        if (excludeEmbedding != null)
        {
            scriptParams["exc"] = excludeEmbedding;
            scriptParams["negWeight"] = negativeWeight;
            scriptSource += " double neg = cosineSimilarity(params.exc, 'fulltextVect');";
            scriptSource += " return Math.max(0.0, (pos - params.negWeight * neg) + 1.0);";
        }
        else
        {
            scriptSource += " return pos + 1.0;";
        }

        // Costruisce i filtri (stessa logica del tuo codice esistente)
        var filters = new List<object>();

        if (!string.IsNullOrEmpty(scope))
        {
            filters.Add(new { term = new { scope = scope } });
        }

        // Filtro BusinessId con la tua logica esistente
        filters.Add(new
        {
            @bool = new
            {
                should = new object[]
                {
                    new { term = new { businessId = businessId } }
                }
            }
        });

        // Query body
        var queryBody = new
        {
            size = size,
            min_score = minScore,
            query = new
            {
                script_score = new
                {
                    query = new
                    {
                        @bool = new
                        {
                            must = new object[]
                            {
                                new { match = new { fulltext = queryText } }
                            },
                            filter = filters.ToArray()
                        }
                    },
                    script = new
                    {
                        source = scriptSource,
                        @params = scriptParams
                    }
                }
            }
        };

        // Aggiungi collapse se necessario
        object finalQuery = queryBody;
        if (groupByExternalId)
        {
            finalQuery = new
            {
                queryBody.size,
                queryBody.min_score,
                queryBody.query,
                collapse = new
                {
                    field = "externalId",
                    inner_hits = new
                    {
                        name = "chunks",
                        size = 10,
                        sort = new object[] { new { _score = "desc" } },
                        _source = true
                    }
                }
            };
        }

        // Esegui la query
        var response = await _client.LowLevel.SearchAsync<StringResponse>(
            IndexName,
            PostData.Serializable(finalQuery)
        );

        if (!response.Success)
        {
            throw new Exception($"Search failed: {response.OriginalException?.Message ?? response.DebugInformation}");
        }

        return ParseSearchResponse(response.Body, groupByExternalId);
    }

    private List<AggregatedElementResult> ParseSearchResponse(string responseBody, bool hasCollapse)
    {
        var results = new List<AggregatedElementResult>();

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var hits = jsonDoc.RootElement.GetProperty("hits").GetProperty("hits");

        foreach (var hit in hits.EnumerateArray())
        {
            var score = hit.GetProperty("_score").GetDouble();
            var elementJson = hit.GetProperty("_source").GetRawText();
            var element = JsonSerializer.Deserialize<Element>(elementJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var result = new AggregatedElementResult
            {
                ParentElement = element!,
                Score = score,
                Chunks = new List<Element> { element! }
            };

            // Gestisci inner_hits se c'è collapse
            if (hasCollapse && hit.TryGetProperty("inner_hits", out var innerHits) &&
                innerHits.TryGetProperty("chunks", out var chunks))
            {
                result.Chunks.Clear();
                var chunkHits = chunks.GetProperty("hits").GetProperty("hits");

                foreach (var chunkHit in chunkHits.EnumerateArray())
                {
                    var chunkJson = chunkHit.GetProperty("_source").GetRawText();
                    var chunkElement = JsonSerializer.Deserialize<Element>(chunkJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (chunkElement != null)
                    {
                        result.Chunks.Add(chunkElement);
                    }
                }
            }

            results.Add(result);
        }

        return results;
    }

    // Metodo per integrare con il tuo sistema di embedding esistente
    public async Task<List<AggregatedElementResult>> SearchFromQueryRequestAsync(
        QueryRequest request,
        Func<string, string?, Task<double[]>> getEmbeddingFunc)
    {
        // Genera embedding per la query principale
        var includeEmbedding = await getEmbeddingFunc(request.Query, request.Type);

        // Genera embedding per query negativa se presente
        double[]? excludeEmbedding = null;
        if (!string.IsNullOrEmpty(request.QueryNegative))
        {
            excludeEmbedding = await getEmbeddingFunc(request.QueryNegative, request.Type);
        }

        return await SearchWithPositiveNegativeAndCollapseAsync(
            includeEmbedding: includeEmbedding,
            excludeEmbedding: excludeEmbedding,
            queryText: request.Query,
            scope: request.Scope,
            businessId: request.BusinessId,
            groupByExternalId: request.GroupByExternalId ?? false
        );
    }

    public async Task<List<AggregatedElementResultChunks>> SearchAggregatedElementsAsync(
    double[] includeEmbedding,
    double[]? excludeEmbedding,
    string queryText,
    string? scope,
    string businessId,
    int size = 20,
    bool standardEmbedding = true)
    {
        // Costruisci script score
        var scriptParams = new Dictionary<string, object>
    {
        { "inc", includeEmbedding }
    };
        string scriptSource;
        if (standardEmbedding)
        {
            scriptSource = "double pos = cosineSimilarity(params.inc, 'fulltextVect');";
        }
        else
        {
            scriptSource = "double pos = cosineSimilarity(params.inc, 'fulltextVectFT');";
        }

        if (excludeEmbedding != null)
        {
            scriptParams["exc"] = excludeEmbedding;
            scriptParams["negWeight"] = 2.8;
            if (standardEmbedding)
            {
                scriptSource += " double neg = cosineSimilarity(params.exc, 'fulltextVect');";
            }
            else
            {
                scriptSource += " double neg = cosineSimilarity(params.exc, 'fulltextVectFT');";
            }
            scriptSource += " return Math.max(0.0, (pos - params.negWeight * neg) + 1.0);";
        }
        else
        {
            scriptSource += " return pos + 1.0;";
        }

        // Query di base
        var mustQueries = new List<QueryContainer>
    {
        new MatchQuery { Field = "fulltext", Query = queryText }
    };
        if (!string.IsNullOrWhiteSpace(scope))
            mustQueries.Add(new TermQuery { Field = "scope", Value = scope });

        var shouldQueries = new List<QueryContainer>
    {
        new TermQuery { Field = "businessId", Value = businessId },
        new TermQuery { Field = "businessId", Value = "0" }
    };

        var scriptScoreQuery = new ScriptScoreQuery
        {
            Query = new BoolQuery
            {
                Must = mustQueries,
                Should = shouldQueries
            },
            Script = new InlineScript(scriptSource) { Params = scriptParams }
        };

        // Query aggregata: terms su externalId + top_hits
        var searchRequest = new SearchRequest("elements")
        {
            Size = 0, // Non vogliamo risultati flat, solo aggregati
            Query = scriptScoreQuery,
            Aggregations = new AggregationDictionary
        {
            {
                "by_externalId", new TermsAggregation("by_externalId")
                {
                    Field = "externalId",
                    Size = size,
                    Order = new List<TermsOrder>
                    {
                        new TermsOrder { Key = "avg_score", Order = SortOrder.Descending }
                    },
                    Aggregations = new AggregationDictionary
                    {
                        { "top_chunks", new TopHitsAggregation("top_chunks") { Size = 10 } },
                        { "avg_score", new AverageAggregation("avg_score", "_score") }
                    }
                }
            }
        }
        };

        var response = await _client.SearchAsync<Element>(searchRequest);

        if (!response.IsValid)
            throw new Exception("Elasticsearch aggregation query failed: " + response.DebugInformation);

        var results = new List<AggregatedElementResultChunks>();

        var buckets = response.Aggregations.Terms("by_externalId")?.Buckets;
        if (buckets != null)
        {
            const double negativeThreshold = 0.05;

            foreach (var bucket in buckets)
            {
                var avgScore = bucket.Average("avg_score")?.Value ?? 0.0;
                var topHits = bucket.TopHits("top_chunks");

                // 1. Crea una lista temporanea di tuple (chunk, vettore)
                var chunkWithVector = new List<(ArticleChunk chunk, double[]? vector)>();

                if (topHits?.Hits<Element>() != null)
                {
                    foreach (var hit in topHits.Hits<Element>())
                    {
                        var src = hit.Source;
                        if (src == null) continue;

                        var chunk = new ArticleChunk
                        {
                            Id = src.Id,
                            Title = src.Title,
                            ChunkSection = src.ChunkSection ?? "",
                            Fulltext = src.Fulltext,
                            Score = hit.Score ?? 0.0
                        };

                        // Prendi il vettore embedding corretto SOLO per il filtro
                        double[]? vect = standardEmbedding ? src.FulltextVect : src.FulltextVectFT;

                        chunkWithVector.Add((chunk, vect));
                    }
                }

                // 2. Logica di esclusione gruppi troppo simili alla query negativa
                bool hasNegative = false;
                if (excludeEmbedding != null && chunkWithVector.Any())
                {
                    foreach (var pair in chunkWithVector)
                    {
                        var vect = pair.vector;
                        if (vect == null) continue;
                        double sim = CosineSimilarity(excludeEmbedding, vect);
                        if (sim >= negativeThreshold)
                        {
                            hasNegative = true;
                            break;
                        }
                    }
                }
                if (hasNegative)
                    continue; // salta questo gruppo

                // 3. Aggiungi alla risposta SOLO la parte "pulita"
                results.Add(new AggregatedElementResultChunks
                {
                    ExternalId = bucket.Key as string ?? "",
                    AvgScore = avgScore,
                    Chunks = chunkWithVector.Select(x => x.chunk).ToList()
                });
            }
        }

        return results;
    }
    public static double CosineSimilarity(double[] a, double[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must be same length");

        double dot = 0, normA = 0, normB = 0;

        int simdLength = Vector<double>.Count; // di solito 4 o 8
        int i = 0;

        // Calcolo vettoriale
        for (; i <= a.Length - simdLength; i += simdLength)
        {
            var va = new Vector<double>(new ReadOnlySpan<double>(a, i, simdLength));
            var vb = new Vector<double>(new ReadOnlySpan<double>(b, i, simdLength));

            dot += Vector.Dot(va, vb);
            normA += Vector.Dot(va, va);
            normB += Vector.Dot(vb, vb);
        }

        // Coda scalare
        for (; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return (normA == 0 || normB == 0) ? 0 : dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}