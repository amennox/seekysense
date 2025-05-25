using Nest;
using McpServer.Configuration;
using Microsoft.Extensions.Options;
using McpServer.Models;

public class ImageService
{
    private readonly IElasticClient _client;
    private const string IndexName = "images";

    public ImageService(IOptions<ElasticSettings> elasticOptions)
    {
        var settings = new ConnectionSettings(new Uri(elasticOptions.Value.BaseUrl))
            .DefaultIndex(IndexName);
        _client = new ElasticClient(settings);
    }

    public async Task<IndexResponse> InsertImageAsync(Image image)
    {
        return await _client.IndexDocumentAsync(image);
    }

    public async Task<Image?> GetImageByIdAsync(string id)
    {
        var result = await _client.GetAsync<Image>(id, idx => idx.Index(IndexName));
        return result.Found ? result.Source : null;
    }

    public async Task<List<Image>> SearchImagesAsync(string? scope = null, string? businessId = null)
    {
        var search = await _client.SearchAsync<Image>(s => s
            .Index(IndexName)
            .Query(q =>
                q.Bool(b =>
                    b.Must(
                        string.IsNullOrEmpty(scope) ? null : q.Term(t => t.Field(f => f.Scope).Value(scope)),
                        string.IsNullOrEmpty(businessId) ? null : q.Term(t => t.Field(f => f.BusinessId).Value(businessId))
                    )
                )
            )
            .Size(1000)
        );

        return search.Hits.Select(hit => hit.Source).ToList();
    }

    public async Task<UpdateResponse<Image>> UpdateImageAsync(Image image)
    {
        return await _client.UpdateAsync<Image>(image.Id, u => u.Index(IndexName).Doc(image));
    }

    public async Task<bool> DeleteImageAsync(string id)
    {
        var response = await _client.DeleteAsync<Image>(id, d => d.Index(IndexName));
        return response.IsValid && response.Result == Result.Deleted;
    }
}
