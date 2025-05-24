using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicExpresso;
using McpServer.Configuration;
using McpServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace McpServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly ElasticsearchService _esService;
        private readonly EmbeddingService _emService;
        private readonly ConfigurationService _configService;
        private readonly HttpClient _httpClient;
        private readonly SummarizeSettings _summSettings;

        public QueryController(
            ElasticsearchService esService,
            EmbeddingService emService,
            ConfigurationService configService,
            IHttpClientFactory httpClientFactory,
            IOptions<SummarizeSettings> summarizeOptions)
        {
            _esService = esService;
            _emService = emService;
            _configService = configService;
            _httpClient = httpClientFactory.CreateClient();
            _summSettings = summarizeOptions.Value;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Post([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest("Query cannot be empty");
            if (string.IsNullOrWhiteSpace(request.BusinessId))
                return BadRequest("BusinessId is required");
            if (string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest("UserId is required");

            string searchType = string.IsNullOrWhiteSpace(request.Type) ? "standard" : request.Type.ToLower();

            var standardEmbedding = (searchType == "standard" || searchType == "mixed")
                ? await _emService.GetEmbedding(request.Query, "standard")
                : null;

            var fineTunedEmbedding = (searchType == "fine-tuned" || searchType == "mixed")
                ? await _emService.GetEmbedding(request.Query, "fine-tuned")
                : null;

            if ((standardEmbedding == null || standardEmbedding.Length == 0) &&
                (fineTunedEmbedding == null || fineTunedEmbedding.Length == 0))
            {
                return StatusCode(500, "Failed to generate embedding(s)");
            }

            var combinedResults = new List<(Element Element, double? Score)>();

            if (standardEmbedding != null)
            {
                var stdResults = await _esService.SearchByVectorAndTextAsync(
                    standardEmbedding,
                    request.Query,
                    request.Scope,
                    request.BusinessId
                );
                combinedResults.AddRange(stdResults);
            }

            if (fineTunedEmbedding != null)
            {
                var ftResults = await _esService.SearchByVectorAndTextFTAsync( // vedi punto successivo
                    fineTunedEmbedding,
                    request.Query,
                    request.Scope,
                    request.BusinessId
                );
                combinedResults.AddRange(ftResults);
            }

            // Normalizza e deduplica risultati
            var grouped = combinedResults
                .GroupBy(x => x.Element.Id)
                .Select(g =>
                {
                    var best = g.OrderByDescending(x => x.Score ?? 0).First();
                    return (Element: best.Element, Score: best.Score);
                })
                .ToList();

            var maxScore = grouped.Max(e => e.Score) ?? 1.0;
            var results = new List<ElementResponseDto>();

            foreach (var (element, score) in grouped)
            {
                if ((score ?? 0) < maxScore * 0.65) continue;

                var dto = await ProcessElementAsync(
                    element,
                    score ?? 0.0,
                    maxScore,
                    request.BusinessId,
                    request.UserId
                );
                if (dto != null)
                    results.Add(dto);
            }

            return Ok(results);
        }


        [HttpPost("deepsearch")]
        public async Task DeepSearch([FromBody] QueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Query cannot be empty");
                return;
            }
            if (string.IsNullOrWhiteSpace(request.BusinessId))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("BusinessId is required");
                return;
            }
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("UserId is required");
                return;
            }

            Response.ContentType = "application/json; charset=utf-8";
            await Response.WriteAsync("[");
            var first = true;

            // Livello 1: embedding e search
            var emb1 = await _emService.GetEmbeddingFromOllama(request.Query);
            var lvl1 = await _esService.SearchByVectorAndTextAsync(
                emb1,
                request.Query,
                request.Scope,
                request.BusinessId
            );

            var maxScore = lvl1.Max(e => e.Score) ?? 1.0;
            var processedIds = new HashSet<string>();
            var parentList = new List<(Element el, string summary)>();

            foreach (var (element, score) in lvl1)
            {
                if ((score ?? 0) < maxScore * 0.7) continue;

                // Stato: elaborazione elemento
                /*
                if (!first) await Response.WriteAsync(",");
                var status1 = new { status = $"Elaborazione elemento {element.Id}" };
                await Response.WriteAsync(JsonSerializer.Serialize(status1));
                await Response.Body.FlushAsync();
                */

                // Processa e ottieni fulltext/live
                var dto = await ProcessElementAsync(
                    element,
                    score ?? 0.0,
                    maxScore,
                    request.BusinessId,
                    request.UserId
                );
                if (dto == null)
                    continue;

                // Riassunto con LLM
                var summary = await SummarizeWithOllamaAsync(
                    dto.Fulltext + (dto.FulltextLive ?? string.Empty),
                    request.Query
                );

                if (summary.Contains("@@DELETE@@"))
                    continue;

                // Scrivi risultato livello 1
                if (!first) await Response.WriteAsync(",");
                var result1 = new
                {
                    id = dto.Id,
                    commands = dto.Commands,
                    summary = summary,
                    parentLevel = (string?)null
                };
                await Response.WriteAsync(JsonSerializer.Serialize(result1));
                await Response.Body.FlushAsync();
                first = false;

                processedIds.Add(dto.Id);
                parentList.Add((element, summary));
            }

            // Livello 2: per ogni elemento di livello 1
            foreach (var (parentEl, parentSummary) in parentList)
            {
                // Stato: approfondimento elemento
                /*
                if (!first) await Response.WriteAsync(",");
                var status2 = new { status = $"Approfondimento elemento {parentEl.Id}" };
                await Response.WriteAsync(JsonSerializer.Serialize(status2));
                await Response.Body.FlushAsync();
                */

                // Nuovo embedding da summary
                var emb2 = await _emService.GetEmbeddingFromOllama(parentSummary);
                var lvl2 = await _esService.SearchByVectorAndTextAsync(
                    emb2,
                    request.Query,
                    request.Scope,
                    request.BusinessId
                );

                foreach (var (el2, _) in lvl2)
                {
                    if (processedIds.Contains(el2.Id))
                        continue;

                    // Stato: elaborazione elemento figlio
                    /*
                    if (!first) await Response.WriteAsync(",");
                    var status3 = new { status = $"Elaborazione elemento {el2.Id} (padre {parentEl.Id})" };
                    await Response.WriteAsync(JsonSerializer.Serialize(status3));
                    await Response.Body.FlushAsync();
                    */

                    // Processa elemento figlio
                    var dto2 = await ProcessElementAsync(
                        el2,
                        0.0,
                        maxScore,
                        request.BusinessId,
                        request.UserId
                    );
                    if (dto2 == null)
                        continue;

                    // Riassunto figlio
                    var summary2 = await SummarizeWithOllamaAsync(
                        dto2.Fulltext + (dto2.FulltextLive ?? string.Empty),
                        request.Query
                    );

                    if (summary2.Contains("@@DELETE@@"))
                        continue;

                    // Scrivi risultato livello 2
                    if (!first) await Response.WriteAsync(",");
                    var result2 = new
                    {
                        id = dto2.Id,
                        commands = dto2.Commands,
                        summary = summary2,
                        parentLevel = parentEl.Id
                    };
                    await Response.WriteAsync(JsonSerializer.Serialize(result2));
                    await Response.Body.FlushAsync();

                    processedIds.Add(dto2.Id);
                    first = false;
                }
            }

            await Response.WriteAsync("]");
        }


        [HttpPost("deepsense")]
        public async Task<IActionResult> PostDeepsense([FromBody] QueryRequest request)
        {
            // Validazioni base
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest("Query cannot be empty");
            if (string.IsNullOrWhiteSpace(request.BusinessId))
                return BadRequest("BusinessId is required");
            if (string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest("UserId is required");

            // Embedding standard
            var embedding = await _emService.GetEmbedding(request.Query, "standard");
            if (embedding == null || embedding.Length == 0)
                return StatusCode(500, "Failed to generate embedding");

            // Prima ricerca: vettore + testo
            var initialResults = await _esService.SearchByVectorAndTextAsync(
                embedding, request.Query, request.Scope, request.BusinessId);

            var topEmbeddings = initialResults
                .Where(e => e.Element.FulltextVect != null)
                .Take(20)
                .Select(e => e.Element.FulltextVect!)
                .ToList();

            if (topEmbeddings.Count < 2)
                return BadRequest("Insufficient embedding vectors for PCA");

            // Costruzione matrice Nx1024
            double[,] matrix = new double[topEmbeddings.Count, 1024];
            for (int i = 0; i < topEmbeddings.Count; i++)
                for (int j = 0; j < 1024; j++)
                    matrix[i, j] = topEmbeddings[i][j];

            // PCA
            var pcaVector = PcaHelper.CalcolaAutovettorePrincipale(matrix);
            var pcaFloat = pcaVector.Select(d => (float)d).ToArray();

            // Seconda ricerca: solo vettore PCA
            var finalResults = await _esService.SearchByVectorOnlyAsync(
                pcaFloat, request.Scope, request.BusinessId);

            // Normalizzazione + filtro + enrichment
            var maxScore = finalResults.Max(e => e.Score) ?? 1.0;
            var results = new List<ElementResponseDto>();

            foreach (var (element, score) in finalResults)
            {
                if ((score ?? 0.0) < maxScore * 0.15)
                    continue;

                var dto = await ProcessElementAsync(
                    element,
                    score ?? 0.0,
                    maxScore,
                    request.BusinessId,
                    request.UserId
                );

                if (dto != null)
                    results.Add(dto);
            }

            return Ok(results);
        }



        private async Task<string> SummarizeWithOllamaAsync(string text, string userQuery)
        {
            // Costruisci il prompt
            var prompt = _summSettings.PromptTemplate
                .Replace("%%query%%", userQuery)
                .Replace("%%fulltext%%", text);

            // Corpo della richiesta
            var body = new
            {
                model = _summSettings.Model,
                prompt = prompt,
                options = new
                {
                    temperature = 0,
                    top_p = 0.9,
                    top_k = 40,
                    repeat_penalty = 1.1
                },
                stream = false
            };

            // Invia la richiesta
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            var resp = await _httpClient.PostAsync(_summSettings.BaseUrl, content);
            resp.EnsureSuccessStatusCode();

            // Leggi il JSON come JsonElement
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();

            // Estrai e ritorna solo la proprietà "response"
            if (json.TryGetProperty("response", out var responseProp))
            {
                return responseProp.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private class OllamaResponse
        {
            public List<Choice>? Choices { get; set; }
            public class Choice { public string? Text { get; set; } }
        }

        private async Task<ElementResponseDto?> ProcessElementAsync(
            Element element,
            double score,
            double maxScore,
            string businessId,
            string userId)
        {
            var renderer = new RenderTextService();
            var interpreter = new Interpreter();

            var scopeConfig = await _configService.GetScopeByIdAsync(element.Scope);
            string? authToken = null;
            if (scopeConfig != null)
            {
                if (scopeConfig.ScopeDataLiveAuthType == "business")
                {
                    authToken = (await _configService.GetBusinessAuthByBusinessIdAndScopeIdAsync(businessId, element.Scope))?.ApiKey;
                }
                else if (scopeConfig.ScopeDataLiveAuthType == "user")
                {
                    authToken = (await _configService.GetUserAuthByUserIdAndScopeIdAsync(userId, element.Scope))?.ApiKey;
                }
            }

            JsonElement? liveDataJson = null;
            if (!string.IsNullOrWhiteSpace(element.LiveDataUrl))
            {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, element.LiveDataUrl);
                    if (!string.IsNullOrEmpty(authToken))
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                    var resp = await _httpClient.SendAsync(req);
                    if (resp.IsSuccessStatusCode)
                        liveDataJson = await resp.Content.ReadFromJsonAsync<JsonElement>();
                }
                catch { }
            }

            if (liveDataJson.HasValue && !string.IsNullOrWhiteSpace(element.LiveDataValidation))
            {
                try
                {
                    var expr = renderer.Render(element.LiveDataValidation, liveDataJson.Value);
                    var modelObj = JsonSerializer.Deserialize<ExpandoObject>(liveDataJson.ToString())!;
                    interpreter.SetVariable("model", modelObj);
                    var isValid = interpreter.Eval(expr) as bool? ?? false;
                    if (!isValid) return null;
                }
                catch { return null; }
            }

            string? fulltextLive = null;
            if (liveDataJson.HasValue && !string.IsNullOrWhiteSpace(element.LiveDataTemplate))
            {
                var rendered = renderer.Render(element.LiveDataTemplate, liveDataJson.Value);
                if (!rendered.StartsWith("Error")) fulltextLive = rendered;
            }

            var relevancePercent = maxScore > 0 ? (score / maxScore) * 100.0 : 0.0;

            return new ElementResponseDto
            {
                Id = element.Id,
                Scope = element.Scope,
                BusinessId = element.BusinessId,
                Title = element.Title,
                Commands = element.Commands,
                Fulltext = element.Fulltext,
                FulltextLive = fulltextLive,
                RelevanceScore = (int)Math.Round(relevancePercent)
            };
        }
    }
}
